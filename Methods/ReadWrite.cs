using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Checksum;
using GH_Toolkit_Core.QB;
using static GH_Toolkit_Core.QB.QBConstants;

namespace GH_Toolkit_Core.Methods
{
    public class ReadWrite
    {
        private readonly bool _flipBytes;
        private readonly string _endian;
        private readonly string _game;
        private readonly Dictionary<string, byte> _qbtype;
        private readonly Dictionary<string, byte> _qbstruct;
        public ReadWrite(string endian)
        {
            // Determine if bytes need to be flipped based on endianness and system architecture.
            _flipBytes = endian == "little" != BitConverter.IsLittleEndian;
            _endian = endian;
        }
        public ReadWrite(string endian, string game, Dictionary<string, byte> QbTypeLookup, Dictionary<string, byte> QbStructLookup)
        {
            // Determine if bytes need to be flipped based on endianness and system architecture.
            _flipBytes = endian == "little" != BitConverter.IsLittleEndian;
            _endian = endian;
            _game = game;
            _qbtype = QbTypeLookup;
            _qbstruct = QbStructLookup;
        }
        public static bool FlipCheck(string endian)
        {
            return endian == "little" != BitConverter.IsLittleEndian;
        }
        public byte[] ProcessBytes(byte[] bytes)
        {
            if (_flipBytes)
            {
                Array.Reverse(bytes);
            }
            return bytes;
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
        public byte[] GetFloatBytes(float f)
        {
            return BitConverter.GetBytes(f);
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
        public void PadStreamToFour(Stream stream)
        {
            // Ensure the stream is writable
            if (!stream.CanWrite)
            {
                throw new InvalidOperationException("Stream is not writable.");
            }

            // Calculate the padding needed to make the length divisible by 4
            long paddingNeeded = 4 - (stream.Length % 4);
            if (paddingNeeded != 4) // If the length is already a multiple of 4, paddingNeeded will be 4
            {
                byte[] padding = new byte[paddingNeeded];
                stream.Seek(0, SeekOrigin.End); // Go to the end of the stream
                stream.Write(padding, 0, (int)paddingNeeded); // Write the padding bytes
            }
        }

        public byte[] ValueHex(string text) // For Qb Keys, Qs Keys, and Pointers
        {
            var qbKey = CRC.QBKey(text);
            var qbKeyInt = Convert.ToInt32(qbKey, 16);
            var bytes = BitConverter.GetBytes(qbKeyInt);
            return ProcessBytes(bytes);
        }
        public byte[] ValueHex(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return ProcessBytes(bytes);
        }
        public byte[] ValueHex(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return ProcessBytes(bytes);
        }
        public byte[] ValueHex(object value)
        {
            if (value is int intVal)
            {
                return ValueHex(intVal);
            }
            else if (value is float floatVal)
            {
                return ValueHex(floatVal);
            }
            else if (value is string stringVal) 
            {
                return ValueHex(stringVal);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public byte[] ComplexHex(object value, string valueType, int streamPos = 0)
        {
            if (value is string strVal)
            {
                if (!strVal.EndsWith('\0'))
                {
                    strVal += '\0';
                }
                switch (valueType)
                {
                    case STRING:
                        return Encoding.UTF8.GetBytes(strVal);
                    case WIDESTRING:
                        return Encoding.BigEndianUnicode.GetBytes(strVal);
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (value is List<float> floatsVal)
            {
                int initialCapacity = 4 + (4 * floatsVal.Count); // 4 bytes for header + 4 bytes for each float
                using (MemoryStream stream = new MemoryStream(initialCapacity))
                {
                    // Write the header
                    byte[] floatsHeader = new byte[] { 0x00, 0x01, 0x00, 0x00 };
                    stream.Write(floatsHeader, 0, floatsHeader.Length);

                    // Write each float value
                    foreach (float f in floatsVal)
                    {
                        WriteAndMaybeFlipBytes(stream, GetFloatBytes(f));
                    }

                    return stream.ToArray();
                }
            }
            else if (value is QBStruct.QBStructData structVal)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // Write the header
                    byte[] structHeader = new byte[] { 0x00, 0x00, 0x01, 0x00 };
                    stream.Write(structHeader, 0, structHeader.Length);
                    streamPos += 8; // Position of first item in struct
                    byte[] firstItem = ValueHex(streamPos);
                    stream.Write(firstItem, 0, firstItem.Length);
                    for (int i = 0; i < structVal.Items.Count; i++)
                    {
                        if (structVal.Items[i] is QBStruct.QBStructItem currItem)
                        {
                            byte[] entryHeader;
                            byte[] id = ValueHex(currItem.Props.ID);
                            //var (itemData, otherData) = GetItemData(currItem.Info.Type, currItem.Data);
                        }
                        

                    }
                }
            }
            throw new NotSupportedException();
        }
        public (byte[] itemData, byte[]? otherData) GetItemData(string type, object data, int streamPos)
        {
            if (IsSimpleValue(type))
            {
                return (ValueHex(data), null);
            }
            else
            {
                byte[] itemData = ValueHex(streamPos);
                byte[] otherData = ComplexHex(data, type, streamPos);
                return (itemData, otherData);
            }
        }
        public static byte[] HexStringToByteArray(string hex)
        {
            string[] hexValues = hex.Split(' ');
            byte[] bytes = new byte[hexValues.Length];

            for (int i = 0; i < hexValues.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexValues[i], 16);
            }

            return bytes;
        }
        public static bool IsSimpleValue(string info)
        {
            switch (info)
            {
                case FLOAT:
                case INTEGER:
                case POINTER:
                case QBKEY:
                case QSKEY:
                    return true;
            }

            return false;
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
        public string Endian()
        {
            return _endian;
        }
    }
}
