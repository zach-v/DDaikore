using System;

namespace DDaikore
{
    public class PlayingSoundEffect
    {
        public SoundEffect se;
        public uint position;
        public bool repeat = false;
        public float volume = 1.0f;

        public PlayingSoundEffect(SoundEffect sound, bool repeat = false, float volume = 1.0f)
        {
            se = sound;
            this.repeat = repeat;
            this.volume = volume;
        }

        public bool isDone
        {
            get {
                return se.duration * 2 == position;
            }
        }

        //Returns true if it finishes playing
        public bool mix(float[] samples)
        {
            lock (this)
            {
                long dur = Math.Min(se.duration * 2 - position, samples.Length);
                for (int sample = 0; sample < dur; sample++)
                {
                    samples[sample] += se.samples[position++] * volume;
                    if (repeat && isDone)
                    {
                        position = 0;
                        dur = sample + Math.Min(se.duration * 2, samples.Length - sample);
                    }
                }
                return isDone;
            }
        }

        public void stopSound()
        {
            lock (this)
            {
                repeat = false;
                position = se.duration * 2;
            }
        }
    }
}
