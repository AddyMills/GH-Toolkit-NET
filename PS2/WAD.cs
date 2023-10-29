using GH_Toolkit_Core.Checksum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.PS2.HED;
using static GH_Toolkit_Core.PS2.WAD;

namespace GH_Toolkit_Core.PS2
{
    public class WAD
    {
        [DebuggerDisplay("{RelPath}")]
        public class WadEntry
        {
            public string? AbsPath { get; set; }
            public string? RelPath { get; set; }
            public string? FilePath { get; set; }
            public string? FolderPath { get; set; }
            public byte[] bytes { get; set; }

            public WadEntry(string? absPath, string? mainPath)
            {
                AbsPath = absPath;
                RelPath = Path.GetRelativePath(mainPath, absPath);
                FilePath = Path.GetFileName(RelPath);
                FolderPath = Path.GetDirectoryName(RelPath);

            }
            public void LoadFileAndPad()
            {
                if (AbsPath == null)
                {
                    throw new InvalidOperationException("AbsPath is null.");
                }

                // Load file bytes
                bytes = File.ReadAllBytes(AbsPath);

                // Determine the padding required
                int paddingSize = 2048 - (bytes.Length % 2048);
                if (paddingSize != 0) // If exactly 2048, no padding needed
                {
                    // Create a padded array and copy the original bytes
                    byte[] paddedBytes = new byte[bytes.Length + paddingSize];
                    bytes.CopyTo(paddedBytes, 0);

                    // Fill the rest with zeroes
                    Array.Clear(paddedBytes, bytes.Length, paddingSize);

                    // Assign the padded array to the bytes variable
                    bytes = paddedBytes;
                }
            }
        }
        [DebuggerDisplay("{folderName} [{wadEntries} in folder]")]
        public class FolderEntry
        {
            public string? folderName { get; set; }
            public uint wadEntries { get; set; }
            public uint pdOffset { get; set; }
            public FolderEntry(string folder, uint offset)
            {
                folderName = folder;
                wadEntries = 1;
                pdOffset = offset;
            }
        }
        public static void CompileWADFile(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                throw new Exception("File path specified does not exist!");
            }
            string[] entries = Directory.GetFileSystemEntries(filePath, "*", SearchOption.AllDirectories);
            List<WadEntry> gameFiles = new List<WadEntry>();
            foreach (string entry in entries)
            {
                if (File.Exists(entry))
                {
                    WadEntry wadEntry = new WadEntry(entry, filePath);
                    wadEntry.LoadFileAndPad();
                    gameFiles.Add(wadEntry);
                }
            }
            gameFiles.Sort((entry1, entry2) => string.Compare(entry1.RelPath, entry2.RelPath));
            Dictionary<string, FolderEntry> folders = new Dictionary<string, FolderEntry>();

            for (int i = 0; i < gameFiles.Count; i++)
            {
                FolderEntry folderEntry = new FolderEntry(gameFiles[i].FolderPath, (uint)i);
                string qbKey = CRC.QBKey(gameFiles[i].FolderPath);
                if (!folders.ContainsKey(qbKey))
                {
                    folders.Add(qbKey, folderEntry);
                }
                else
                {
                    folders[qbKey].wadEntries++;
                }
            }
            return;
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
                Console.WriteLine($"Extracting File {i + 1}/{HedFiles.Count}: {HedFiles[i].FilePath}");
                File.WriteAllBytes(extractFilePath, fileData);
            }
        }
    }
}
