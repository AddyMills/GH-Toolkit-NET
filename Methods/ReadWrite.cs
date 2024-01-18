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
using static System.Runtime.InteropServices.JavaScript.JSType;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.QB.QBScript;

/*
 * * ReadWrite
 * * Contains all functions related to reading and writing data to and from files
 * * It is non-static so that it can be instantiated with a specific endianness and game
 * */

namespace GH_Toolkit_Core.Methods
{
    public class ReadWrite
    {
        private readonly bool _flipBytes;
        private readonly string _endian;
        private readonly string _game;
        private readonly Dictionary<string, byte> _qbtype;
        private readonly Dictionary<string, byte> _qbstruct;
        private readonly Dictionary<string, byte> _scriptbytes;
        private readonly ReadWrite _scriptwriter;
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

            // Create a copy of the scriptDict from QBConstants
            _scriptbytes = new Dictionary<string, byte>(QBConstants.scriptDict);

            _scriptwriter = new ReadWrite("little");

            if (_endian == "little" && _game == "GH3")
            {
                if (_scriptbytes.ContainsKey(NOTEQUALS))
                {
                    _scriptbytes[NOTEQUALS] = 0x4C;
                }
            }
        }
        public byte GetScriptByte(string scriptEntry)
        {
            if ((scriptEntry == IF || scriptEntry == ELSE) && _game.StartsWith("GH"))
            {
                scriptEntry = "fast" + scriptEntry;
            }
            return _scriptbytes[scriptEntry];
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
        public static void WriteStringBytes(MemoryStream stream, string str)
        {
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
        public short ReadInt16(MemoryStream stream)
        {
            return unchecked((short)ReadUInt16(stream));
        }
        public int ReadInt32(MemoryStream stream)
        {
            return unchecked((int)ReadUInt32(stream));
        }
        public void WriteStringBytes(MemoryStream s, byte[] data)
        {
            byte[] stringLen = ValueHex((int)data.Length);
            s.Write(stringLen);
            s.Write(data);
        }
        public void WriteAndMaybeFlipBytes(MemoryStream s, byte[] data)
        {
            if (_flipBytes)
            {
                Array.Reverse(data);
            }
            s.Write(data);
        }
        public void WriteNoFlipBytes(MemoryStream s, byte[] data)
        {
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
            PadStreamTo(stream, 4);
        }

        public void PadStreamTo(Stream stream, int padding)
        {
            // Ensure the stream is writable
            if (!stream.CanWrite)
            {
                throw new InvalidOperationException("Stream is not writable.");
            }

            // Calculate the padding needed to make the length divisible by {padding}
            long paddingNeeded = padding - (stream.Length % padding);
            if (paddingNeeded != padding) // If the length is already a multiple of {padding}, paddingNeeded will be {padding}
            {
                AddPaddingToStream(paddingNeeded, stream);
            }
        }

        public void AddPaddingToStream(long paddingNeeded, Stream stream)
        {
            byte[] padding = new byte[paddingNeeded];
            stream.Seek(0, SeekOrigin.End); // Go to the end of the stream
            stream.Write(padding, 0, (int)paddingNeeded); // Write the padding bytes
        }

        public byte[] ValueHex(string text) // For Qb Keys, Qs Keys, and Pointers
        {
            if (text == null)
            {
                text = "0x0";
            }
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
        public byte[] ValueHex(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return ProcessBytes(bytes);
        }
        public byte[] ValueHex(short value)
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
            else if (value is short shortVal)
            {
                return ValueHex(shortVal);
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
                    if (structVal.Items.Count == 0)
                    {
                        byte[] zeroes = ValueHex(0);
                        stream.Write(zeroes, 0, zeroes.Length);
                        return stream.ToArray(); // Empty struct
                    }
                    byte[] firstItem = ValueHex(streamPos + 8);
                    stream.Write(firstItem, 0, firstItem.Length);

                    for (int i = 0; i < structVal.Items.Count; i++)
                    {
                        if (structVal.Items[i] is QBStruct.QBStructItem currItem)
                        {
                            byte[] entryHeader = StructHeader(currItem.Info.Type);
                            stream.Write(entryHeader, 0, 4);
                            byte[] id = ValueHex(currItem.Props.ID);
                            stream.Write(id, 0, 4);
                            //streamPos += 8;
                            var (itemData, otherData) = GetItemData(currItem.Info.Type, currItem.Data, streamPos + (int)stream.Length + 8);

                            stream.Write(itemData, 0, 4);

                            byte[]? nextItem = i == structVal.Items.Count - 1 ? ValueHex(0) : null;
                            int nextPos = streamPos + (int)stream.Length + 4;
                            if (otherData != null)
                            {
                                nextPos += RoundUp(otherData.Length);
                                nextItem = nextItem == null ? ValueHex(nextPos) : nextItem;
                                stream.Write(nextItem, 0, 4);
                                stream.Write(otherData, 0, otherData.Length);
                                //stream.Write(otherData, 0, otherData.Length);
                            }
                            else
                            {
                                nextItem = nextItem == null ? ValueHex(nextPos) : nextItem;
                                stream.Write(nextItem, 0, 4);
                            }
                            PadStreamToFour(stream);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    return stream.ToArray();
                }
            }
            else if (value is QBArray.QBArrayNode arrayVal)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // Write the header
                    byte[] arrayHeader = new byte[] { 0x00, 0x01, _qbtype[arrayVal.FirstItem.Type], 0x00 };
                    stream.Write(arrayHeader, 0, arrayHeader.Length);
                    int itemCount = arrayVal.Items.Count;
                    byte[] countBytes = ValueHex(itemCount);
                    stream.Write(countBytes, 0, countBytes.Length);
                    streamPos += 8;
                    if (itemCount > 1)
                    {
                        streamPos += 4;
                        byte[] firstItem = ValueHex(streamPos);
                        stream.Write(firstItem, 0, firstItem.Length);
                    }
                    string firstItemType = arrayVal.FirstItem.Type;
                    if (itemCount == 0)
                    {
                        stream.Write(countBytes, 0, countBytes.Length);
                    }
                    else if (IsSimpleValue(firstItemType))
                    {
                        foreach (object o in arrayVal.Items)
                        {
                            byte[] entryBytes = ValueHex(o);
                            stream.Write(entryBytes, 0, entryBytes.Length);
                        }
                    }
                    else
                    {
                        int currPointer = streamPos + (arrayVal.Items.Count * 4);

                        using (MemoryStream arrayStream = new MemoryStream())
                        using (MemoryStream pointerStream = new MemoryStream())
                        {
                            for (int i = 0; i < arrayVal.Items.Count; i++)
                            {
                                byte[] pointerBytes = ValueHex(currPointer);
                                pointerStream.Write(pointerBytes, 0, pointerBytes.Length);

                                byte[] entryBytes = ComplexHex(arrayVal.Items[i], firstItemType, currPointer);
                                arrayStream.Write(entryBytes, 0, entryBytes.Length);

                                currPointer += entryBytes.Length;
                            }
                            AppendStream(stream, pointerStream);
                            AppendStream(stream, arrayStream);
                        }
                    }
                    PadStreamToFour(stream);
                    return stream.ToArray();
                }
                throw new NotSupportedException();
            }
            else if (value is QBScript.QBScriptData scriptVal)
            {
                
                Lzss lzss = new Lzss();
                using (MemoryStream mainStream = new MemoryStream())
                using (MemoryStream noCrcStream = new MemoryStream())
                using (MemoryStream scriptStream = new MemoryStream())
                {
                    int loopStart = 0;
                    ScriptLoop(scriptVal.ScriptParsed, ref loopStart, noCrcStream, scriptStream);

                    string scriptCrc = CRC.GenQBKey(noCrcStream.ToArray());
                    mainStream.Write(ValueHex(scriptCrc), 0, 4);
                    byte[] uncompressedScript = scriptStream.ToArray();
                    int decompLen = (int)scriptStream.Length;
                    mainStream.Write(ValueHex(decompLen), 0, 4);
                    byte[] compressedScript = lzss.Compress(scriptStream.ToArray());
                    if (compressedScript.Length >= decompLen)
                    {
                        compressedScript = uncompressedScript;
                    }
                    int compLen = (int)compressedScript.Length;
                    mainStream.Write(ValueHex(compLen), 0, 4);
                    mainStream.Write(compressedScript, 0, compLen);
                    PadStreamToFour(mainStream);
                    byte[] currentContents = mainStream.ToArray();
                    return currentContents;
                }
                throw new NotImplementedException();
            }
            else 
            {
                throw new NotSupportedException(); 
            }
        }
        public void ScriptLoop(List<object> script, ref int scriptPos, MemoryStream noCrcStream, MemoryStream scriptStream)
        {
            for (int i = scriptPos; i < script.Count; i++)
            {
                ScriptToStream(script, ref i, noCrcStream, scriptStream);
            }
        }
        public void ScriptToStream(List<object> script, ref int i, MemoryStream noCrcStream, MemoryStream scriptStream)
        {
            object o = script[i];
            if (o is string scriptString)
            {
                ScriptStringParse(scriptString, script, ref i, noCrcStream, scriptStream);
            }
            else if (o is ScriptNode scriptNode)
            {
                byte scriptType = _scriptbytes[scriptNode.Type];
                AddScriptToStream(scriptType, noCrcStream, scriptStream);
                byte[] scriptBytes = _scriptwriter.GetScriptBytes(scriptNode.Type, scriptNode.DataQb);
                _scriptwriter.AddArrayToStream(scriptBytes, scriptType, noCrcStream, scriptStream);
            }
            else if (o is ScriptTuple scriptTuple)
            {
                byte scriptType = _scriptbytes[scriptTuple.Type];
                AddScriptToStream(scriptType, noCrcStream, scriptStream);
                byte[] scriptBytes = _scriptwriter.GetScriptBytes(scriptTuple.Type, scriptTuple.Data);
                _scriptwriter.AddArrayToStream(scriptBytes, scriptType, noCrcStream, scriptStream);
            }
            else if (o is ScriptNodeStruct scriptStruct)
            {
                byte scriptType = _scriptbytes[STRUCT];
                AddScriptToStream(scriptType, noCrcStream, scriptStream);
                byte[] scriptBytes = GetScriptBytes(STRUCT, scriptStruct.Data); // This can be little or big endian
                AddShortToStream((short)scriptBytes.Length, noCrcStream, scriptStream);
                if (scriptStream.Length % 4 != 0)
                {
                    // For some ungodly reason, structs have to start on a 4-byte boundary, even in scripts!
                    // These blanks have to be added to both the stream and the script CRC
                    long paddingNeeded = 4 - (scriptStream.Length % 4);
                    PadStreamToFour(scriptStream);
                    AddPaddingToStream(paddingNeeded, noCrcStream);
                }
                _scriptwriter.AddArrayToStream(scriptBytes, scriptType, noCrcStream, scriptStream);
            }
            else if (o is ConditionalCollection scriptConditional)
            {
                scriptConditional.WriteToStream(noCrcStream, scriptStream, this);
            }
            else if (o is SwitchNode switchNode)
            {
                switchNode.WriteToStream(script, noCrcStream, scriptStream, this);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void ScriptStringParse(string scriptString, 
            List<object> script, 
            ref int scriptPos, 
            MemoryStream crcStream, 
            MemoryStream scriptStream
            )
        {
            switch (scriptString)
            {
                case SWITCH:
                    scriptPos += 1;
                    SwitchNode switchNode = new SwitchNode(script, ref scriptPos);
                    switchNode.WriteToStream(script, crcStream, scriptStream, this);
                    scriptPos -= 1;
                    break;
                case IF:
                    scriptPos += 1;
                    ConditionalCollection conditional = new ConditionalCollection(script, ref scriptPos);
                    conditional.WriteToStream(crcStream, scriptStream, this);
                    scriptPos -= 1;
                    break;
                case RANDOM:
                case RANDOM2:
                case RANDOMNOREPEAT:
                case RANDOMPERMUTE:
                    scriptPos += 1;
                    ScriptRandom scriptRandom = new ScriptRandom(scriptString, script, ref scriptPos);
                    scriptRandom.WriteToStream(crcStream, scriptStream, this);
                    scriptPos -= 1;
                    break;
                default:
                    AddScriptToStream(_scriptbytes[scriptString], crcStream, scriptStream);
                    break;
            }

        }
        public void AddScriptToStream(byte scriptByte, MemoryStream noCrcStream, MemoryStream scriptStream)
        {
            if (scriptByte != NEWLINE_BYTE && scriptByte != ENDSCRIPT_BYTE)
            {
                noCrcStream.WriteByte(scriptByte);
            }
            scriptStream.WriteByte(scriptByte);
            /*if (scriptByte == NEXTGLOBAL_BYTE)
            {
                noCrcStream.WriteByte(QBKEY_BYTE);
                scriptStream.WriteByte(QBKEY_BYTE);
            }*/
        }
        public void AddShortToStream(short shortVal, MemoryStream noCrcStream, MemoryStream scriptStream)
        {
            byte[] shortBytes = _scriptwriter.ValueHex(shortVal);
            noCrcStream.Write(shortBytes, 0, shortBytes.Length);
            scriptStream.Write(shortBytes, 0, shortBytes.Length);
        }
        public void AddIntToStream(int intVal, MemoryStream noCrcStream, MemoryStream scriptStream)
        {
            byte[] intBytes = _scriptwriter.ValueHex(intVal);
            noCrcStream.Write(intBytes, 0, intBytes.Length);
            scriptStream.Write(intBytes, 0, intBytes.Length);
        }
        public void AddArrayToStream(byte[] scriptBytes, byte type, MemoryStream noCrcStream, MemoryStream scriptStream)
        {
            switch (type)
            {
                case STRING_BYTE:
                case WIDESTRING_BYTE:
                    WriteStringBytes(noCrcStream, scriptBytes);
                    WriteStringBytes(scriptStream, scriptBytes);
                    break;
                default:
                    WriteAndMaybeFlipBytes(noCrcStream, scriptBytes);
                    WriteAndMaybeFlipBytes(scriptStream, scriptBytes);
                    break;
            }
        }
        public byte[] GetScriptBytes(string type, object data)
        {
            var(itemData, otherData) = GetItemData(type, data, 0);
            if (type == PAIR || type == VECTOR)
            {
                byte[] newArray = new byte[otherData.Length - 4];
                Array.Copy(otherData, 4, newArray, 0, otherData.Length - 4);
                return newArray;
            }
            return otherData == null ? itemData : otherData;
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
        public byte[] StructHeader(string type)
        {
            byte flags;
            byte qbType;
            if (_game == "GH3")
            {
                flags =  _endian == "big" ? (byte)(_qbstruct[type] + FLAG_STRUCT_GH3) : _qbstruct[type];
                qbType = 0x00;
            }
            else
            {
                flags = 0x01;
                qbType = _qbstruct[type];
            }
            byte[] bytes = new byte[] { 0x00, flags, qbType, 0x00 };
            return bytes;
        }
        public static int RoundUp(int num)
        {
            return (int)Math.Ceiling(num / 4.0) * 4;
        }
        static void AppendStream(MemoryStream target, MemoryStream source)
        {
            // Check for null streams
            if (target == null || source == null)
                throw new ArgumentNullException("Streams cannot be null");

            // Reset the position of the source stream to ensure all of its content is copied
            source.Position = 0;

            // Copy the source stream into the target stream
            source.CopyTo(target);
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
