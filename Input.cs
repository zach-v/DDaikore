using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DDaikore
{
    public enum InputState
    {
        NotHeld, JustReleased, Held, JustPressed
    }

    public class Input
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern GetKeyStateRet GetKeyState(int keyCode);

        [Flags]
        private enum GetKeyStateRet : ushort
        {
            None = 0,
            Down = 0x8000,
            Toggled = 0x1
        }

        private class InputInfo
        {
            public InputState state;
            public int controller; //-1 is keyboard and mouse, 0+ are game controllers (arbitrary count, hence why I shouldn't use an enum)
            public int button;
        }

        private class AnalogInputInfo
        {
            public int value;
            public int delta;
            public int controller; //-1 is mouse, 0+ are game controllers (arbitrary count, hence why I shouldn't use an enum)
            public int axis;
            public bool useDelta;
        }


        /// <summary>
        /// Item1 is the state; Item2 is additional info required to figure out what we're looking for internally
        /// </summary>
        private List<InputInfo> DigitalInputs = new List<InputInfo>();
        private List<AnalogInputInfo> AnalogInputs = new List<AnalogInputInfo>();

        /// <summary>
        /// Request input state using the value returned by RegisterInput for the desired key
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public InputState GetInputState(int index)
        {
            return DigitalInputs[index].state;
        }

        /// <summary>
        /// Request input value using the identifier returned by RegisterAnalogInput for the desired controller/axis
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetAnalogInput(int index)
        {
            return AnalogInputs[index].useDelta ? AnalogInputs[index].delta : AnalogInputs[index].value;
        }

        /// <summary>
        /// Register a keyboard key or mouse button
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Identifier that you can use with GetInputState</returns>
        public int RegisterInput(Keys key)
        {
            DigitalInputs.Add(new InputInfo { state = InputState.NotHeld, controller = -1, button = (int)key });
            return DigitalInputs.Count - 1;
        }

        public int RegisterInput(int gameController, int button) //TODO: Make an enum for game controller buttons? Make a method to query what buttons are being pressed?
        {
            DigitalInputs.Add(new InputInfo { state = InputState.NotHeld, controller = gameController, button = button });
            return DigitalInputs.Count - 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mouseAxis">0 for X axis, 1 for Y axis (TODO: enum, wheel)</param>
        /// <param name="delta">If true, return the change in the value since the last frame. Otherwise, return the value.</param>
        /// <param name="keepCentered">If true, keep the cursor centered on the screen on this axis (TODO: detect when game loses focus and unlock it)</param>
        /// <returns></returns>
        public int RegisterAnalogInput(int mouseAxis, bool delta, bool keepCentered)
        {
            AnalogInputs.Add(new AnalogInputInfo() { axis = mouseAxis, useDelta = delta, value = -1, delta = -1, controller = -1 });
            return AnalogInputs.Count - 1;
        }

        public int RegisterAnalogInput(int gameController, int button, bool delta)
        {
            //TODO
            throw new NotImplementedException("Not yet implemented");
        }

        public void UpdateInputs()
        {
            //Loop through all registered inputs and update their states
            foreach (var input in DigitalInputs)
            {
                if (input.controller == -1) //Keyboard or mouse
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
                    throw new NotImplementedException("Game controllers not yet supported");
                }
            }

            foreach (var input in AnalogInputs)
            {
                if (input.controller == -1) //Mouse
                {
                    var pos = Cursor.Position;
                    if (input.axis == 0)
                    {
                        input.delta = pos.X - input.value;
                        input.value = pos.X;
                    }
                    else if (input.axis == 1)
                    {
                        input.delta = pos.Y - input.value;
                        input.value = pos.Y;
                    }
                    else
                    {
                        throw new NotImplementedException("Scroll wheel not yet supported");
                    }
                }
                else
                {
                    throw new NotImplementedException("Game controllers not yet supported");
                }
            }
        }
    }
}
