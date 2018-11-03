using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DDaikore
{
    public class Core
    {
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
        public Action ReceiveMessage; //Network comms
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
        /// Currently active menu (0 = main menu; -1 = not in a menu)
        /// </summary>
        public int menuIndex = 0;
        public int menuOption = 0;
        /// <summary>
        /// Computational frame counter
        /// </summary>
        public long frameCounter = 0;
        protected bool exiting = false;

        #region Input
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern GetKeyStateRet GetKeyState(int keyCode);

        [Flags]
        private enum GetKeyStateRet : ushort
        {
            None = 0,
            Down = 0x8000,
            Toggled = 0x1
        }

        public enum InputState
        {
            NotHeld, JustReleased, Held, JustPressed
        }

        private class InputInfo
        {
            public InputState state;
            public int controller; //-2 is mouse, -1 is keyboard, 0+ are game controllers (arbitrary count, hence why I shouldn't use an enum)
            public int button;
        }

        /// <summary>
        /// Item1 is the state; Item2 is additional info required to figure out what we're looking for internally
        /// </summary>
        private List<InputInfo> DigitalInputs = new List<InputInfo>();

        /// <summary>
        /// Request input state using the value returned by RegisterInput for the desired key
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public InputState GetInputState(int index)
        {
            return DigitalInputs[index].state;
        }

        public int RegisterInput(Keys key)
        {
            DigitalInputs.Add(new InputInfo { state = InputState.NotHeld, controller = -1, button = (int)key });
            return DigitalInputs.Count - 1;
        }

        private void UpdateInputs()
        {
            //Loop through all registered inputs and update their states
            foreach (var input in DigitalInputs)
            {
                if (input.controller == -1) //Keyboard
                {
                    var keyDown = GetKeyState(input.button).HasFlag(GetKeyStateRet.Down);
                    if (keyDown)
                    {
                        if (input.state == InputState.NotHeld) input.state = InputState.JustPressed;
                        else input.state = InputState.Held;
                    }
                    else
                    {
                        if (input.state == InputState.Held) input.state = InputState.JustReleased;
                        else input.state = InputState.NotHeld;
                    }
                }
                else
                {
                    throw new NotImplementedException("Mouse and game controllers not yet supported");
                }
            }
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

        public void PlaySound(int soundIndex) //TODO: Allow the user to specify a value to send to AudioResponse when the sound finishes playing
        {
            PlayingSounds.Add(new PlayingSoundEffect(LoadedSounds[soundIndex]));
        }

        private void ClearPlayedSounds()
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

        /// <summary>
        /// This is called by the AudioPlayer with no relation to game framerate or video framerate in any way.
        /// For that reason, ClearPlayedSounds removes the completed sounds instead of this method.
        /// </summary>
        public uint OnNext(float[] samples, uint offset, uint frames)
        {
            //Mix all the playing sound effects together into the samples array
            Array.Clear(samples, 0, samples.Length);

            foreach (var sound in PlayingSounds)
            {
                if (!sound.isDone) sound.mix(samples);
            }
            return frames;
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

            me.MenuLoop = () => {
                //Console.WriteLine("Proc " + me.frameCounter);
                Console.WriteLine(me.GetInputState(akey));
                if (me.GetInputState(bkey) == InputState.JustPressed) Console.WriteLine("B just pressed");
                if (me.GetInputState(bkey) == InputState.JustReleased) Console.WriteLine("B just released");
                //Play some sounds (3x at start and 60bpm)
                if (me.frameCounter == 30 || me.frameCounter == 60 || me.frameCounter % 120 == 0) me.PlaySound(testSound); 
                //if (me.frameCounter > 300) me.Exit();
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
            while (!exiting)
            {
                if (sw.ElapsedTicks >= nextTicks)
                {
                    UpdateInputs();
                    //Don't try to catch up if you're more than 60 frames behind
                    if (sw.ElapsedTicks > nextTicks + targetTickRate * 60)
                    {
                        nextTicks = sw.ElapsedTicks;
                    }
                    nextTicks += targetTickRate;

                    //Always run the processing loops
                    if (menuIndex != -1)
                    {
                        if (!ReferenceEquals(MenuLoop, null)) MenuLoop();
                    }
                    else if (!ReferenceEquals(GameLoop, null)) GameLoop();

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
                    frameCounter++;
                }

                //Sleep if we have time to do so
                if (nextTicks - sw.ElapsedTicks > millisecond) Thread.Sleep(1);
            }

            ap.Close();
        }


    }
}
