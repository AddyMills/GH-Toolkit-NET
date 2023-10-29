using GH_Toolkit_Core.Checksum;
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
        [DebuggerDisplay("{FileChecksum} at {SectorIndex*2048} in WAD, sector {SectorIndex} [{DataLength} bytes long]")]
        public class HdpFileEntry
        {
            public uint SectorIndex { get; set; } // Data starts at this value * 2048 in the WAD file
            public uint DataLength { get; set; } // Length of the data. This does not include padding to next 2048 byte boundary
            public string? FileChecksum { get; set; } // Checksum of file path
        }
        public class HdpFile
        {
            public List<HdpFolderEntry>? HdpFolders { get; set; }
            public List<HdpFileEntry>? HdpFiles { get; set; }
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
        public static void ExtractWADFile(List<HedEntry> HedFiles, byte[] wad, string extractPath)
        {
            for (int i = 0; i < HedFiles.Count; i++)
            {
                byte[] fileData = new byte[HedFiles[i].FileSize];
                Array.Copy(wad, HedFiles[i].SectorIndex * 2048, fileData, 0, HedFiles[i].FileSize);
                if (HedFiles[i].FilePath.StartsWith("\\"))
                {
                    HedFiles[i].FilePath = HedFiles[i].FilePath.Substring(1);
                }
                string extractFilePath = Path.Combine(extractPath, HedFiles[i].FilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(extractFilePath));
                Console.WriteLine($"Extracting File {i+1}/{HedFiles.Count}: {HedFiles[i].FilePath}");
                File.WriteAllBytes(extractFilePath, fileData);
            }
        }
        public static HdpFile ReadHDPFile(byte[] HdpBytes, Dictionary<uint, string>? folderChecksums)
        {
            // PS2 files are always little-endian
            bool flipBytes = Readers.FlipCheck("little");
            HdpFile hdpFile = new HdpFile();
            MemoryStream stream = new MemoryStream(HdpBytes);
            if (folderChecksums == null)
            {
                folderChecksums = new Dictionary<uint, string>();
                // Since we're checking the dictionary often, it's good to have an empty dictionary if non-existant.
            }
            hdpFile.HdpFiles = new List<HdpFileEntry>();

            uint numEntries = Readers.ReadUInt32(stream, flipBytes);
            uint fileOffset = Readers.ReadUInt32(stream, flipBytes);
            uint folderOffset = Readers.ReadUInt32(stream, flipBytes);
            uint unkOffset = Readers.ReadUInt32(stream, flipBytes);

            if (folderOffset > 0)
            {
                hdpFile.HdpFolders = new List<HdpFolderEntry>();
                stream.Seek(folderOffset, SeekOrigin.Begin);
                while (true)
                {
                    HdpFolderEntry folderEntry = new HdpFolderEntry();
                    folderEntry.FileCount = Readers.ReadUInt32(stream, flipBytes);
                    if (folderEntry.FileCount == 0xffffffff || stream.Position >= fileOffset)
                    {
                        break;
                    }
                    uint checksum = Readers.ReadUInt32(stream, flipBytes);
                    if (folderChecksums.ContainsKey(checksum))
                    {
                        folderEntry.FolderChecksum = folderChecksums[checksum];
                    }
                    else
                    {
                        Console.WriteLine($"Could not find string for {checksum}.");
                        folderEntry.FolderChecksum = "0x" + checksum.ToString("X");
                    }
                    folderEntry.HdpOffset = Readers.ReadUInt32(stream, flipBytes);
                    hdpFile.HdpFolders.Add(folderEntry);
                }
            }
            stream.Seek(fileOffset, SeekOrigin.Begin);
            while (true)
            {
                HdpFileEntry fileEntry = new HdpFileEntry();
                fileEntry.SectorIndex = Readers.ReadUInt32(stream, flipBytes);
                fileEntry.DataLength = Readers.ReadUInt32(stream, flipBytes);
                uint checksum = Readers.ReadUInt32(stream, flipBytes);
                if (folderChecksums.ContainsKey(checksum))
                {
                    fileEntry.FileChecksum = folderChecksums[checksum];
                }
                else
                {
                    fileEntry.FileChecksum = "0x" + checksum.ToString("X");
                    Console.WriteLine($"Could not find string for {fileEntry.FileChecksum}.");
                }
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
