using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikore
{
    public class PseudoRandom
    {
        public uint lastValue;
        /// <summary>
        /// Seed with a random number from the built-in Random class
        /// </summary>
        public PseudoRandom()
        {
            lastValue = (uint)new Random().Next();
        }

        /// <summary>
        /// Seed the sequence
        /// </summary>
        public PseudoRandom(uint seed)
        {
            lastValue = seed;
        }

        /// <summary>
        /// Update the 
        /// </summary>
        /// <returns>the next number in the sequence</returns>
        public uint Next()
        {
            lastValue = (uint)(((long)lastValue * 0x41C64E6D + 0x6073) & uint.MaxValue);
            return lastValue;
        }

        public double RandomDouble()
        {
            return (double)lastValue / ((double)uint.MaxValue + 1);
        }
    }
}
