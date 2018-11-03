//This file is committed to the public domain (written by GuyPerfect)

using System;
using System.Collections.Generic;
using System.IO;

namespace DDaikore
{
    /*
     * The Resource Interchange File Format (RIFF) is a generic container for
     * multiple streams of file data. The Microsoft Wave file is packed into a
     * RIFF container. This class parses RIFF data from a file stream.
     */
    class RIFF
    {
        // Protected fields
        protected Chunk[]      Chunks;     // RIFF subchunks
        protected Stream       DataStream; // Data stream for RIFF file
        protected uint         FileSize;   // Top-level RIFF data size
        protected String       FileType;   // RIFF content type
        protected BinaryReader Reader;     // Accesses stream data as binary

        // Type describing a RIFF subchunk (immutable)
        public struct Chunk
        {

            // Public fields
            public readonly string ID;     // Contents identifier
            public readonly uint   Size;   // Size of contents, in bytes
            public readonly uint   Offset; // Position within file of data

            // Simple constructor
            public Chunk(String id, uint size, uint offset)
            {
                ID     = id;
                Size   = size;
                Offset = offset;
            }
        };



        ///////////////////////////////////////////////////////////////////////
        //                          Public Methods                           //
        ///////////////////////////////////////////////////////////////////////

        // Retrieve a copy of the instance's list of RIFF chunks
        public Chunk[] GetChunks()
        {
            // Error checking -- There is no associated RIFF file
            if (!HasStream())
            {
                return null;
            }

            // Make a copy of the list of chunks
            return (Chunk[]) Chunks.Clone();
        }

        // Retrieve the RIFF's content ID
        public string GetFileType()
        {
            // Error checking -- There is no associated RIFF file
            if (!HasStream())
            {
                return null;
            }

            // Return the content ID
            return FileType;
        }

        // Determine whether a data stream is associated with this instance
        public bool HasStream()
        {
            return DataStream != null;
        }
        
        // Associate a new file data stream with this instance
        // Returns true on success
        public bool SetStream(Stream stream)
        {
            try
            {
                // Inspect stream capabilities
                if (!stream.CanSeek || !stream.CanRead)
                {
                    throw new Exception("Unsupported stream");
                }

                // Rewind the stream to the beginning and prepare for reading
                stream.Seek(0, SeekOrigin.Begin);
                DataStream = stream;
                Reader = new BinaryReader(stream);

                // Process file contents
                ReadHeader();
                ParseChunks();
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
        //                         Protected Methods                         //
        ///////////////////////////////////////////////////////////////////////

        // Reads an arbitrary-length string from the file stream
        protected string ReadString(int length)
        {
            // Retrieve the bytes from the stream
            byte[] ret = Reader.ReadBytes(length);
            if (ret.Length < length)
            {
                throw new Exception("Error reading string");
            }

            // Reinterpret the bytes as an ASCII string
            return System.Text.Encoding.ASCII.GetString(ret);
        }



        ///////////////////////////////////////////////////////////////////////
        //                          Private Methods                          //
        ///////////////////////////////////////////////////////////////////////

        // Parse the names and locations of data chunks within the RIFF file
        private void ParseChunks()
        {
            // Dynamic-sized list for enumerating file chunks
            List<Chunk> list = new List<Chunk>();

            // File position of the end of the RIFF chunk
            long eof = 8 + FileSize;

            // Process chunks until there is no file data left
            while (DataStream.Position < eof)
            {
                // Read chunk header
                String id   = ReadString(4);
                uint?  size = Reader.ReadUInt32();

                // Error checking on chunk header
                if (id == null || size == null ||
                    DataStream.Position + size > eof)
                {
                    throw new Exception("Error parsing chunks");
                }

                // Add the chunk to the list
                list.Add(new Chunk(
                    id, (uint)size, (uint)DataStream.Position));

                // Advance the file position by the chunk's size
                DataStream.Position += (long)size;
            }

            // Update the class instance's list of chunks
            Chunks = list.ToArray();
        }

        // Read and verify the RIFF header from the file
        private void ReadHeader()
        {
            // Read header fields
            string id       = ReadString(4);
            uint?  size     = Reader.ReadUInt32();
                   FileType = ReadString(4);

            // Error checking on header
            if (
                id       == null   || // RIFF file identifier
                !id.Equals("RIFF") ||
                size     == null   || // Size of file contents
                FileType == null      // Content type identifier
            )
            {
                throw new Exception("Invalid RIFF file");
            }

            // Update instance members
            FileSize = (uint)size;
        }

    }
}
