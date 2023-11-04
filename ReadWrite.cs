using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core
{
    public class ReadWrite
    {
        private readonly bool _flipBytes;
        public ReadWrite(string endian)
        {
            // Determine if bytes need to be flipped based on endianness and system architecture.
            _flipBytes = (endian == "little") != BitConverter.IsLittleEndian;
        }
        public static bool FlipCheck(string endian)
        {
            return (endian == "little") != BitConverter.IsLittleEndian;
        }
        public static string ReadUntilNullByte(MemoryStream memoryStream)
        {
            List<byte> byteList = new List<byte>();
            int currentByte;

            // Read byte by byte
            while ((currentByte = memoryStream.ReadByte()) != -1) // -1 means end of stream
            {
                // Break if currentByte is null byte
                if (currentByte == 0)
                    break;

                byteList.Add((byte)currentByte);
            }

            // Convert byte list to string using UTF-8 encoding
            return Encoding.UTF8.GetString(byteList.ToArray());
        }
        public static void WriteNullTermString(MemoryStream stream, string str)
        {
            str += "\0";
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            stream.Write(byteArray, 0, byteArray.Length);
        }
        public static void FillNullTermString(MemoryStream stream, uint padding)
        {
            byte[] nullBytes = new byte[padding];
            stream.Write(nullBytes, 0, nullBytes.Length);
        }
        public static byte[] ReadNoFlip(MemoryStream s, int count)
        {
            byte[] buffer = new byte[count];
            s.Read(buffer, 0, count);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }
        public byte[] ReadAndMaybeFlipBytes(MemoryStream s, int count)
        {
            byte[] buffer = new byte[count];
            s.Read(buffer, 0, count);
            if (_flipBytes)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }
        public uint ReadUInt8(MemoryStream stream)
        {
            return ReadAndMaybeFlipBytes(stream, 1)[0];
        }
        public uint ReadUInt16(MemoryStream stream)
        {
            return BitConverter.ToUInt16(ReadAndMaybeFlipBytes(stream, 2), 0);
        }
        public uint ReadUInt32(MemoryStream stream)
        {
            return BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, 4), 0);
        }
        public float ReadFloat(MemoryStream stream)
        {
            return BitConverter.ToSingle(ReadAndMaybeFlipBytes(stream, 4), 0);
        }
        public void WriteAndMaybeFlipBytes(MemoryStream s, byte[] data)
        {
            if (_flipBytes)
            {
                Array.Reverse(data);
            }
            s.Write(data);
        }
        public void WriteUInt32(MemoryStream stream, uint data)
        {
            WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes((uint)data));
        }
        public static void CopyStreamClose(MemoryStream source, MemoryStream dest)
        {
            source.Seek(0, SeekOrigin.Begin);
            source.CopyTo(dest);
            source.Close();
        }
    }
}
