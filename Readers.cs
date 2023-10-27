using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core
{
    public class Readers
    {
        public static bool FlipCheck(string endian)
        {
            bool big_endian;
            bool little_arc;
            if (endian == "little")
            {
                big_endian = false;
            }
            else
            {
                big_endian = true;
            }
            if (BitConverter.IsLittleEndian)
            {
                little_arc = true;
            }
            else
            {
                little_arc = false;
            }
            return big_endian && little_arc;
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

        public static byte[] ReadAndMaybeFlipBytes(MemoryStream s, int count, bool flipBytes)
        {
            byte[] buffer = new byte[count];
            s.Read(buffer, 0, count);
            if (flipBytes)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }
        public static uint ReadUInt8(MemoryStream stream, bool flipBytes)
        {
            return ReadAndMaybeFlipBytes(stream, 1, flipBytes)[0];
        }
        public static uint ReadUInt16(MemoryStream stream, bool flipBytes)
        {
            return BitConverter.ToUInt16(ReadAndMaybeFlipBytes(stream, 2, flipBytes), 0);
        }
        public static uint ReadUInt32(MemoryStream stream, bool flipBytes)
        {
            return BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, 4, flipBytes), 0);
        }
    }
}
