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
        public ushort GameVersion = 0;

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
        /// </summary>
        public Action Connected;
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
        public long lastFrameCounterReceivedViaComm { get; private set; } = 0;
        protected bool exiting = false;
        public bool CommIsConnected { get { return connection == null || !connection.Connected; } }
        /// <summary>
        /// Estimated number of frames for a round-trip message-response with the connected player (running average)
        /// </summary>
        public long estimatedPing { get; private set; } = 0;

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

                foreach (var sound in PlayingSounds)
                {
                    if (!sound.isDone) sound.mix(samples);
                }
                return frames;
            }
        }
        #endregion

        #region Comm
        private long lastCommReceived;
        private int permissibleCommFramesAhead = 120; //You can get up to about a second ahead (slows down the framerate as you approach this number)
        private int slowdownLagFrames = 20; //Decelerate the framerate as you approach this many frames since the last comm message received
        private const int port = 53252;
        private Socket connection = null;
        private int headerLength = 12; //Message length is first in the header, followed by the frame counter when the message was generated (nothing else is currently necessary)
        private List<byte[]> incomingMessages = new List<byte[]>(); //Incoming network messages. Should be one at a time (per connection)
        private PseudoRandom safeRnd = new PseudoRandom();
        private bool actedAsListener = false;
        private Thread commThread, commHostThread;
        private Mutex acceptingConnectionMutex = new Mutex(); //When this is locked, the game loop should freeze entirely

        //Note about random number generators: 
        //You never have to sync the seed if you generate a random number exactly once per frame and you allow both clients to calculate every frame.

        /// <summary>
        /// Network multiplayer-safe random number. Changes once per frame to keep clients in sync in case random numbers may be generated due to player behavior.
        /// </summary>
        public double RandomDouble()
        {
            return (double)safeRnd.lastValue / ((double)uint.MaxValue + 1);
        }

        /// <summary>
        /// Network multiplayer-safe random number. Changes once per frame to keep clients in sync in case random numbers may be generated due to player behavior.
        /// </summary>
        public int RandomInt(int maxValue)
        {
            return (int)(safeRnd.lastValue % maxValue); //Modulus hurts the uniform distribution, especially with large maxValue, but I'm not worried about it
        }

        private void CommInit()
        {
            lastCommReceived = 0;

            commThread = new Thread(RunComms);
            commThread.Start();
        }

        /// <summary>
        /// When expecting another player to connect to you, call this. Creates a new thread.
        /// </summary>
        public void ListenForIncomingConnection()
        {
            if (GameVersion == 0) throw new Exception("Game version must be set for networking");
            commHostThread = new Thread(CommListen);
            commHostThread.Start();
        }

        public void StopListening()
        {
            if (commHostThread != null && commHostThread.IsAlive)
            {
                commHostThread.Abort();
                connection.Close();
                commHostThread = null;
            }
        }

        /// <summary>
        /// This must be the first message sent after connecting to a peer. Lock acceptingConnectionMutex around this call.
        /// </summary>
        private void SendInitialMessage()
        {
            //Send the initial message (Core version, game version, lastRandom but modified to avoid a sign error, and frameCount)
            var buffer = new byte[32];
            var tBytes = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder(CoreVersion));
            tBytes.CopyTo(buffer, 0);
            tBytes = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder(GameVersion));
            tBytes.CopyTo(buffer, 2);
            safeRnd.lastValue &= 0x7FFFFF7F; //Mask to avoid sign issues
            tBytes = BitConverter.GetBytes((int)IPAddress.HostToNetworkOrder(safeRnd.lastValue));
            tBytes.CopyTo(buffer, 4);
            tBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(frameCounter));
            tBytes.CopyTo(buffer, 8);
            
            SendCommMessage(buffer);
        }

        private void CommListen()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            connection = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            connection.Bind(localEndPoint);
            connection.Listen(1);
            actedAsListener = true;

            while (true)
            {
                Socket client;
                try
                {
                    lock (acceptingConnectionMutex)
                    {
                        client = connection.Accept();
                        connection.Close();
                        connection = client;
                        SendInitialMessage();
                    }
                    return;
                }
                catch { }
                Thread.Sleep(100);
            }
        }

        public void Connect(IPAddress ip)
        {
            if (connection != null) connection.Close();
            actedAsListener = false;
            IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);
            connection = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            connection.ReceiveTimeout = 10000; //Timeout is 10 seconds
            //Connect on a new thread so we don't block the client (though we could just do it in RunComms)
            new Thread(() => {
                connection.Connect(remoteEndPoint);
            }).Start();
        }

        private void RunComms()
        {
            var buffer = new byte[1024 * 1024]; //A megabyte buffer? Why not!

            while (true)
            {
                bool wasAlreadyConnected = false;
                while (connection != null && connection.Connected)
                {
                    try
                    {
                        var receivedBytes = 0;
                        //Get the message header
                        while (receivedBytes < headerLength)
                        {
                            receivedBytes += connection.Receive(buffer, receivedBytes, headerLength - receivedBytes, SocketFlags.None);
                        }
                        //Now we know what to expect; wait for the rest of it
                        receivedBytes = 0;
                        //Get the first 32 bits from the buffer as an int the endianness-safe way
                        var bodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
                        //Get the next 64 bits from the buffer as a long the endianness-safe way. We'll store it in lastReceivedCommFrameCounter when we lock on incomingMessages.
                        var incomingFrameCounter = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 4));
                        while (receivedBytes < bodyLength)
                        {
                            receivedBytes += connection.Receive(buffer, receivedBytes, bodyLength - receivedBytes, SocketFlags.None);
                        }

                        if (wasAlreadyConnected) //Continuing connection -> expect a message controlled by the game, not by Core
                        {
                            var msgBuffer = new byte[bodyLength];
                            Array.Copy(buffer, msgBuffer, bodyLength);
                            lock (incomingMessages)
                            {
                                incomingMessages.Add(msgBuffer);
                                lastCommReceived = frameCounter;
                                estimatedPing = (estimatedPing * 4 + incomingFrameCounter - lastFrameCounterReceivedViaComm) / 5;
                                lastFrameCounterReceivedViaComm = incomingFrameCounter;
                            }
                        }
                        else //The first message received is different than the others. It has to check the program version and get the random seed.
                        {
                            lock (acceptingConnectionMutex)
                            {
                                if (actedAsListener) //Only the peer that accepted the other one's connection request takes the other's data
                                {
                                    lock (incomingMessages)
                                    {
                                        //First two bytes are Core version number; next are the game version number.
                                        var clientCoreVersion = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, 0));
                                        var clientGameVersion = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, 2));
                                        //Next four bytes are lastRandom (has to have been masked to avoid an exception due to signed numbers)
                                        safeRnd.lastValue = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 4));
                                        //Next four bytes are the frame counter we're going to start at
                                        frameCounter = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 8));
                                        //Throw an empty message on the queue to cause the user's game to start responding
                                        incomingMessages.Add(new byte[0]);
                                        lastCommReceived = frameCounter;
                                        lastFrameCounterReceivedViaComm = frameCounter;
                                    }
                                }
                                //Ready to play multiplayer!
                                estimatedPing = 100; //Start off with a guess
                                Connected(); //TODO: Shouldn't we do this on the game thread? Well, it locks the same mutex, so it's probably fine.
                            }
                        }
                    }
                    catch
                    {
                        //No additional code needed; we got disconnected for whatever reason, so fall out of the while (connection.Connected) loop
                    }
                    Thread.Sleep(1);
                    wasAlreadyConnected = true;
                }
                Thread.Sleep(10);
            }
        }

        //TODO: Maintain a queue of events that can cause the clients to desync (should just be player actions, such ash JustPressed or JustReleased, *and the results thereof*)
        //Example: player pressed Up; velocity is now this (not from queue, but current); position is now this (ditto); player health now 3; enemy health now 0
        //When you receive a message, differences between its recorded events and yours should be rectified and the solution sent with the next message.
        
        //The best possible way to work would be tracking player JustPressed and JustReleased messages and recalculating what the current state of the game should be if those actions had been taken.
        //But that's very hard to do with the way that games usually get developed and with the sheer amount of data that can change frame to frame.

        public void SendCommMessage(byte[] message)
        {
            //Prepend the message length and send it
            var buffer = new byte[message.Length + 4];
            var msgLen = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)message.Length));
            msgLen.CopyTo(buffer, 0);
            var frameCounterBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(frameCounter));
            frameCounterBytes.CopyTo(buffer, 4);
            message.CopyTo(buffer, 12); //TODO: This is just wasting time. We can just send the 12 header bytes first and send 'message' in a second call.
            connection.Send(buffer);
        }
        #endregion

        static void Main(string[] args)
        {
            Console.WriteLine("Please start from the main method of a game class.");

            //Test timing code
            var me = new Core();
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
            ap.Open(44100, 44100 / 30, OnNext); //TODO: We can allow different frequencies and formats
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

            CommInit();

            while (!exiting)
            {
                lock (acceptingConnectionMutex)
                {
                    if (sw.ElapsedTicks >= nextTicks)
                    {
                        controls.UpdateInputs();
                        //Pass on any received comm messages
                        lock (incomingMessages)
                        {
                            //Note: because of the mutexes, even if you're using loopback, you can't receive responses caused by responding to these messages until the next frame.
                            while (incomingMessages.Count != 0)
                            {
                                ReceiveMessage(incomingMessages.First());
                                incomingMessages.RemoveAt(0);
                            }
                        }
                        //Don't try to catch up if you're more than 60 frames behind
                        if (sw.ElapsedTicks > nextTicks + targetTickRate * 60)
                        {
                            nextTicks = sw.ElapsedTicks;
                        }
                        nextTicks += targetTickRate;

                        if (!CommIsConnected) lastCommReceived = frameCounter; //If there's no net connection, no slowing down.
                        var framesTilFreezeDueToComms = Math.Max(lastCommReceived + permissibleCommFramesAhead - frameCounter, 0); //Cap at 0 minimum
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
                            safeRnd.Next();
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

            commThread.Abort();
            if (commHostThread != null && commHostThread.IsAlive) commHostThread.Abort();
            ap.Close();
            Thread.Sleep(100); //Wait for ap threads to stop; still possible to throw an exception, but we're exiting anyway.
        }


    }
}
