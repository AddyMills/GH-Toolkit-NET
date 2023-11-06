using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Methods;

namespace GH_Toolkit_Core.PS2
{
    public class HED
    {
        [DebuggerDisplay("Entry: Sector {SectorIndex}, Size: {FileSize}, {FilePath}")]
        public class HedEntry
        {
            public uint SectorIndex { get; set; }
            public uint FileSize { get; set; }
            public string? FilePath { get; set; }
            public HedEntry(uint index, uint size, string path)
            {
                SectorIndex = index;
                FileSize = size;
                FilePath = path;
            }
        }
        public class HedFile
        {
            public List<HedEntry> HedEntries { get; set; }
            public HedFile()
            {
                HedEntries = new List<HedEntry>();
            }
            public void AddEntry(uint index, uint size, string path)
            {
                HedEntry entry = new HedEntry(index, size, path);
                HedEntries.Add(entry);
            }
        }
        public static List<HedEntry> ReadHEDFile(byte[] HedBytes)
        {
            // PS2 files are always little-endian
            ReadWrite reader = new ReadWrite("little");
            // There are no headers in a HED file so go right into reading the data
            MemoryStream stream = new MemoryStream(HedBytes);
            List<HedEntry> HedList = new List<HedEntry>();
            uint checkend = 1;
            uint originalPosition = (uint)stream.Position;
            while (checkend != 0xffffffff)
            {
                if (originalPosition % 4 != 0)
                {
                    stream.Position = originalPosition + (4 - originalPosition % 4);
                }
                else
                {
                    
                    uint sectorIndex = reader.ReadUInt32(stream);
                    uint fileSize = reader.ReadUInt32(stream);
                    string filePath = ReadWrite.ReadUntilNullByte(stream);
                    HedEntry entry = new HedEntry(sectorIndex, fileSize, filePath);
                    HedList.Add(entry);
                }
                originalPosition = (uint)stream.Position;

                checkend = reader.ReadUInt32(stream);
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
            stream.Close();
            return HedList;
        }
        
    }
}
