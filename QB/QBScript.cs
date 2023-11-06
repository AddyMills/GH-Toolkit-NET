using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.Lzss;
using System.Diagnostics;

namespace GH_Toolkit_Core.QB
{
    public class QBScript
    {
        [DebuggerDisplay("{ScriptSize} bytes ({CompressedSize} compressed)")]
        public class QBScriptData
        {
            public string ScriptCRC { get; set; }
            public uint ScriptSize { get; set; }
            public uint CompressedSize { get; set; }
            public byte[] CompressedData { get; set; }
            public byte[] ScriptData { get; set; }
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
            }
        }
        private static byte[] ReadCompScript(MemoryStream stream, int size)
        {
            byte[] buffer = new byte[size];
            stream.Read(buffer, 0, size);
            return buffer;
        }
        private static void ParseScript(byte[] script)
        {

        }
    }
}
