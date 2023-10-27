using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.PS2.HED;

namespace GH_Toolkit_Core.PS2
{
    public class HDP
    {
        [DebuggerDisplay("{FolderChecksum} at {HdpOffset} [{FileCount} in folder]")]
        public class HdpFolderEntry
        {
            public uint FileCount { get; set; } // Number of files in folder path
            public string? FolderChecksum { get; set; } // Checksum of folder path
            public uint HdpOffset { get; set; } // Offset in DATAPD file of first file in folder
        }
        public class HdpFileEntry
        {
            public uint SectorIndex { get; set; } // Number of files in folder path
            public uint DataLength { get; set; } // Offset in DATAPD file of first file in folder
            public string? FolderChecksum { get; set; } // Checksum of file path
        }
        public class HdpFile
        {
            public HdpFolderEntry? HdpFolders { get; set; }
            public HdpFileEntry? HdpFiles { get; set; }
        }
        public static Dictionary<uint, string> CreateHDPDict(List<HedEntry> hedEntries)
        {
            Dictionary<uint, string> entries = new Dictionary<uint, string>();
            foreach (HedEntry entry in hedEntries)
            {
                string folderPath = Path.GetDirectoryName(entry.FilePath);
                if (folderPath.StartsWith("\\"))
                {
                    folderPath = folderPath.Substring(1);
                }
                try
                {
                    var key = Convert.ToUInt32(folderPath, 16);
                    entries[key] = folderPath;
                }
                catch
                {
                    // If an exception occurs, ignore and continue processing the next line.
                }
            }
            return entries;
        }
        public static HdpFile ReadHDPFile(byte[] HdpBytes, Dictionary<uint, string>? folderEntries)
        {
            // PS2 files are always little-endian
            bool flipBytes = Readers.FlipCheck("little");
            HdpFile hdpFile = new HdpFile();
            MemoryStream stream = new MemoryStream(HdpBytes);
            uint numEntries = Readers.ReadUInt32(stream, flipBytes);
            uint fileOffset = Readers.ReadUInt32(stream, flipBytes);
            uint folderOffset = Readers.ReadUInt32(stream, flipBytes);
            uint unkOffset = Readers.ReadUInt32(stream, flipBytes);

            if (folderOffset > 0)
            {

            }
            return hdpFile;
        }
    }
}
