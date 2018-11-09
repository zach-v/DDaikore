using System;
namespace DDaikore
{
    //Not based on Stream because I'd rather not implement a bunch of functionality that nobody should need
    public class CommMessageStreamWriter
    {
        /// <summary>
        /// Buffer
        /// </summary>
        private byte[] b;
        /// <summary>
        /// Position
        /// </summary>
        private int p;

        public CommMessageStreamWriter(byte[] buffer, int initialPosition = 0)
        {
            b = buffer;
            p = initialPosition;
        }

        public void Write(string v)
        {
            var o = v.ToCharArray();
            Write(o.Length);
            for (int x = 0; x < o.Length; x++) Write(o[x]);
        }

        public void Write(double v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(float v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(ulong v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(long v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(uint v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(int v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(ushort v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(short v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(char v)
        {
            Write(BitConverter.GetBytes(v));
        }

        public void Write(byte v)
        {
            b[p++] = v;
        }

        public void Write(byte[] v)
        {
            //Write as-is because most machines are little-endian now
            if (BitConverter.IsLittleEndian)
            {
                v.CopyTo(b, p);
                p += v.Length;
            }
            else
            {
                //Probably faster equivalent of: v.Reverse().ToArray().CopyTo(b, p);
                for (var x = v.Length - 1; x >= 0; x--) b[p++] = v[x];
            }
        }
    }
}
