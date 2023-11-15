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
using GH_Toolkit_Core.Methods;

namespace GH_Toolkit_Core.PAK
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
            bool debugFile = fileName.Contains("dbg.pak");
            string masterFilePath = Path.Combine(NewFolderPath, "master.txt");
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
            catch (Exception ex)
            {
                test_pak = Compression.DecompressData(test_pak);
                if (test_pab != null)
                {
                    test_pab = Compression.DecompressData(test_pab);
                }
                pakEntries = ExtractPAK(test_pak, test_pab, endian: endian, songName: songName);
            }

            foreach (PakEntry entry in pakEntries)
            {
                string pakFileName = entry.FullName;
                if (!pakFileName.EndsWith(fileExt, StringComparison.CurrentCultureIgnoreCase))
                {
                    pakFileName += fileExt;
                }

                string saveName = Path.Combine(NewFolderPath, pakFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(saveName));
                File.WriteAllBytes(saveName, entry.EntryData);

                if (debugFile)
                {
                    string[] lines = File.ReadAllLines(saveName);

                    using (StreamWriter masterFileWriter = File.AppendText(masterFilePath))
                    {
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("0x"))
                            {
                                masterFileWriter.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }
        private static uint CheckPabType(byte[] pakBytes, string endian = "big")
        {
            bool flipBytes = ReadWrite.FlipCheck(endian);
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
            ReadWrite reader = new ReadWrite(endian);
            if (Compression.isChnkCompressed(pakBytes))
            {
                pakBytes = Compression.DecompressWTPak(pakBytes);
            }
            if (pabBytes != null)
            {
                uint pabType = CheckPabType(pakBytes, endian);
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
            List<PakEntry> pakList = ExtractOldPak(pakBytes, endian, songName);



            return pakList;
        }
        public static List<PakEntry> ExtractOldPak(byte[] pakBytes, string endian, string songName = "")
        {
            ReadWrite reader = new ReadWrite(endian);
            MemoryStream stream = new MemoryStream(pakBytes);
            List<PakEntry> PakList = new List<PakEntry>();
            Dictionary<uint, string> headers = DebugReader.MakeDictFromName(songName);

            bool TryGH3 = false;
            while (true)
            {
                PakEntry entry = new PakEntry();
                uint header_start = (uint)stream.Position; // To keep track of which entry since the offset in the header needs to be added to the StartOffset below

                uint extension = reader.ReadUInt32(stream);
                if (extension != 0x2cb3ef3b && extension != 0xb524565f)
                {
                    entry.Extension = DebugReader.DebugCheck(headers, extension);
                }
                else
                {
                    break;
                }
                if (!entry.Extension.StartsWith("."))
                {
                    entry.Extension = "." + entry.Extension;
                }
                uint offset = reader.ReadUInt32(stream);
                entry.StartOffset = offset + header_start;
                uint filesize = reader.ReadUInt32(stream);
                entry.FileSize = filesize;
                uint asset = reader.ReadUInt32(stream);
                entry.AssetContext = DebugReader.DebugCheck(headers, asset);
                uint fullname = reader.ReadUInt32(stream);
                entry.FullName = DebugReader.DebugCheck(headers, fullname);
                uint name = reader.ReadUInt32(stream);
                entry.NameNoExt = DebugReader.DebugCheck(headers, name);
                if (entry.FullName.StartsWith("0x"))
                {
                    entry.FullName = $"{entry.FullName}.{entry.NameNoExt}";
                }
                uint parent = reader.ReadUInt32(stream);
                entry.Parent = parent;
                uint flags = reader.ReadUInt32(stream);
                entry.Flags = flags;
                switch (flags)
                {
                    case 0:
                        break;
                    case 0x20:
                    case 0x21:
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
