//This file is committed to the public domain (written by GuyPerfect)

using System;
using System.IO;

namespace DDaikore
{

    /*
     * Extends RIFF to implement a wrapper around a Microsoft Wave file. 
     * Regardless of the data format, audio frames are read from the underlying
     * stream as stereo into an array of floats representing samples in the
     * range of [-1.0, 1.0].
     */

    class WaveReader : RIFF
    {

        // Private fields
        private uint BitsPerSample; // Sample size (bits) of source
        private uint DataOffset;    // Position in source of sample data
        private uint DataSize;      // Data size (bytes) of source
        private uint Duration;      // Duration in frames of the audio
        private uint FrameSize;     // Frame size (bytes) of source
        private uint SamplingRate;  // Audio sampling rate
        private bool Stereo;        // True only if source is stereo

        // Microsoft Wave format constant
        private const int WAVE_FORMAT_PCM = 1;



        ///////////////////////////////////////////////////////////////////////
        //                          Public Methods                           //
        ///////////////////////////////////////////////////////////////////////

        // Retrieve the size in bits of source audio samples
        public uint GetBitsPerSample()
        {
            return BitsPerSample;
        }

        // Retrieve the offset in the stream of the audio data
        public uint GetDataOffset()
        {
            return DataOffset;
        }

        // Retrieve the size in bytes in the stream of the audio data
        public uint GetDataSize()
        {
            return DataSize;
        }

        // Retrieve the duration in frames of the source audio
        public uint GetDuration()
        {
            return Duration;
        }

        // Retrieve the duration in seconds of the source audio
        public double GetDurationTime()
        {
            // Error checking -- There is no associated wave file
            if (!HasStream())
            {
                return 0.0;
            }
            
            return (double) Duration / SamplingRate;
        }

        // Retrieve the size in bytes of a source audio frame
        public uint GetFrameSize()
        {
            return FrameSize;
        }

        // Retrieve the current position, in frames, in the source
        public uint GetPosition()
        {
            // Error checking -- There is no associated wave file
            if (!HasStream())
            {
                return 0;
            }
            
            // Position - Offset is always a multiple of FrameSize
            return (uint) (DataStream.Position - DataOffset) / FrameSize;
        }

        // Retrieve the current position, in seconds, in the source
        public double GetPositionTime()
        {
            // Error checking -- There is no associated wave file
            if (!HasStream())
            {
                return 0.0;
            }

            return (double) GetPosition() / SamplingRate;
        }

        // Retrieve the sampling rate
        public uint GetSamplingRate()
        {
            return SamplingRate;
        }

        // Retrieve whether the end of the stream has been reached
        public bool IsEoS()
        {
            return GetPosition() == Duration;
        }

        // Retrieve whether the source is stereo
        public bool IsStereo()
        {
            return Stereo;
        }

        // Read some number of frames from the source
        // Returns the actual number of frames processed
        public uint Read(float[] samples, uint offset, uint frames)
        {
            // Error checking -- There is no associated wave file
            if (!HasStream())
            {
                return 0;
            }

            // Restrict the number of frames based on stream length
            frames = Math.Min(frames, Duration - GetPosition());

            // Process frames as stereo floats
            for (long x = offset, count = frames; count > 0; x += 2, count--)
            {
                // Read the left sample from the source
                samples[x] = ReadSample();

                // Read the right sample if stereo, copy left sample if mono
                samples[x + 1] = Stereo ? ReadSample() : samples[x];
            }

            // Return the actual number of frames processed
            return frames;
        }

        // Specify the current position, in frames, in the source
        // Returns the actual position set
        public uint SetPosition(uint frames)
        {
            // Error checking -- There is no associated wave file
            if (!HasStream())
            {
                return 0;
            }

            // Restrict the range of the input position
            frames = Math.Min(frames, Duration);

            // Seek to the corresponding position in the file
            try
            {
                DataStream.Seek(DataOffset + frames * FrameSize,
                    SeekOrigin.Begin);
            }
            catch { }

            // Return the actual position set
            return GetPosition();
        }

        // Specify the current position, in seconds, in the source
        // Returns the actual position set
        public double SetPositionTime(double seconds)
        {
            // Restrict the range of the input position
            seconds = Math.Min(Math.Max(seconds, 0.0), 1.0);

            // Seek to the corresponding frame
            SetPosition((uint)Math.Floor(seconds * Duration));

            // Return the actual position set
            return GetPositionTime();

        }

        // Associate a new file data stream with this instance
        // Returns true on success
        public new bool SetStream(Stream stream) {

            // Attempt the base class's method
            if (!base.SetStream(stream))
            {
                return false;
            }

            try
            {
                // Process Wave file data
                Chunk[] chunks = CheckChunks();
                DecodeFormat (chunks[0]); // Process data format
                ConfigMembers(chunks[1]); // Prepare instance fields
                
                // Prime the stream for reading audio samples
                DataStream.Seek(DataOffset, SeekOrigin.Begin);
            }

            // Any type of error occurred
            catch
            {
                DataStream = null; // Indicate no associated stream
                return false;
            }

            // No error occurred -- Return success
            return true;
        }



        ///////////////////////////////////////////////////////////////////////
        //                          Private Methods                          //
        ///////////////////////////////////////////////////////////////////////

        // Inspect the RIFF chunks for the necessary Wave file information
        private Chunk[] CheckChunks()
        {
            Chunk? fmt = null, data = null;

            // Search through all chunks for the required chunk IDs
            for (int x = 0; x < Chunks.Length; x++)
            {
                if (Chunks[x].ID.Equals("fmt ")) // Audio format information
                    fmt  = Chunks[x];
                if (Chunks[x].ID.Equals("data")) // Audio sample data
                    data = Chunks[x];
            }

            // Ensure all required chunks were found
            if (fmt == null || data == null || ((Chunk)fmt).Size < 16)
            {
                throw new Exception("Invalid wave file");
            }

            // Return all required chunks as an array
            return new Chunk[2] { (Chunk)fmt, (Chunk)data };
        }

        // Configure remaining instance members for an audio file
        private void ConfigMembers(Chunk data)
        {
            // Configure instance members
            DataOffset = data.Offset;
            DataSize   = data.Size;
            FrameSize  = BitsPerSample >> (Stereo ? 2 : 3);
            Duration   = DataSize / FrameSize;

            // Ensure there is an integer number of frames in the data
            if ((DataSize & -FrameSize) != DataSize)
            {
                throw new Exception("Insufficient audio data.");
            }

        }

        // Decode the audio format information from the "fmt " chunk
        private void DecodeFormat(Chunk fmt)
        {
            // Seek to the format information in the stream
            DataStream.Seek(fmt.Offset, SeekOrigin.Begin);
            
            // Decode format information
            ushort encoding = Reader.ReadUInt16();
            ushort channels = Reader.ReadUInt16();
            SamplingRate    = Reader.ReadUInt32();
            Reader.ReadUInt32(); // ByteRate   = SamplingRate * BlockAlign
            Reader.ReadInt16();  // BlockAlign = Channels * BitsPerSample/8
            BitsPerSample   = (uint) Reader.ReadInt16();

            // Error checking on format information
            if (
                encoding != WAVE_FORMAT_PCM  || // Only PCM is supported
                channels < 1 || channels > 2 || // Only mono or stereo
                SamplingRate == 0            || // Invalid sampling rate
                (BitsPerSample != 8 && BitsPerSample != 16) // Supported bits
            )
            {
                throw new Exception("Wave file format error");
            }

            // Update instance members
            Stereo = channels == 2;
        }

        // Read the next sample from the stream, converting to float
        private float ReadSample()
        {
            // 8-bit unsigned
            if (BitsPerSample == 8)
                return (float) Reader.ReadByte() / 127.5f - 1.0f;

            // Else, 16-bit signed
            return (float) Reader.ReadInt16() / 32768.0f;
        }

    }
}
