using System;
namespace DDaikore
{
    //Not based on Stream because I'd rather not implement a bunch of functionality that nobody should need
    public class CommMessageStreamReader
    {
        /// <summary>
        /// Buffer
        /// </summary>
        private byte[] b;
        /// <summary>
        /// Position
        /// </summary>
        private int p;

        public CommMessageStreamReader(byte[] buffer, int initialPosition = 0)
        {
            b = buffer;
            p = initialPosition;
        }

        public void Read(out string v)
        {
            int len;
            Read(out len);
            var o = new char[len];
            for (int x = 0; x < len; x++) Read(out o[x]);
            v = new string(o);
        }

        public void Read(out double v)
        {
            Read(out var o, sizeof(double));
            v = BitConverter.ToDouble(o, 0);
        }

        public void Read(out float v)
        {
            Read(out var o, sizeof(float));
            v = BitConverter.ToSingle(o, 0);
        }

        public void Read(out ulong v)
        {
            Read(out var o, sizeof(ulong));
            v = BitConverter.ToUInt64(o, 0);
        }

        public void Read(out long v)
        {
            Read(out var o, sizeof(long));
            v = BitConverter.ToInt64(o, 0);
        }

        public void Read(out uint v)
        {
            Read(out var o, sizeof(uint));
            v = BitConverter.ToUInt32(o, 0);
        }

        public void Read(out int v)
        {
            Read(out var o, sizeof(int));
            v = BitConverter.ToInt32(o, 0);
        }

        public void Read(out ushort v)
        {
            Read(out var o, sizeof(ushort));
            v = BitConverter.ToUInt16(o, 0);
        }

        public void Read(out short v)
        {
            Read(out var o, sizeof(short));
            v = BitConverter.ToInt16(o, 0);
        }

        public void Read(out char v)
        {
            Read(out var o, sizeof(char));
            v = BitConverter.ToChar(o, 0);
        }

        public void Read(out byte v)
        {
            v = b[p++];
        }

        public void Read(out byte[] v, int len)
        {
            v = new byte[len];
            //Write as-is because most machines are little-endian now
            if (BitConverter.IsLittleEndian)
            {
                b.CopyTo(v, p);
                p += len;
            }
            else
            {
                //Probably faster equivalent of: b.CopyTo(v, p); v = v.Reverse().ToArray();
                for (var x = v.Length - 1; x >= 0; x--) v[x] = b[p++];
            }
        }
    }
}
