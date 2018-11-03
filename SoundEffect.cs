using System;
using System.IO;

namespace DDaikore
{
    public class SoundEffect
    {
        public uint samplingRate;
        public float[] samples;
        public uint duration;

        public SoundEffect(string filename)
        {
            FileStream Stream = File.OpenRead(filename);
            WaveReader wr = new WaveReader();
            wr.SetStream(Stream);
            samplingRate = wr.GetSamplingRate();
            duration = wr.GetDuration();

            samples = new float[duration * 2];
            wr.Read(samples, 0, duration);
            Stream.Close();
        }
    }
}
