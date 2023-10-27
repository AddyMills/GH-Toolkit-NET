using System.Diagnostics;
using System.Text;
using Ionic.Zlib;
using static GH_Toolkit_Core.PAK;

namespace GH_Toolkit_Core
{
    public class Compression
    {
        public static bool isCompressed(byte[] data)
        {
            if (data.Length >= 4 &&
                data[0] == (byte)'C' &&
                data[1] == (byte)'H' &&
                data[2] == (byte)'N' &&
                data[3] == (byte)'K')
            {
                return true;
            }
            return false;
        }
        [DebuggerDisplay("CHNK - Compressed: {CompSize} Decompressed: {DecompSize}")]
        public class ChnkEntry
        {
            public uint Offset { get; set; }
            public uint CompSize { get; set; }
            public uint NextChnkOffset { get; set; }
            public uint NextChnkLength { get; set; }
            public uint DecompSize { get; set; }
            public uint DecompOffset { get; set; }
        }

        public static byte[] DecompressWTPak(byte[] compData)
        {
            const int UnitSize = 4;    // Size of each unit in one entry
            // Compressed PAK files are always 360/PS3 and thus always big-endian
            bool flipBytes = Readers.FlipCheck("big");
            MemoryStream stream = new MemoryStream(compData);
            List<ChnkEntry> ChnkList = new List<ChnkEntry>();
            List<byte[]> decompressedDataList = new List<byte[]>();
            while (true)
            {
                uint baseOffset = (uint)stream.Position;
                byte[] buffer = Readers.ReadAndMaybeFlipBytes(stream, 4, false);
                string magic = Encoding.UTF8.GetString(buffer);
                ChnkEntry entry = new ChnkEntry();
                entry.Offset = Readers.ReadUInt32(stream, flipBytes) + baseOffset;
                entry.CompSize = Readers.ReadUInt32(stream, flipBytes);;
                entry.NextChnkOffset = Readers.ReadUInt32(stream, flipBytes);;
                entry.NextChnkLength = Readers.ReadUInt32(stream, flipBytes);;
                entry.DecompSize = Readers.ReadUInt32(stream, flipBytes);;
                entry.DecompOffset = Readers.ReadUInt32(stream, flipBytes);;
                ChnkList.Add(entry);

                // Decompress the current chunk
                byte[] compressedChunk = new byte[entry.CompSize];
                stream.Position = entry.Offset;
                stream.Read(compressedChunk, 0, (int)entry.CompSize);

                byte[] decompressedChunk = DecompressData(compressedChunk);
                decompressedDataList.Add(decompressedChunk); // Save the decompressed data

                if (entry.NextChnkOffset != 0xffffffff)
                {
                    stream.Position = baseOffset + entry.NextChnkOffset;
                }
                else
                {
                    break;
                }  
            }
            // Combine all decompressed chunks into one byte array
            int totalSize = decompressedDataList.Sum(arr => arr.Length);
            byte[] result = new byte[totalSize];
            int offset = 0;
            foreach (byte[] chunk in decompressedDataList)
            {
                Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }

        public static byte[] DecompressData(byte[] compressedChunk)
        {
            using MemoryStream compressedStream = new MemoryStream(compressedChunk);
            using DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using MemoryStream decompressedStream = new MemoryStream();

            byte[] buffer = new byte[4096];  // Adjust buffer size as necessary
            int bytesRead;
            while ((bytesRead = deflateStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                decompressedStream.Write(buffer, 0, bytesRead);
            }
            return decompressedStream.ToArray();
        }


    }
}
