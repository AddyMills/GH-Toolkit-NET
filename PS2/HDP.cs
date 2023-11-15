using GH_Toolkit_Core.Checksum;
using GH_Toolkit_Core.Methods;
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
            public HdpFolderEntry(uint count, string checksum, uint offset)
            {
                FileCount = count;
                FolderChecksum = checksum;
                HdpOffset = offset;
            }
        }
        [DebuggerDisplay("{FileChecksum} at {SectorIndex*2048} in WAD, sector {SectorIndex} [{DataLength} bytes long]")]
        public class HdpFileEntry
        {
            public uint SectorIndex { get; set; } // Data starts at this value * 2048 in the WAD file
            public uint DataLength { get; set; } // Length of the data. This does not include padding to next 2048 byte boundary
            public string? FileChecksum { get; set; } // Checksum of file path
            public HdpFileEntry(uint sectorIndex, uint dataLength, string fileChecksum)
            {
                SectorIndex = sectorIndex;
                DataLength = dataLength;
                FileChecksum = fileChecksum;
            }
        }
        public class HdpFile
        {
            public List<HdpFolderEntry>? HdpFolders { get; set; }
            public List<HdpFileEntry>? HdpFiles { get; set; }
            public void AddFileEntry(uint sectorIndex, uint dataLength, string fileChecksum)
            {
                HdpFileEntry hdpFileEntry = new HdpFileEntry(sectorIndex, dataLength, fileChecksum);
                HdpFiles.Add(hdpFileEntry);
            }
            public void AddFolderEntry(uint count, string checksum, uint offset)
            {
                HdpFolderEntry hdpFolderEntry = new HdpFolderEntry(count, checksum, offset);
                HdpFolders.Add(hdpFolderEntry);
            }
        }
        private static void AddToDict(Dictionary<uint, string> dict, string entry)
        {
            var key_string = CRC.QBKey(entry);
            var key = Convert.ToUInt32(key_string, 16);
            if (!dict.ContainsKey(key))
            {
                dict[key] = entry;
            }
            return;
        }
        public static Dictionary<uint, string> CreateHDPDict(List<HedEntry> hedEntries)
        {
            Dictionary<uint, string> entries = new Dictionary<uint, string>();
            foreach (HedEntry entry in hedEntries)
            {
                string folderPath = Path.GetDirectoryName(entry.FilePath);
                string filePath = Path.GetFileName(entry.FilePath);
                if (folderPath.StartsWith("\\"))
                {
                    folderPath = folderPath.Substring(1);
                }
                try
                {
                    AddToDict(entries, folderPath);
                }
                catch
                {
                    // If an exception occurs, ignore and continue processing.
                    Console.WriteLine($"Could not parse folder {folderPath} into QB Key (possible type issue?)");
                }
                try
                {
                    AddToDict(entries, filePath);
                }
                catch
                {
                    // If an exception occurs, ignore and continue processing.
                    Console.WriteLine($"Could not parse folder {folderPath} into QB Key (possible type issue?)");
                }
            }
            return entries;
        }

        public static HdpFile ReadHDPFile(byte[] HdpBytes, Dictionary<uint, string>? folderChecksums)
        {
            // PS2 files are always little-endian
            ReadWrite reader = new ReadWrite("little");
            HdpFile hdpFile = new HdpFile();
            MemoryStream stream = new MemoryStream(HdpBytes);
            if (folderChecksums == null)
            {
                folderChecksums = new Dictionary<uint, string>();
                // Since we're checking the dictionary often, it's good to have an empty dictionary if non-existant.
            }
            hdpFile.HdpFiles = new List<HdpFileEntry>();

            uint numEntries = reader.ReadUInt32(stream);
            uint fileOffset = reader.ReadUInt32(stream);
            uint folderOffset = reader.ReadUInt32(stream);
            uint unkOffset = reader.ReadUInt32(stream);

            if (folderOffset > 0)
            {
                hdpFile.HdpFolders = new List<HdpFolderEntry>();
                stream.Seek(folderOffset, SeekOrigin.Begin);
                while (true)
                {
                    uint fileCount = reader.ReadUInt32(stream);
                    if (fileCount == 0xffffffff || stream.Position >= fileOffset)
                    {
                        break;
                    }
                    uint checksum = reader.ReadUInt32(stream);
                    string folderChecksum;
                    if (folderChecksums.ContainsKey(checksum))
                    {
                        folderChecksum = folderChecksums[checksum];
                    }
                    else
                    {
                        Console.WriteLine($"Could not find string for {checksum}.");
                        folderChecksum = "0x" + checksum.ToString("x8");
                    }
                    uint hdpOffset = reader.ReadUInt32(stream);
                    HdpFolderEntry folderEntry = new HdpFolderEntry(fileCount, folderChecksum, hdpOffset);
                    hdpFile.HdpFolders.Add(folderEntry);
                }
            }
            stream.Seek(fileOffset, SeekOrigin.Begin);
            while (true)
            {
                uint sectorIndex = reader.ReadUInt32(stream);
                uint dataLength = reader.ReadUInt32(stream);
                uint checksum = reader.ReadUInt32(stream);
                string fileChecksum;
                if (folderChecksums.ContainsKey(checksum))
                {
                    fileChecksum = folderChecksums[checksum];
                }
                else
                {
                    fileChecksum = "0x" + checksum.ToString("x8");
                    Console.WriteLine($"Could not find string for {fileChecksum}.");
                }
                HdpFileEntry fileEntry = new HdpFileEntry(sectorIndex, dataLength, fileChecksum);
                hdpFile.HdpFiles.Add(fileEntry);
                if (hdpFile.HdpFiles.Count >= numEntries)
                {
                    break;
                }
            }
            return hdpFile;
        }
    }
}
