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

        public void UpdateInputs()
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
    }
}
