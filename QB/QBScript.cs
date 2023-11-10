using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBConstants;
using System.Diagnostics;
using System.Data.SqlTypes;
using GH_Toolkit_Core.Methods;
using System.IO;
using GH_Toolkit_Core.Debug;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using GH_Toolkit_Core.Checksum;

namespace GH_Toolkit_Core.QB
{
    public class QBScript
    {
        public static ReadWrite ScriptReader = new ReadWrite("little"); // Scripts are always little endian, but qbkeys within structs are not...
        [DebuggerDisplay("{ScriptSize} bytes ({CompressedSize} compressed)")]
        public class QBScriptData
        {
            public string ScriptCRC { get; set; }
            public uint ScriptSize { get; set; }
            public uint CompressedSize { get; set; }
            public byte[] CompressedData { get; set; }
            public byte[] ScriptData { get; set; }
            public List<object> ScriptParsed { get; set; }
            public QBScriptData(MemoryStream stream)
            { 
                ScriptCRC = ReadQBKey(stream);
                ScriptSize = Reader.ReadUInt32(stream);
                CompressedSize = Reader.ReadUInt32(stream);
                CompressedData = ReadCompScript(stream, (int)CompressedSize);
                if (ScriptSize != CompressedSize)
                {
                    Lzss lz = new Lzss();
                    ScriptData = lz.Decompress(CompressedData);
                }
                else
                {
                    ScriptData = CompressedData;
                }
                /*
                 * Trying to RE the script CRC value
                byte[] tryArray = new byte[ScriptSize];
                for (int i = 0; i < ScriptSize; i++)
                {
                    tryArray[i] = ScriptData[i];
                }
                byte[] tryArray2 = new byte[CompressedSize];
                for (int i = 0; i < CompressedSize; i++)
                {
                    tryArray2[i] = CompressedData[i];
                }
                string tryCrc = CRC.GenQBKey(tryArray);
                string tryCrc2 = CRC.GenQBKey(tryArray2);
                */
                ScriptParsed = ParseScript(ScriptData);
            }
        }
        [DebuggerDisplay("{Type,nq} - {Data,nq}")]
        public class ScriptNode
        {
            public string Type { get; set; }
            public object Data { get; set; }
            public ScriptNode(string nodeType, object data)
            {
                Type = nodeType;
                Data = data;
            }
        }
        [DebuggerDisplay("{Data}")]
        public class ScriptNodeStruct
        {
            public uint ByteSize { get; set; }
            public QBStruct.QBStructData Data { get; set; }
            public ScriptNodeStruct(MemoryStream stream)
            {
                ByteSize = ScriptReader.ReadUInt16(stream);
                ReadWrite.MoveToModFour(stream);
                byte[] buffer = new byte[ByteSize];
                stream.Read(buffer, 0, buffer.Length);
                using (MemoryStream structStream = new MemoryStream(buffer))
                {
                    Data = new QBStruct.QBStructData(structStream);
                }
                
            }
        }
        [DebuggerDisplay("{Name}")]
        public class Conditional
        {
            public string Name { get; set; }
            public uint Jump { get; set; }
            public Conditional(string name, uint jump)
            {
                Name = name;
                Jump = jump;
            }
        }
        [DebuggerDisplay("{Type, nq} - {Text, nq}")]
        public class ScriptString
        {
            public string Type { get; set; }
            public string Text { get; set; }
            public ScriptString(string name, uint length, MemoryStream stream)
            {
                Type = name;
                byte[] buffer = new byte[length];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                switch (Type)
                {
                    case STRING:
                        Text = Encoding.UTF8.GetString(buffer);
                        break;
                    case WIDESTRING:
                        Text = Encoding.BigEndianUnicode.GetString(buffer); 
                        break;
                }
                
            }
        }
        [DebuggerDisplay("{Type, nq} - {ListDisplay, nq}")]
        public class ScriptTuple
        {
            public string Type { get; set; }
            public List<float> Data { get; set; }
            public ScriptTuple(string type, MemoryStream stream)
            {
                Type = type;
                uint floats = (uint)(type == PAIR ? 2 : 3);
                Data = new List<float>();
                for (int i = 0; i < floats; i++)
                {
                    Data.Add(ScriptReader.ReadFloat(stream));
                }
            }
            private string ListDisplay
            {
                get { return string.Join(", ", Data); }
            }
        }
        [DebuggerDisplay("{Name} - {Entries} Entries")]
        public class ScriptRandom
        {
            public string Name { get; set; }
            public uint Entries { get; set; }
            public ScriptRandom(string random, MemoryStream stream)
            {
                Name = random;
                Entries = ScriptReader.ReadUInt32(stream);
                stream.Position += (Entries * 2); // Skip weights
                stream.Position += (Entries * 4); // Skip offsets
            }
        }
        [DebuggerDisplay("Long Jump - {Jump} Bytes")]
        public class ScriptLongJump
        { 
            public uint Jump { get; set; }
            public ScriptLongJump(uint jump) 
            {
                Jump = jump;
            }
        }
        private static byte[] ReadCompScript(MemoryStream stream, int size)
        {
            byte[] buffer = new byte[size];
            stream.Read(buffer, 0, size);
            return buffer;
        }
        public static string ReadScriptString(string name, uint length, MemoryStream stream)
        {
            byte[] buffer = new byte[length];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            switch (name)
            {
                case STRING:
                    return Encoding.UTF8.GetString(buffer);
                case WIDESTRING:
                    return Encoding.BigEndianUnicode.GetString(buffer);
                default:
                    throw new Exception("Unknown string type found.");
            }

        }
        public static string ReadScriptQBKey(MemoryStream stream)
        {
            return DebugReader.DebugCheck(SongHeaders, ScriptReader.ReadUInt32(stream));
        }
        private static List<object> ParseScript(byte[] script)
        {
            List<object> list = new List<object>();
            bool nextGlobal = false;
            bool nextArg = false;
            using (MemoryStream stream = new MemoryStream(script))
            {
                while (stream.Position < stream.Length)
                {
                    var scriptByte = Reader.ReadUInt8(stream);
                    uint length;
                    switch (scriptByte)
                    {
                        case 0x01:
                            list.Add("Newline"); // New line
                            break;
                        case 0x03:
                            list.Add("{");
                            break;
                        case 0x04:
                            list.Add("}");
                            break;
                        case 0x05:
                            list.Add("[");
                            break;
                        case 0x06:
                            list.Add("]");
                            break;
                        case 0x07:
                            list.Add("=");
                            break;
                        case 0x08:
                            list.Add(".");
                            break;
                        case 0x09:
                            list.Add(",");
                            break;
                        case 0x0A:
                            list.Add("-");
                            break;
                        case 0x0B:
                            list.Add("+");
                            break;
                        case 0x0C:
                            list.Add("/");
                            break;
                        case 0x0D:
                            list.Add("*");
                            break;
                        case 0x0E:
                            list.Add("(");
                            break;
                        case 0x0F:
                            list.Add(")");
                            break;
                        case 0x12:
                            list.Add("<");
                            break;
                        case 0x13:
                            list.Add("<=");
                            break;
                        case 0x14:
                            list.Add(">");
                            break;
                        case 0x15:
                            list.Add(">=");
                            break;
                        case 0x16:
                            if (nextGlobal)
                            {
                                list.Add(new ScriptNode(POINTER, ReadScriptQBKey(stream)));
                                nextGlobal = false;
                            }
                            else
                            {
                                list.Add(new ScriptNode(QBKEY, ReadScriptQBKey(stream)));
                            }
                            break;
                        case 0x17:
                            list.Add(new ScriptNode(INTEGER, ScriptReader.ReadUInt32(stream)));
                            break;
                        case 0x1A:
                            list.Add(new ScriptNode(FLOAT, ScriptReader.ReadFloat(stream)));
                            break;
                        case 0x1B:
                            length = ScriptReader.ReadUInt32(stream);
                            list.Add(new ScriptNode(STRING, ReadScriptString(STRING, length, stream)));
                            break;
                        case 0x1E:
                            list.Add(new ScriptTuple(VECTOR, stream));
                            break;
                        case 0x1F:
                            list.Add(new ScriptTuple(PAIR, stream));
                            break;
                        case 0x20:
                            list.Add("begin"); // Loop
                            break;
                        case 0x21:
                            list.Add("repeat");
                            break;
                        case 0x22:
                            list.Add("break");
                            break;
                        case 0x24:
                            list.Add("endscript");
                            break;
                        case 0x27:
                            uint nextComp = ScriptReader.ReadUInt16(stream); // This is either another else if or else. It can also be an endif if there are no more comparisons
                            uint lastComp = ScriptReader.ReadUInt16(stream); // I think this is the last byte before the end of the else if statement
                            list.Add("elsef");
                            break;
                        case 0x28:
                            list.Add("endif");
                            break;
                        case 0x29:
                            list.Add("return");
                            break;
                        case 0x2C:
                            list.Add("<...>"); // All Args
                            break;
                        case 0x2D:
                            list.Add("Argument"); // surround next item in <> when parsing
                            break;
                        case 0x2E:
                            list.Add(new ScriptLongJump(ScriptReader.ReadUInt32(stream)));
                            break;
                        case 0x2F:
                            list.Add(new ScriptRandom("Random", stream));
                            break;
                        case 0x30:
                            list.Add("randomrange"); // Random Range?
                            break;
                        case 0x32:
                            list.Add("||");
                            break;
                        case 0x33:
                            list.Add("&&");
                            break;
                        case 0x39:
                            list.Add("NOT");
                            break;
                        case 0x3A:
                            list.Add("AND");
                            break;
                        case 0x3B:
                            list.Add("OR");
                            break;
                        case 0x3C:
                            list.Add("switch");
                            break;
                        case 0x3D:
                            list.Add("endswitch");
                            break;
                        case 0x3E:
                            list.Add("case");
                            break;
                        case 0x3F:
                            list.Add("default");
                            break;
                        case 0x40:
                            list.Add(new ScriptRandom("RandomNoRepeat", stream));
                            break;
                        case 0x42:
                            list.Add(":");
                            break;
                        case 0x47:
                            list.Add(new Conditional(FASTIF, ScriptReader.ReadUInt16(stream)));
                            break;
                        case 0x48:
                            list.Add(new Conditional(FASTELSE, ScriptReader.ReadUInt16(stream)));
                            break;
                        case 0x49:
                            stream.Position += 2;
                            break;
                        case 0x4A:
                            list.Add(new ScriptNode(STRUCT, new ScriptNodeStruct(stream)));
                            ReadWrite.MoveToModFour(stream);
                            break;
                        case 0x4B: // This byte makes the next QbKey a Pointer instead
                            nextGlobal = true;
                            break;
                        case 0x4C:
                            length = ScriptReader.ReadUInt32(stream);
                            list.Add(new ScriptNode(WIDESTRING, ReadScriptString(WIDESTRING, length, stream)));
                            break;
                        case 0x4D:
                            list.Add("!=");
                            break;
                        case 0x4E:
                            list.Add(new ScriptNode(QSKEY, ReadScriptQBKey(stream)));
                            break;
                        case 0x4F:
                            list.Add("RandomFloat");
                            break;
                        case 0x50:
                            list.Add("RandomInteger");
                            break;
                        default:
                            throw new Exception("Not supported");
                    }
                }
            }
            return list;
        }
    }
}
