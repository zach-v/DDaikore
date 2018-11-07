using DDaikore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DDaikontin
{
    public class InputMappings
    {
        public readonly int enterKey;
        public readonly int upArrowKey;
        public readonly int downArrowKey;
        public readonly int leftArrowKey;
        public readonly int rightArrowKey;
        public readonly int wKey;
        public readonly int sKey;
        public readonly int aKey;
        public readonly int dKey;
        public readonly int spaceKey;
        public readonly int escapeKey;
        public readonly int shiftKey;
        protected Core core;

        public InputMappings(Core core)
        {
            this.core = core;
            enterKey = core.RegisterInput(Keys.Enter);
            upArrowKey = core.RegisterInput(Keys.Up);
            downArrowKey = core.RegisterInput(Keys.Down);
            leftArrowKey = core.RegisterInput(Keys.Left);
            rightArrowKey = core.RegisterInput(Keys.Right);
            wKey = core.RegisterInput(Keys.W);
            sKey = core.RegisterInput(Keys.S);
            aKey = core.RegisterInput(Keys.A);
            dKey = core.RegisterInput(Keys.D);
            spaceKey = core.RegisterInput(Keys.Space);
            escapeKey = core.RegisterInput(Keys.Escape);
            shiftKey = core.RegisterInput(Keys.LShiftKey);
        }

        public InputState GetState(int inputIndex)
        {
            return core.GetInputState(inputIndex);
        }
    }
}
