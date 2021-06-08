using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace DDaikore
{
    public class Core
    {
        public readonly ushort CoreVersion = 0;
        /// <summary>
        /// Must be set to 1 or higher for networking
        /// </summary>
        public readonly ushort GameVersion = 0;

        /// <summary>
        /// Menu procesing. Occurs 120 times per second.
        /// Will only be called if in a menu (menuIndex != -1)
        /// </summary>
        public Action MenuLoop;
        /// <summary>
        /// Game processing. Occurs 120 times per second.
        /// Will only be called if not in a menu (menuIndex = -1)
        /// </summary>
        public Action GameLoop;
        /// <summary>
        /// When the frame counter freezes due to communications latency, this will be called instead of GameLoop or MenuLoop until the situation improves or the peer disconnects.
        /// </summary>
        public Action CommFreezeMenu;
        /// <summary>
        /// Network connection made successfully. Occurs when the first message is received from the other side.
        /// Parameters are connected peer's CoreVersion and connected peer's GameVersion
        /// </summary>
        public Action<ushort, ushort> Connected;
        /// <summary>
        /// Message received over the network. This should contain game state changes, complete with frame counter, since the last message.
        /// </summary>
        public Action<byte[]> ReceiveMessage;
        /// <summary>
        /// Will only be called if in a menu (menuIndex != -1)
        /// </summary>
        public Action MenuDraw;
        /// <summary>
        /// Will only be called if not in a menu (menuIndex = -1)
        /// </summary>
        public Action GameDraw;
        public Action<int> AudioResponse; //TODO

        /// <summary>
        /// Currently active menu (0 = startup screen; -1 = not in a menu)
        /// </summary>
        public int menuIndex = 0;
        public int menuOption = 0;
        
        /// <summary>
        /// Computational frame counter
        /// </summary>
        public long frameCounter { get; private set; } = 0;
        protected bool exiting = false;

        public Core(ushort gameVersion, int commPort = 53252)
        {
            GameVersion = gameVersion;
            comm = new Comm(CoreVersion, GameVersion, () => frameCounter, (val) => { frameCounter = val; }, 
                (coreVer, gameVer) => { Connected(coreVer, gameVer); }, (msg) => { ReceiveMessage(msg); }, commPort);
        }

        #region Input
        protected Input controls = new Input();

        public int RegisterInput(Keys key)
        {
            return controls.RegisterInput(key);
        }

        public InputState GetInputState(int index)
        {
            return controls.GetInputState(index);
        }

        public int RegisterAnalogInput(int mouseAxis, bool delta, bool keepCentered)
        {
            return controls.RegisterAnalogInput(mouseAxis, delta, keepCentered);
        }

        public int GetAnalogInput(int index)
        {
            return controls.GetAnalogInput(index);
        }
        #endregion

        #region Audio
        private List<SoundEffect> LoadedSounds = new List<SoundEffect>();
        private List<PlayingSoundEffect> PlayingSounds = new List<PlayingSoundEffect>();

        /// <summary>
        /// Keep a sound in memory until unregistered or the program exits //TODO: Allow unregistering (takes some effort to do it safely)
        /// </summary>
        /// <returns>A sound identifier index for use with PlaySound</returns>
        public int RegisterSound(string filename)
        {
            LoadedSounds.Add(new SoundEffect(filename));
            return LoadedSounds.Count - 1;
        }

        public PlayingSoundEffect PlaySound(int soundIndex, bool repeat = false, float volume = 1.0f) //TODO: Allow the user to specify a value to send to AudioResponse when the sound finishes playing
        {
            lock (PlayingSounds)
            {
                PlayingSoundEffect t = new PlayingSoundEffect(LoadedSounds[soundIndex], repeat, volume);
                PlayingSounds.Add(t);
                return t;
            }
        }

        private void ClearPlayedSounds()
        {
            lock (PlayingSounds)
            {
                for (int x = PlayingSounds.Count - 1; x >= 0; x--)
                {
                    if (PlayingSounds[x].isDone)
                    {
                        //TODO: This is where you'd call AudioResponse
                        PlayingSounds.RemoveAt(x);
                    }
                }
            }
        }

        /// <summary>
        /// This is called by the AudioPlayer with no relation to game framerate or video framerate in any way.
        /// For that reason, ClearPlayedSounds removes the completed sounds instead of this method.
        /// </summary>
        public uint OnNext(float[] samples, uint offset, uint frames)
        {
            lock (PlayingSounds)
            {
                //Mix all the playing sound effects together into the samples array
                Array.Clear(samples, 0, samples.Length);
                if (exiting) return frames;

                foreach (var sound in PlayingSounds)
                {
                    if (!sound.isDone) sound.mix(samples);
                }
                return frames;
            }
        }
        #endregion

        #region Comm
        private Comm comm;
        private int permissibleCommFramesAhead = 120; //You can get up to about a second ahead (slows down the framerate as you approach this number)
        private int slowdownLagFrames = 20; //Decelerate the framerate as you approach this many frames since the last comm message received

        public bool CommIsConnected { get { return comm.IsConnected; } }

        public long estimatedPing { get { return comm.estimatedPing; } }

        /// <summary>
        /// Network multiplayer-safe random number. Changes once per frame to keep clients in sync in case random numbers may be generated due to player behavior.
        /// </summary>
        public double RandomDouble() { return comm.RandomDouble(); }

        /// <summary>
        /// Network multiplayer-safe random number. Changes once per frame to keep clients in sync in case random numbers may be generated due to player behavior.
        /// </summary>
        public int RandomInt(int maxValue) { return comm.RandomInt(maxValue); }

        /// <summary>
        /// When expecting another player to connect to you, call this. Creates a new thread.
        /// </summary>
        public void ListenForIncomingConnection() { comm.ListenForIncomingConnection(); }

        public void Disconnect() { comm.Disconnect(); }

        public void Connect(IPAddress ip) { comm.Connect(ip); }

        public void SendCommMessage(byte[] message) { comm.SendCommMessage(message); }
        #endregion

        #region Test
        static void Main(string[] args)
        {
            Console.WriteLine("Please start from the main method of a game class.");

            //Test timing code
            var me = new Core(1);
            var akey = me.RegisterInput(Keys.A);
            var bkey = me.RegisterInput(Keys.B);

            var testSound = me.RegisterSound("../../assets/sounds/testSound.wav");
            PlayingSoundEffect playingSound = null;
            me.MenuLoop = () => {
                //Console.WriteLine("Proc " + me.frameCounter);
                Console.WriteLine(me.GetInputState(akey));
                if (me.GetInputState(bkey) == InputState.JustPressed) Console.WriteLine("B just pressed");
                if (me.GetInputState(bkey) == InputState.JustReleased) Console.WriteLine("B just released");
                //Play some sounds (3x at start and 60bpm)
                //if (me.frameCounter == 30 || me.frameCounter == 60 || me.frameCounter % 120 == 0) me.PlaySound(testSound); 
                //if (me.frameCounter > 300) me.Exit();
                //if (me.frameCounter == 1) playingSound = me.PlaySound(testSound, true);
                //if (me.frameCounter == 600) playingSound.stopSound();
            };
            me.MenuDraw = () => {
                //Console.WriteLine("Draw " + me.frameCounter);
            };
            me.Begin();
            //Cleanup goes here
            Console.ReadKey();
        }
        #endregion

        /// <summary>
        /// Allow the Begin method to return
        /// It's a method in case I need to do something like cleanup here. Otherwise I'd just make the boolean public.
        /// </summary>
        public void Exit()
        {
            exiting = true;
        }

        /// <summary>
        /// Starts the initial menu loop
        /// </summary>
        public void Begin()
        {
            //Initialize audio (separate thread)
            var ap = new AudioPlayer();
            ap.Open(44100, 44100 / 30, OnNext, () => { ap = null; }); //TODO: We can allow different frequencies and formats
            ap.Play();

            var sw = new Stopwatch();
            sw.Start();
            const long movingAverageTicks = 5;
            var lastTicks = sw.ElapsedTicks;
            var nextTicks = lastTicks;
            var targetTickRate = Stopwatch.Frequency / 120; //120ths of a second
            var drawTime = targetTickRate / 4; //A sort of simple running average to estimate the ticks needed to draw
            var drawStart = lastTicks;
            var millisecond = Stopwatch.Frequency / 1000; //One millisecond
            var skippedFrames = 0;
            var commsOK = true;

            while (!exiting)
            {
                lock (comm.acceptingConnectionMutex)
                {
                    if (sw.ElapsedTicks >= nextTicks)
                    {
                        controls.UpdateInputs();
                        //Pass on any received comm messages
                        comm.ReceiveMessages();
                        //Don't try to catch up if you're more than 60 frames behind
                        if (sw.ElapsedTicks > nextTicks + targetTickRate * 60)
                        {
                            nextTicks = sw.ElapsedTicks;
                        }
                        nextTicks += targetTickRate;

                        if (!comm.IsConnected) comm.lastCommReceived = frameCounter; //If there's no net connection, no slowing down.
                        var framesTilFreezeDueToComms = Math.Max(comm.lastCommReceived + permissibleCommFramesAhead - frameCounter, 0); //Cap at 0 minimum
                        if (framesTilFreezeDueToComms < slowdownLagFrames)
                        {
                            //Scales at 1x + (0.24x up to 5x)
                            nextTicks += targetTickRate * 5 / (framesTilFreezeDueToComms + 1);
                        }

                        //Always run the processing loops (unless in comm-based freeze)
                        if (framesTilFreezeDueToComms != 0)
                        {
                            if (menuIndex != -1)
                            {
                                if (!ReferenceEquals(MenuLoop, null)) MenuLoop();
                            }
                            else if (!ReferenceEquals(GameLoop, null)) GameLoop();
                            comm.safeRnd.Next();
                            commsOK = true; //To avoid race condition with framesTilFreezeDueToComms and increasing frameCounter
                        }
                        else
                        {
                            //Make some kind of menus available to player during comm freeze, even if gameplay cannot continue
                            CommFreezeMenu();
                            commsOK = false;
                        }

                        //Draw if we have plenty of time
                        if (nextTicks - sw.ElapsedTicks >= drawTime || skippedFrames >= 8)
                        {
                            drawStart = sw.ElapsedTicks;
                            if (menuIndex != -1)
                            {
                                if (!ReferenceEquals(MenuDraw, null)) MenuDraw();
                            }
                            else if (!ReferenceEquals(GameDraw, null)) GameDraw();

                            //Adjust estimated draw time
                            drawTime = (drawTime * (movingAverageTicks - 1) + sw.ElapsedTicks - drawStart) / movingAverageTicks;
                            if (drawTime > targetTickRate * 100) drawTime = targetTickRate * 5; //Cap so it doesn't have trouble catching up if one frame took forever
                            skippedFrames = 0;
                        }
                        else skippedFrames++; //Track frame skipping so even on slow machines it will draw once in a while--once every 8 processing frames
                        if (commsOK) frameCounter++;
                    }
                }

                //Sleep if we have time to do so
                if (nextTicks - sw.ElapsedTicks > millisecond) Thread.Sleep(1);
            }

            comm.Abort();
            ap.Close();
            while (ap != null) Thread.Sleep(10); //Wait for ap threads to stop (ap is set to null when done playing)
        }


    }
}
