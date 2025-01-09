using System.Diagnostics;
using System.Text;
using GH_Toolkit_Core.Methods;
//using Ionic.Zlib;
using System.IO.Compression;
using static GH_Toolkit_Core.PAK.PAK;

/*
 * This file is intended to be a collection of custom methods to compress and decompress PAK files
 * Script compression is handled in the QB namespace with the LZSS class
 * 
 * 
 * 
 */

namespace GH_Toolkit_Core.PAK
{
    public class Compression
    {
        public static bool isChnkCompressed(byte[] data)
        {
            ReadOnlySpan<byte> magicBytes = new byte[] { (byte)'C', (byte)'H', (byte)'N', (byte)'K' };
            ReadOnlySpan<byte> dataSpan = new ReadOnlySpan<byte>(data, 0, 4);
            return dataSpan.SequenceEqual(magicBytes);
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
            // Compressed PAK files are always 360/PS3 and thus always big-endian
            ReadWrite reader = new ReadWrite("big");
            using (MemoryStream stream = new MemoryStream(compData)) 
            {
                List<ChnkEntry> ChnkList = new List<ChnkEntry>();
                List<byte[]> decompressedDataList = new List<byte[]>();
                while (true)
                {
                    uint baseOffset = (uint)stream.Position;
                    byte[] buffer = ReadWrite.ReadNoFlip(stream, 4);
                    string magic = Encoding.UTF8.GetString(buffer);
                    ChnkEntry entry = new ChnkEntry();
                    entry.Offset = reader.ReadUInt32(stream) + baseOffset;
                    entry.CompSize = reader.ReadUInt32(stream);
                    entry.NextChnkOffset = reader.ReadUInt32(stream);
                    entry.NextChnkLength = reader.ReadUInt32(stream);
                    entry.DecompSize = reader.ReadUInt32(stream);
                    entry.DecompOffset = reader.ReadUInt32(stream);
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
        }

        public static byte[] DecompressData(byte[] compressedChunk)
        {
            /*
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
            */
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
