using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.PS2
{
    public class HED
    {
        [DebuggerDisplay("Entry: {SectorIndex}, {FileSize}, {FilePath}")]
        public class HedEntry
        {
            public uint SectorIndex { get; set; }
            public uint FileSize { get; set; }
            public string? FilePath { get; set; }
        }
        public static List<HedEntry> ReadHEDFile(byte[] HedBytes)
        {
            // PS2 files are always little-endian
            bool flipBytes = Readers.FlipCheck("little");
            const int UnitSize = 4;
            // There are no headers in a HED file so go right into reading the data
            MemoryStream stream = new MemoryStream(HedBytes);
            List<HedEntry> HedList = new List<HedEntry>();
            uint checkf = 1;
            uint checkend = 1;
            uint originalPosition = (uint)stream.Position;
            while (checkf != 0xff || checkend != 0xffffffff)
            {
                if (originalPosition % 4 != 0)
                {
                    stream.Position = originalPosition + 1;
                }
                else
                {
                    HedEntry entry = new HedEntry();
                    entry.SectorIndex = Readers.ReadUInt32(stream, flipBytes);
                    entry.FileSize = Readers.ReadUInt32(stream, flipBytes);
                    entry.FilePath = Readers.ReadUntilNullByte(stream);
                    HedList.Add(entry);
                }
                originalPosition = (uint)stream.Position;
                checkf = Readers.ReadUInt8(stream, flipBytes);
                stream.Seek(originalPosition, SeekOrigin.Begin);
                checkend = Readers.ReadUInt32(stream, flipBytes);
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
            stream.Close();
            return HedList;
        }
        
    }
}
