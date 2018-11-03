using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDaikore
{
    public class PlayingSoundEffect
    {
        public SoundEffect se;
        public uint position;

        public PlayingSoundEffect(SoundEffect sound)
        {
            se = sound;
        }

        public bool isDone
        {
            get { return se.duration * 2 == position; }
        }

        //Returns true if it finishes playing
        public bool mix(float[] samples)
        {
            long dur = Math.Min(se.duration * 2 - position, samples.Length);
            for (int sample = 0; sample < dur; sample++)
            {
                samples[sample] += se.samples[position++];
            }
            return isDone;
        }
    }
}
