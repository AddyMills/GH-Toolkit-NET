using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Debug;

namespace GH_Toolkit_Core
{
    public class PAK
    {
        [DebuggerDisplay("Entry: {FullName}")]
        public class PakEntry
        {
            public string? Extension { get; set; }
            public uint StartOffset { get; set; }
            public uint FileSize { get; set; }
            public string? AssetContext { get; set; }
            public string? FullName { get; set; }
            public string? NameNoExt { get; set; }
            public uint Parent { get; set; }
            public uint Flags { get; set; }
            public byte[]? EntryData { get; set; }
        }
        public static void ProcessPAKFromFile(string file)
        {
            string fileName = Path.GetFileName(file);
            if (fileName.IndexOf(".pab", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                return;
            }
            if (fileName.IndexOf(".pak", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                throw new Exception("Invalid File");
            }
            string fileNoExt = fileName.Substring(0, fileName.IndexOf(".pak"));
            string fileExt = Path.GetExtension(file);
            Console.WriteLine($"Extracting {fileNoExt}");
            string folderPath = Path.GetDirectoryName(file);
            string NewFolderPath = Path.Combine(folderPath, fileNoExt);
            string songCheck = "_song";
            string songName = "";
            List<PakEntry> pakEntries;
            if (fileName.Contains(songCheck))
            {
                songName = fileName.Substring(0, fileName.IndexOf(songCheck));
            }

            byte[] test_pak = File.ReadAllBytes(file);
            byte[] test_pab = null;

            // Check for a corresponding .pab file
            string pabFilePath = Path.Combine(folderPath, fileNoExt + $".pab{fileExt}");
            if (File.Exists(pabFilePath))
            {
                test_pab = File.ReadAllBytes(pabFilePath);
            }

            string endian;
            if (fileExt == ".ps2")
            {
                endian = "little";
            }
            else
            {
                endian = "big";
                fileExt = ".xen";
            }
            try
            {
                pakEntries = ExtractPAK(test_pak, test_pab, endian: endian, songName: songName);
            }
            catch
            {
                throw;
            }

            foreach (PakEntry entry in pakEntries)
            {
                string pakFileName = (string)entry.FullName;
                if (!pakFileName.EndsWith(fileExt, StringComparison.CurrentCultureIgnoreCase))
                { 
                    pakFileName += entry.Extension;
                }

                string saveName = Path.Combine(NewFolderPath, pakFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(saveName));
                File.WriteAllBytes(saveName, entry.EntryData);
            }
        }
        private static uint CheckPabType(byte[] pakBytes, bool flipBytes)
        {
            byte[] pabCheck = new byte[4];
            Array.Copy(pakBytes, 4, pabCheck, 0, 4);
            if (flipBytes)
            {
                Array.Reverse(pabCheck);
            }
            uint pabOff = BitConverter.ToUInt32(pabCheck);
            if (pabOff == 0)
            {
                return 0;
            }
            return pabOff;
        }
        public static List<PakEntry> ExtractPAK(byte[] pakBytes, byte[]? pabBytes, string endian = "big", string songName = "")
        {
            bool newPak = false;
            bool flipBytes = ReadWrite.FlipCheck(endian);
            if (Compression.isCompressed(pakBytes))
            {
                pakBytes = Compression.DecompressWTPak(pakBytes);
            }
            if (pabBytes != null)
            {
                uint pabType = CheckPabType(pakBytes, flipBytes);
                switch (pabType)
                {
                    case 0:
                        throw new Exception("PAK type not yet implemented.");
                    case uint size when size >= pakBytes.Length:
                        byte[] bytes = new byte[pabType + pabBytes.Length];
                        Array.Copy(pakBytes, 0, bytes, 0, pakBytes.Length);
                        Array.Copy(pabBytes, 0, bytes, pabType, pabBytes.Length);
                        pakBytes = bytes;
                        break;
                }
            }
            List<PakEntry> pakList = ExtractOldPak(pakBytes, flipBytes, songName);



            return pakList;
        }
        public static List<PakEntry> ExtractOldPak(byte[] pakBytes, bool flipBytes, string songName = "")
        {
            
            MemoryStream stream = new MemoryStream(pakBytes);
            List<PakEntry> PakList = new List<PakEntry>();
            Dictionary<uint, string> headers = DebugReader.MakeDictFromName(songName);

            string DebugCheck(uint check)
            {
                return headers.TryGetValue(check, out string? result) ? result : DebugReader.DbgCheck(check);
            }
            bool TryGH3 = false;
            while (true)
            {
                PakEntry entry = new PAK.PakEntry();
                uint header_start = (uint)stream.Position; // To keep track of which entry since the offset in the header needs to be added to the StartOffset below

                uint extension = ReadWrite.ReadUInt32(stream, flipBytes);
                if (extension != 0x2cb3ef3b && extension != 0xb524565f)
                {
                    entry.Extension = DebugCheck(extension);
                }
                else
                {
                    break;
                }
                if (!entry.Extension.StartsWith("."))
                {
                    entry.Extension = "." + entry.Extension;
                }
                uint offset = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.StartOffset = offset + header_start;
                uint filesize = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.FileSize = filesize;
                uint asset = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.AssetContext = DebugCheck(asset);
                uint fullname = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.FullName = DebugCheck(fullname);
                uint name = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.NameNoExt = DebugCheck(name);
                if (entry.FullName.StartsWith("0x"))
                {
                    entry.FullName = $"{entry.FullName}.{entry.NameNoExt}";
                }
                uint parent = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.Parent = parent;
                uint flags = ReadWrite.ReadUInt32(stream, flipBytes);
                entry.Flags = flags;
                switch (flags)
                {
                    case 0:
                        break;
                    case 0x20:
                    case 0x22:
                        var skipTo = stream.Position + 160;
                        string tempString = ReadWrite.ReadUntilNullByte(stream);
                        switch (tempString)
                        {
                            case string s when s.StartsWith("ones\\"):
                                tempString = "z" + tempString;
                                break;
                            case string s when s.StartsWith("cripts\\"):
                                tempString = "s" + tempString;
                                break;
                            case string s when s.StartsWith("kies\\"):
                                tempString = "s" + tempString;
                                break;
                            case string s when s.StartsWith("ongs\\"):
                                tempString = "s" + tempString;
                                break;
                            case string s when s.StartsWith("odels\\"):
                                tempString = "m" + tempString;
                                break;
                            case string s when s.StartsWith("ak\\"):
                                tempString = "p" + tempString;
                                break;
                        }
                        entry.FullName = tempString;
                        stream.Position = skipTo;
                        break;
                    default: 
                        throw new InvalidOperationException("Unknown flag found");
                }
                try
                {
                    entry.EntryData = new byte[entry.FileSize];
                    Array.Copy(pakBytes, entry.StartOffset, entry.EntryData, 0, entry.FileSize);
                    PakList.Add(entry);
                }
                catch (Exception ex)
                {
                    if (TryGH3 == true)
                    {
                        Console.WriteLine(ex.Message);
                        throw new Exception("Could not extract PAK file.");
                    }
                    Console.WriteLine("Could not find last entry. Trying Guitar Hero 3 Compression.");
                    PakList.Clear();
                    pakBytes = Compression.DecompressData(pakBytes);
                    stream = new MemoryStream(pakBytes);
                    TryGH3 = true;
                }
            }

            Console.WriteLine("Success!");
            return PakList;
        }
    }
}
