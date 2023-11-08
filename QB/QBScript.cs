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
                Text = Encoding.UTF8.GetString(buffer);
            }
        }
        private static byte[] ReadCompScript(MemoryStream stream, int size)
        {
            byte[] buffer = new byte[size];
            stream.Read(buffer, 0, size);
            return buffer;
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
                    switch (scriptByte)
                    {
                        case 0x01:
                            list.Add("Newline");
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
                            list.Add(new ScriptNode(INTEGER, Reader.ReadUInt32(stream)));
                            break;
                        case 0x1A:
                            list.Add(new ScriptNode(FLOAT, Reader.ReadFloat(stream)));
                            break;
                        case 0x1B:
                            uint length = ScriptReader.ReadUInt32(stream);
                            list.Add(new ScriptString(STRING, length, stream));
                            break;
                        case 0x24:
                            list.Add("EndScript");
                            break;
                        case 0x28:
                            list.Add("Endif");
                            break;
                        case 0x29:
                            list.Add("Return");
                            break;
                        case 0x2C:
                            list.Add("<...>"); // All Args
                            break;
                        case 0x2D:
                            list.Add("Argument");
                            break;
                        case 0x47:
                            list.Add(new Conditional(FASTIF, ScriptReader.ReadUInt16(stream)));
                            break;
                        case 0x48:
                            list.Add(new Conditional(FASTELSE, ScriptReader.ReadUInt16(stream)));
                            break;
                        case 0x4A:
                            list.Add(new ScriptNode(STRUCT, new ScriptNodeStruct(stream)));
                            ReadWrite.MoveToModFour(stream);
                            break;
                        case 0x4B:
                            nextGlobal = true;
                            //list.Add("Argument Pack (Global)");
                            break;
                        default:
                            throw new Exception("Not supported");
                    }
                }

            }
            return list;
        }
        private static object GetScriptItem(uint scriptByte, MemoryStream stream)
        {
            return 0;
        }
    }
}
