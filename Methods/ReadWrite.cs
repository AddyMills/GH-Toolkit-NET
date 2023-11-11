using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Methods
{
    public class ReadWrite
    {
        private readonly bool _flipBytes;
        public ReadWrite(string endian)
        {
            // Determine if bytes need to be flipped based on endianness and system architecture.
            _flipBytes = endian == "little" != BitConverter.IsLittleEndian;
        }
        public static bool FlipCheck(string endian)
        {
            return endian == "little" != BitConverter.IsLittleEndian;
        }
        private static void Swap(ref int first, ref int second)
        {
            int temp = first;
            first = second;
            second = temp;
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
        public static string ReadWideString(MemoryStream memoryStream, string endian = "little")
        {
            List<byte> byteList = new List<byte>();
            int firstByte, secondByte;

            // Read two bytes at a time
            while ((secondByte = memoryStream.ReadByte()) != -1)
            {
                firstByte = memoryStream.ReadByte();
                if (firstByte == -1)
                {
                    throw new InvalidDataException("Stream ends with a single byte, which is not valid for UTF-16 encoding.");
                }

                // Check for null terminator (depends on endianness)
                bool shouldFlip = FlipCheck(endian);
                if (shouldFlip)
                {
                    Swap(ref firstByte, ref secondByte);
                }

                if (firstByte == 0 && secondByte == 0)
                {
                    break; // Null terminator found.
                }

                byteList.Add((byte)firstByte);
                byteList.Add((byte)secondByte);
            }

            // Convert byte list to string using Unicode encoding (UTF-16)
            return Encoding.Unicode.GetString(byteList.ToArray());
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
        public int ReadInt32(MemoryStream stream)
        {
            return unchecked((int)ReadUInt32(stream));
        }
        public void WriteAndMaybeFlipBytes(MemoryStream s, byte[] data)
        {
            if (_flipBytes)
            {
                Array.Reverse(data);
            }
            s.Write(data);
        }
        public static void MoveToModFour(MemoryStream stream)
        {
            long currentPosition = stream.Position;
            long remainder = currentPosition % 4;

            if (remainder == 0)
            {
                // The current position is already divisible by 4
                return;
            }

            // Calculate the next nearest position divisible by 4
            long newPosition = currentPosition + (4 - remainder);

            // Ensure the new position does not exceed the length of the stream.
            // Depending on your requirements, you might want to handle this situation differently.
            newPosition = Math.Min(newPosition, stream.Length);

            // Set the new position
            stream.Position = newPosition;
        }
        public void WriteUInt32(MemoryStream stream, uint data)
        {
            WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(data));
        }
        public static void CopyStreamClose(MemoryStream source, MemoryStream dest)
        {
            source.Seek(0, SeekOrigin.Begin);
            source.CopyTo(dest);
            source.Close();
        }
    }
}
