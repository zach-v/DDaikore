using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace DDaikore
{
    class Core
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
        public Action<int> AudioResponse;
        public Action<int, int> ReceiveInput;
        /// <summary>
        /// Currently active menu (0 = main menu; -1 = not in a menu)
        /// </summary>
        public int menuIndex = 0;
        /// <summary>
        /// Computational frame counter
        /// </summary>
        public long frameCounter = 0;
        protected bool exiting = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Please start from the main method of a game class.");

            //Test timing code
            var me = new Core();
            me.MenuLoop = () => {
                Console.WriteLine("Proc " + me.frameCounter);
                if (me.frameCounter > 300) me.Exit();
            };
            me.MenuDraw = () => {
                Console.WriteLine("Draw " + me.frameCounter);
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
                    //Don't try to catch up if you're more than 60 frames behind
                    if (sw.ElapsedTicks > nextTicks + targetTickRate * 60)
                    {
                        nextTicks = sw.ElapsedTicks;
                    }
                    nextTicks += targetTickRate;

                    //Always run the processing loops
                    if (menuIndex != -1)
                    {
                        if (!MenuLoop.Equals(null)) MenuLoop();
                    }
                    else if (!GameLoop.Equals(null)) GameLoop();

                    //Draw if we have plenty of time
                    if (nextTicks - sw.ElapsedTicks >= drawTime || skippedFrames >= 8)
                    {
                        drawStart = sw.ElapsedTicks;
                        if (menuIndex != -1)
                        {
                            if (!MenuDraw.Equals(null)) MenuDraw();
                        }
                        else if (!GameDraw.Equals(null)) GameDraw();

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
        }


    }
}
