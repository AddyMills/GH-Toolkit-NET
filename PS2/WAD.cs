using GH_Toolkit_Core.Checksum;
using GH_Toolkit_Core.Methods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.PS2.HED;
using static GH_Toolkit_Core.PS2.WAD;

/*
 * * This file is intended to be a collection of custom methods to read and create WAD files
 * * Used in PS2 customs only
 * * 
 * */

namespace GH_Toolkit_Core.PS2
{
    public class WAD
    {
        private const uint SECTORSIZE = 2048;
        private const uint HEADERSIZE = 16;
        private const uint EOF = 0xffffffff;
        private readonly bool flipBytes = InitializeFlipBytes();

        private static bool InitializeFlipBytes()
        {
            // Your logic to determine the value of flipBytes
            return ReadWrite.FlipCheck("little");
        }
        public static ReadWrite Reader = new ReadWrite("little");
        [DebuggerDisplay("{RelPath} at index {sectorIndex}")]
        public class WadEntry
        {
            public string? AbsPath { get; set; }
            public string? RelPath { get; set; }
            public string? FilePath { get; set; }
            public string? FolderPath { get; set; }
            public uint fileQbKey { get; set; }
            public uint folderQbKey { get; set; }
            public uint fileSize { get; set; }
            public uint paddedSize { get; set; }
            public uint sectorIndex { get; set; }
            public byte[] bytes { get; set; }

            public WadEntry(string? absPath, string? mainPath)
            {
                AbsPath = absPath;
                RelPath = Path.GetRelativePath(mainPath, absPath);
                FilePath = Path.GetFileName(RelPath);
                FolderPath = Path.GetDirectoryName(RelPath);
                fileQbKey = Convert.ToUInt32(CRC.QBKey(FilePath), 16);
                folderQbKey = Convert.ToUInt32(CRC.QBKey(FolderPath), 16);
            }
            public void LoadFileAndPad()
            {
                if (AbsPath == null)
                {
                    throw new InvalidOperationException("AbsPath is null.");
                }

                // Load file bytes
                bytes = File.ReadAllBytes(AbsPath);

                fileSize = (uint)bytes.Length;

                // Determine the padding required
                uint paddingSize = SECTORSIZE - ((uint)bytes.Length % SECTORSIZE);
                if (paddingSize != SECTORSIZE) // If exactly 2048, no padding needed
                {
                    // Create a padded array and copy the original bytes
                    byte[] paddedBytes = new byte[bytes.Length + paddingSize];
                    bytes.CopyTo(paddedBytes, 0);

                    // Fill the rest with zeroes
                    Array.Clear(paddedBytes, bytes.Length, (int)paddingSize);

                    // Assign the padded array to the bytes variable
                    bytes = paddedBytes;
                }
                paddedSize = (uint)bytes.Length;

            }
        }
        [DebuggerDisplay("{folderName} [{wadEntries} in folder]")]
        public class FolderEntry
        {
            public string? folderName { get; set; }
            public List<WadEntry> wadEntries { get; set; }
            public FolderEntry(string folder, WadEntry wadEntry)
            {
                folderName = folder;
                wadEntries = new List<WadEntry>() { wadEntry};
            }
        }

        private static void ValidateFilePath(string filePath)
        {
            if (!Directory.Exists(filePath))
                throw new DirectoryNotFoundException("File path specified does not exist!");
        }
        private static List<WadEntry> LoadWadEntries(string filePath, bool cliMode = true)
        {
            Console.WriteLine($"Loading files from {filePath}. Please wait...");
            string[] entries = Directory.GetFileSystemEntries(filePath, "*", SearchOption.AllDirectories);
            List<WadEntry> hedEntries = new List<WadEntry>();
            foreach (string entry in entries)
            {
                if (File.Exists(entry))
                {
                    if (cliMode)
                    {
                        Console.WriteLine($"Loading {entry}");
                    }
                    WadEntry wadEntry = new WadEntry(entry, filePath);
                    wadEntry.LoadFileAndPad();
                    hedEntries.Add(wadEntry);
                }
            }
            return hedEntries;
        }
        private static void ProcessEntries(List<WadEntry> hedEntries, Dictionary<uint, FolderEntry> folders, List<uint> folderChecks, HedFile hedFile, bool cliMode = true)
        {
            uint sectorIndex = 0;
            if (!cliMode)
            {
                Console.WriteLine($"Adding {hedEntries.Count} to new WAD file. Please wait...");
            }
            foreach (var entry in hedEntries)
            {
                if (cliMode)
                {
                    Console.WriteLine($"Adding {entry.RelPath}");
                }
                    
                entry.sectorIndex = sectorIndex;
                UpdateFolderEntries(entry, folders, folderChecks);
                hedFile.AddEntry(sectorIndex, entry.fileSize, "\\" + entry.RelPath);
                sectorIndex += entry.paddedSize / SECTORSIZE;
            }
        }
        private static void UpdateFolderEntries(WadEntry entry, Dictionary<uint, FolderEntry> folders, List<uint> folderChecks)
        {
            uint qbKey = entry.folderQbKey;

            if (!folders.ContainsKey(qbKey))
            {
                var folderEntry = new FolderEntry(entry.FolderPath, entry);
                folders.Add(qbKey, folderEntry);
                folderChecks.Add(qbKey);
            }
            else
            {
                folders[qbKey].wadEntries.Add(entry);
            }
        }
        private static void InitializeDataStreams(MemoryStream datapd, MemoryStream datapf, List<WadEntry> hedEntries, uint foldersCount, bool flipBytes)
        {
            uint fileOffset = HEADERSIZE + ((foldersCount + 1) * 12);

            // Initialize datapd stream
            Reader.WriteUInt32(datapd, (uint)hedEntries.Count);
            Reader.WriteUInt32(datapd, fileOffset);
            Reader.WriteUInt32(datapd, (uint)16);
            Reader.WriteUInt32(datapd, (uint)0);

            // Initialize datapf stream
            hedEntries.Sort((entry1, entry2) => entry1.fileQbKey.CompareTo(entry2.fileQbKey));
            Reader.WriteUInt32(datapf, (uint)hedEntries.Count);
            Reader.WriteUInt32(datapf, HEADERSIZE);
            Reader.WriteUInt32(datapf, (uint)0);
            Reader.WriteUInt32(datapf, (uint)0);
        }
        private static void PopulateFolderStreams(MemoryStream pd_folders, MemoryStream pd_files, Dictionary<uint, FolderEntry> folders, List<uint> folderChecks, bool flipBytes)
        {
            uint fileOffset = HEADERSIZE + (((uint)folders.Count + 1) * 12);

            foreach (uint check in folderChecks)
            {
                var curr = folders[check].wadEntries;
                Reader.WriteUInt32(pd_folders, (uint)curr.Count);
                Reader.WriteUInt32(pd_folders, check);
                Reader.WriteUInt32(pd_folders, fileOffset);
                fileOffset += 12 * (uint)curr.Count;

                PopulateFileStreams(pd_files, curr);
                /*foreach (var entry in curr)
                {
                    Reader.WriteUInt32(pd_files, entry.sectorIndex);
                    Reader.WriteUInt32(pd_files, entry.fileSize);
                    Reader.WriteUInt32(pd_files, entry.fileQbKey);
                }*/
            }

            // Write footer
            Reader.WriteUInt32(pd_folders, 0xffffffff);
            Reader.WriteUInt32(pd_folders, 0xcdcdcdcd);
            Reader.WriteUInt32(pd_folders, 0xcdcdcdcd);
        }
        private static void PopulateFileStreams(MemoryStream stream, List<WadEntry> entries)
        {
            foreach (var entry in entries)
            {
                Reader.WriteUInt32(stream, entry.sectorIndex);
                Reader.WriteUInt32(stream, entry.fileSize);
                Reader.WriteUInt32(stream, entry.fileQbKey);
            }
        }
        private static void FinalizeStreams(MemoryStream pd_folders, MemoryStream pd_files, MemoryStream datapd, MemoryStream datapf, MemoryStream pf_files)
        {
            ReadWrite.CopyStreamClose(pd_folders, datapd);
            ReadWrite.CopyStreamClose(pd_files, datapd);
            ReadWrite.CopyStreamClose(pf_files, datapf);
        }
        private static MemoryStream MakeHedFile(HedFile hedFile, bool flipBytes)
        {
            MemoryStream stream = new MemoryStream();
            foreach (HedEntry entry in hedFile.HedEntries)
            {
                Reader.WriteUInt32(stream, entry.SectorIndex);
                Reader.WriteUInt32(stream, entry.FileSize);
                ReadWrite.WriteNullTermString(stream, entry.FilePath);
                uint padding = 4 - (uint)stream.Length % 4;
                if (padding != 4)
                {
                    ReadWrite.FillNullTermString(stream, padding);
                }
            }
            Reader.WriteUInt32(stream, EOF);
            return stream;
        }
        private static void SaveStreamToFile(MemoryStream stream, string filePath)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                stream.Position = 0; // Reset the position of MemoryStream to the beginning
                stream.CopyTo(file);
            }
            stream.Close();
        }
        private static void SaveWadToFile(List<WadEntry> wadFiles, string filePath)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                foreach (WadEntry entry in wadFiles)
                {
                    file.Write(entry.bytes, 0, entry.bytes.Length);
                }
            }

        }

        public static void CompileWADFile(string filePath, bool cliMode = true)
        {
            ValidateFilePath(filePath);
            string parentFolder = Path.GetDirectoryName(filePath);
            string saveFolder = Path.Combine(parentFolder, "WAD Compile");
            Directory.CreateDirectory(saveFolder);
            bool flipBytes = ReadWrite.FlipCheck("little");

            var hedEntries = LoadWadEntries(filePath, cliMode);
            hedEntries.Sort((entry1, entry2) => string.Compare(entry1.RelPath, entry2.RelPath));

            Dictionary<uint, FolderEntry> folders = new Dictionary<uint, FolderEntry>();
            List<uint> folderChecks = new List<uint>();

            HedFile hedFile = new HedFile(); // All entries by Sector Index and file names

            ProcessEntries(hedEntries, folders, folderChecks, hedFile, cliMode);
            SaveWadToFile(hedEntries, Path.Combine(saveFolder, "DATAP.WAD"));

            MemoryStream datapd = new MemoryStream(); // File of all entries by folder name
            MemoryStream datapf = new MemoryStream(); // All entries by checksum order
            InitializeDataStreams(datapd, datapf, hedEntries, (uint)folders.Count, flipBytes);

            MemoryStream pd_folders = new MemoryStream();
            MemoryStream pd_files = new MemoryStream();
            MemoryStream pf_files = new MemoryStream();
            PopulateFolderStreams(pd_folders, pd_files, folders, folderChecks, flipBytes);



            hedEntries.Sort((entry1, entry2) => entry1.fileQbKey.CompareTo(entry2.fileQbKey));
            PopulateFileStreams(pf_files, hedEntries);

            FinalizeStreams(pd_folders, pd_files, datapd, datapf, pf_files);

            MemoryStream datahed = MakeHedFile(hedFile, flipBytes);

            SaveStreamToFile(datapd, Path.Combine(saveFolder, "DATAPD.HDP"));
            SaveStreamToFile(datapf, Path.Combine(saveFolder, "DATAPF.HDP"));
            SaveStreamToFile(datahed, Path.Combine(saveFolder, "DATAP.HED"));
            if (!cliMode)
            {
                Console.WriteLine("WAD file compiled successfully.");
            }
            return;
        }
        public static void ExtractWADFile(List<HedEntry> HedFiles, byte[] wad, string extractPath, bool cliMode = true)
        {
            if (!cliMode)
            {
                // Write "Found x files" when using a GUI
                Console.WriteLine($"Found {HedFiles.Count} files.");
            }
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
                if (cliMode)
                {
                    // GUI seems to be sloooooow with writing text, so only write to console in CLI mode
                    Console.WriteLine($"Extracting File {i + 1}/{HedFiles.Count}: {HedFiles[i].FilePath}");
                }                
                File.WriteAllBytes(extractFilePath, fileData);
            }
            if (!cliMode)
            {
                // Write "All x files extracted" when using GUI instead
                Console.WriteLine($"All {HedFiles.Count} files extracted.");
            }
        }

    }
}
