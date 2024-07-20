using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using GH_Toolkit_Core.Debug;
using GH_Toolkit_Core.Methods;
using static GH_Toolkit_Core.PAK.PAK;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.Checksum.CRC;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.PortableExecutable;
using GH_Toolkit_Core.MIDI;
using GH_Toolkit_Core.SKA;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.Xml;
using System.Text;
using static GH_Toolkit_Core.Methods.Exceptions;

/*
 * * This file is intended to be a collection of custom methods to read and create PAK files
 * * 
 * * 
 * * 
 * */

namespace GH_Toolkit_Core.PAK
{
    public class PAK
    {
        [DebuggerDisplay("Entry: {PakName}")]
        public class PakEntry
        {
            public string? Extension { get; set; }
            public uint StartOffset { get; set; }
            public uint FileSize { get; set; }
            public string? AssetContext { get; set; }
            public string? FullName { get; set; }
            public string? NameNoExt { get; set; }
            public uint Parent { get; set; }
            public int Flags { get; set; }
            public string? FullFlagPath { get; set; } // If flags contains 0x20 byte, this gets added
            public byte[]? EntryData { get; set; }
            public byte[]? ExtraData { get; set; }
            public string ConsoleType {  get; set; }
            public int ByteLength { get; set; } = 32;
            private string PakName { 
                get
                { 
                    return FullName == FLAGBYTE ? AssetContext! : FullName!;
                } 
            }
            public PakEntry()
            {

            }
            public PakEntry(string console, string game, string? assetContext = null)
            {
                if (console == CONSOLE_PS2 && game == GAME_GH3)
                {
                    MakeLastEntry("last");
                }
                else
                {
                    MakeLastEntry(".last");
                }
                AssetContext = assetContext;
            }
            public PakEntry(byte[] bytes, string console, string? assetContext = null)
            {
                EntryData = bytes;
                FileSize = (uint)EntryData.Length;
                ConsoleType = console;
                AssetContext = assetContext;
            }
            public void MakeLastEntry(string lastType)
            {
                EntryData = [0xAB, 0xAB, 0xAB, 0xAB];
                FileSize = (uint)EntryData.Length;
                SetExtension(lastType);
                SetNameNoExt("0x6AF98ED1");
                SetFullName("0x897ABB4A");
            }
            public void SetExtension(string extension)
            {
                Extension = extension;
                /*if (FullFlagPath != null && FullFlagPath!.IndexOf(DOT_MID_QS) != -1 && !FullFlagPath!.EndsWith(DOT_QS))
                {
                    Extension = $"{DOT_QS}{extension}";
                }
                else
                {
                    
                }*/
            }
            public void SetNameNoExt(string nameNoExt)
            {
                NameNoExt = nameNoExt;
                if (nameNoExt.IndexOf(".") != -1)
                {
                    NameNoExt = nameNoExt.Substring(0, nameNoExt.IndexOf("."));
                }
            }
            public void SetFullFlagPath(string fullFlagPath)
            {
                FullFlagPath = fullFlagPath;
            }
            public void SetFullName(string fullName)
            { 
                FullName = fullName;
            }
            public void SetNames(bool isQb)
            {
                if (ConsoleType == CONSOLE_PS2)
                {
                    if ((Flags & 0x20) != 0)
                    {
                        if (FullFlagPath!.ToLower().IndexOf(DOTPS2) == -1)
                        {
                            FullFlagPath += DOTPS2;
                        }
                        AssetContext = FullFlagPath;
                        FullFlagPath = FullFlagPath.PadRight(160, '\0');
                        ByteLength += 160;
                    }
                    if (!isQb && FullFlagPath.EndsWith(DOTPS2))
                    {
                        AssetContext = AssetContext.Substring(0, AssetContext.IndexOf(DOTPS2));
                        if (AssetContext.IndexOf(DOT_MID_QB) != -1)
                        {
                            AssetContext = AssetContext.Substring(0, AssetContext.IndexOf(DOT_QB));
                        }
                    }
                    FullName = FLAGBYTE;
                }
                else if (FullFlagPath!.StartsWith(HEXSTART))
                {
                    FullName = NameNoExt;

                    // Find the position of the first "0x" in FullFlagPath
                    int firstIndex = FullFlagPath.IndexOf(HEXSTART);

                    // Find the position of the second "0x" after the first "0x"
                    int secondIndex = FullFlagPath.IndexOf(HEXSTART, firstIndex + 1);

                    if (secondIndex != -1)
                    {
                        // Extract the second "0x" portion up to the next dot or end of the string
                        int endOfSecondPortion = FullFlagPath.IndexOf('.', secondIndex);
                        string secondPortion;

                        if (endOfSecondPortion != -1)
                        {
                            secondPortion = FullFlagPath.Substring(secondIndex, endOfSecondPortion - secondIndex);
                        }
                        else
                        {
                            secondPortion = FullFlagPath.Substring(secondIndex);
                        }

                        NameNoExt = secondPortion;
                    }
                    else
                    {
                        NameNoExt = FullName;
                    }
                }
                else if (FullFlagPath.IndexOf(DOT_MID_QS) != -1)
                {
                    FullName = FullFlagPath.Substring(0, FullFlagPath.IndexOf(DOT_QS) + 3); // Filter out the language portion
                }
                else if (FullFlagPath.IndexOf(DOT_QS) != -1)
                {
                    var langExt = FullFlagPath.Substring(FullFlagPath.IndexOf(DOT_QS) + 3).ToLower();
                    switch (langExt)
                    {
                        case ".en":
                        case ".de":
                        case ".es":
                        case ".fr":
                        case ".it":
                            FullName = FullFlagPath.Substring(0, FullFlagPath.IndexOf(DOT_QS) + 3);
                            break;
                        default:
                            FullName = FullFlagPath;
                            break;
                    }
                }
                else if (FullFlagPath.IndexOf(DOT_SKA) != -1)
                {
                    FullName = NameNoExt;
                }
                else
                {
                    FullName = FullFlagPath;
                }

                if (ConsoleType != CONSOLE_PS2 && AssetContext == null)
                {
                    AssetContext = FLAGBYTE;
                }
            }
            public void SetFlags()
            {
                if (ConsoleType == CONSOLE_PS2)
                {
                    switch (Extension)
                    {
                        case DOT_QB:
                        case DOT_MQB:
                        case DOT_SQB:
                            Flags |= 0x20;
                            break;
                        default: 
                            throw new NotImplementedException();
                    }
                    if (NameNoExt.LastIndexOf(_SFX) != -1)
                    {
                        Flags |= 0x02;
                    }
                    else if (NameNoExt.LastIndexOf(_GFX) != -1)
                    {
                        Flags |= 0x04;
                    }
                }
                else
                {
                    Flags = 0;
                }
            }
            public void OverwriteData(byte[] data)
            {
                EntryData = data;
                FileSize = (uint)EntryData.Length;
            }
        }
        public static Dictionary<string, PakEntry> PakEntryDictFromFile(string file)
        {
            var pakEntries = PakEntriesFromFilepath(file);
            Dictionary<string, PakEntry> pakDict = new Dictionary<string, PakEntry>();
            foreach (var entry in pakEntries)
            {
                pakDict.Add(entry.FullName, entry);
            }
            return pakDict;
        }
        public static List<PakEntry>? PakEntriesFromFilepath(string file)
        {
            string fileName = Path.GetFileName(file).ToLower();
            if (fileName.IndexOf(".pab", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                return null;
            }
            if (fileName.IndexOf(".pak", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                throw new Exception("Invalid File");
            }
            string fileNoExt = fileName.Substring(0, fileName.ToLower().IndexOf(".pak"));
            string fileExt = Path.GetExtension(file);
            Console.WriteLine($"Processing {fileNoExt}");
            string folderPath = Path.GetDirectoryName(file);
            string NewFolderPath = Path.Combine(folderPath, fileNoExt);
            string songCheck = "_song";
            string songCheck2 = "_s";
            string songName = "";
            List<PakEntry> pakEntries;
            if (fileNoExt.EndsWith(songCheck))
            {
                songName = fileNoExt.Substring(0, fileNoExt.IndexOf(songCheck));
            }
            else if (fileNoExt.EndsWith(songCheck2))
            {
                songName = fileNoExt.Substring(0, fileNoExt.IndexOf(songCheck2));
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
            return pakEntries;
        }
        
        public static void ProcessPAKFromFile(string file, bool convertQ = true)
        {
            string fileName = Path.GetFileName(file);
            string fileNoExt;
            try
            {
                fileNoExt = fileName.Substring(0, fileName.ToLower().IndexOf(".pak"));
            }
            catch
            {
                return;
            }
            string fileExt = Path.GetExtension(file);
            string folderPath = Path.GetDirectoryName(file);
            string NewFolderPath = Path.Combine(folderPath, fileNoExt);
            bool debugFile = fileName.Contains("dbg.pak");
            string masterFilePath = Path.Combine(NewFolderPath, "master.txt");

            var pakEntries = PakEntriesFromFilepath(file);

            foreach (PakEntry entry in pakEntries)
            {
                string pakFileName = entry.FullName;
                bool convToQ = (entry.Extension == DOT_QB && convertQ) ? true : false;

                if (convToQ)
                {
                    pakFileName = pakFileName.Substring(0, pakFileName.LastIndexOf('.')) + ".q";
                }
                else if (!pakFileName.EndsWith(fileExt, StringComparison.CurrentCultureIgnoreCase))
                {
                    pakFileName += fileExt;
                }

                string saveName = Path.Combine(NewFolderPath, pakFileName);

                Console.WriteLine($"Extracting {pakFileName}");
                Directory.CreateDirectory(Path.GetDirectoryName(saveName));

                if (convToQ)
                {
                    string songHeader = "";
                    if (fileNoExt.EndsWith("_song"))
                    {
                        songHeader = fileNoExt.Substring(0, fileNoExt.LastIndexOf("_song"));
                    }
                    else if (fileNoExt.EndsWith("_s"))
                    {
                        songHeader = fileNoExt.Substring(0, fileNoExt.LastIndexOf("_s"));
                    }
                    List<QBItem> qBItems = DecompileQb(entry.EntryData, GetEndian(fileExt), songHeader);
                    QbToText(qBItems, saveName);
                }
                else
                {
                    File.WriteAllBytes(saveName, entry.EntryData);
                }

                if (debugFile)
                {
                    Console.WriteLine($"Writing {pakFileName}");
                    string[] lines = File.ReadAllLines(saveName);
                    List<string> master = new List<string>();

                    using (StreamWriter masterFileWriter = File.AppendText(masterFilePath))
                    {
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("0x"))
                            {
                                string check = line.Substring(0, line.IndexOf(" "));
                                string stringData = line.Substring(line.IndexOf(" ")+1);
                                if (check == "0xf9e6c3fe")
                                {

                                }
                                if (!master.Contains(check))
                                {
                                    string crcCheck = QBKey(stringData);
                                    if (crcCheck == check)
                                    {
                                        master.Add(check);
                                        masterFileWriter.WriteLine(line);
                                    }
                                    
                                }
                                
                            }
                        }
                    }
                    File.Delete(saveName);
                }
            }
            Console.WriteLine("Success!");
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
            List<PakEntry> pakList = new List<PakEntry>();
            if (pabBytes != null)
            {
                uint pabType = CheckPabType(pakBytes, endian);
                switch (pabType)
                {
                    case 0:
                        //pakList = ExtractNewPak(pakBytes, pabBytes, endian, songName);
                        throw new Exception("PAK type not yet implemented.");
                    case uint size when size >= pakBytes.Length:
                        byte[] bytes = new byte[pabType + pabBytes.Length];
                        Array.Copy(pakBytes, 0, bytes, 0, pakBytes.Length);
                        Array.Copy(pabBytes, 0, bytes, pabType, pabBytes.Length);
                        pakBytes = bytes;
                        pakList = ExtractOldPak(pakBytes, endian, songName);
                        break;
                }
            }
            else
            {
                pakList = ExtractOldPak(pakBytes, endian, songName);
            }
            

            return pakList;
        }
        public static List<string> GetFilesFromFolder(string filePath)
        {
            List<string> files = new List<string>();
            if (Directory.Exists(filePath))
            {
                foreach (string file in Directory.GetFiles(filePath)) { files.Add(file); }
            }
            else if (File.Exists(filePath))
            {
                files.Add(filePath);
            }
            else
            {
                throw new Exception("Could not find valid file or folder to parse.");
            }
            return files;
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
                uint header_start = (uint)stream.Position; // To keep track of which entry since the offset in the header needs to be added to the StartOffset below
                PakEntry? entry = GetPakEntry(stream, reader, headers, header_start);
                if (entry == null)
                {
                    break;
                }
                try
                {
                    entry.EntryData = new byte[entry.FileSize];
                    Array.Copy(pakBytes, entry.StartOffset, entry.EntryData, 0, entry.FileSize);
                    if (entry.FullName == "0x00000000.0x00000000")
                    {
                        entry.FullName = entry.AssetContext;
                    }
                    // entry.FullName = entry.FullName.Replace(".qb", entry.Extension);
                    if (entry.FullName.IndexOf(entry.Extension, StringComparison.CurrentCultureIgnoreCase) == -1) 
                    {
                        GetCorrectExtension(entry);
                    }
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
            stream.Close();
            return PakList;
        }
        public static List<PakEntry> ExtractNewPak(byte[] pakBytes, byte[] pabBytes, string endian, string songName = "")
        {
            ReadWrite reader = new ReadWrite(endian);
            MemoryStream stream = new MemoryStream(pakBytes);
            List<PakEntry> PakList = new List<PakEntry>();
            Dictionary<uint, string> headers = DebugReader.MakeDictFromName(songName);

            while (true)
            {
                PakEntry? entry = GetPakEntry(stream, reader, headers, 0);
                if (entry == null)
                {
                    break;
                }
            }
            stream.Close();
            return PakList;
            }

        private static PakEntry? GetPakEntry(MemoryStream stream, ReadWrite reader, Dictionary<uint, string> headers, uint header_start = 0)
        {
            PakEntry entry = new PakEntry();
            uint extension = reader.ReadUInt32(stream);
            if (extension != 0x2cb3ef3b && extension != 0xb524565f)
            {
                entry.Extension = DebugReader.DebugCheck(headers, extension);
            }
            else
            {
                return null;
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
            int flags = reader.ReadInt32(stream);
            entry.Flags = flags;
            if ((flags & 0x20) != 0)
            {
                entry.ByteLength += 160;
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
                entry.FullName = tempString.Replace(DOTPS2, "", StringComparison.InvariantCultureIgnoreCase);
                stream.Position = skipTo;
            }
            return entry;
        }
        private static void GetCorrectExtension(PakEntry entry)
        {
            if (entry.Extension.IndexOf(DOT_QS) != -1)
            {
                // QS files are weird inside paks and ".qs" is found twice. Don't need it twice.
                entry.FullName += entry.Extension.Replace(DOT_QS, "");
            }
            else if (entry.Extension.IndexOf(DOT_SQB) != -1 || entry.Extension.IndexOf(DOT_MQB) != -1)
            {
                if (entry.FullName.IndexOf(DOT_QB) == -1)
                {
                    entry.FullName += DOT_QB;
                }
                entry.SetExtension(DOT_QB);
            }
            else
            {
                entry.FullName += entry.Extension;
            }
        }
        private static string GetEndian(string fileExt)
        {
            if (fileExt == DOTPS2)
            {
                return "little";
            }
            else
            {
                return "big";
            }
        }
        public class PakCompiler
        {
            public string Game { get; set; }
            public string? ConsoleType { get; set; }
            public bool IsQb {  get; set; }
            public bool Split {  get; set; }
            public string? AssetContext { get; set; }
            private ReadWrite Writer { get; set; }
            public PakCompiler(string game, bool isQb = false, bool split = false)
            {
                Game = game;
                IsQb = isQb; // Meaning qb.pak, really only used for PS2 to differentiate .qb files from .mqb files
                Split = split;
            }
            public PakCompiler(string game, string console, string? assetContext = null, bool isQb = false, bool split = false)
            {
                Game = game;
                IsQb = isQb; // Meaning qb.pak, really only used for PS2 to differentiate .qb files from .mqb files
                Split = split;
                ConsoleType = console;
                SetWriter();
                AssetContext = assetContext;
            }
            private void SetConsole(string filePath)
            {
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == DOTPS2)
                {
                    ConsoleType = CONSOLE_PS2;
                }
                else
                {
                    ConsoleType = CONSOLE_XBOX;
                }
                SetWriter();
                Console.WriteLine($"Compiling {ConsoleType} PAK file.");
            }
            private void SetWriter()
            {
                if (ConsoleType == CONSOLE_PS2)
                {
                    Writer = new ReadWrite("little");
                }
                else
                {
                    Writer = new ReadWrite("big");
                }
            }
            public (byte[]? itemData, byte[]? otherData) CompilePAK(string folderPath, string console = "")
            {
                if (!Directory.Exists(folderPath))
                {
                    throw new NotSupportedException("Argument given is not a folder.");
                }

                string[] entriesRaw = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                List<string> rootFiles = new List<string>();
                List<string> otherFiles = new List<string>();
                foreach (string entry in entriesRaw)
                {
                    if (Path.GetDirectoryName(entry) == folderPath)
                    {
                        rootFiles.Add(entry);
                    }
                    else
                    {
                        otherFiles.Add(entry);
                    }
                }
                string[] entries = otherFiles.ToArray().Concat(rootFiles.ToArray()).ToArray();

                if (ConsoleType == null)
                {
                    if (console == CONSOLE_PS2)
                    {
                        SetConsole(DOTPS2);
                    }
                    else if (console == CONSOLE_XBOX)
                    {
                        SetConsole(DOTXEN);
                    }
                    else
                    {
                        for (int i = 0; i < entries.Length; i++)
                        {
                            if (File.Exists(entries[i]))
                            {
                                SetConsole(entries[i]);
                                break;
                            }
                        }
                    }
                }
                bool isPs2 = ConsoleType == CONSOLE_PS2;
                List<PakEntry> PakEntries = new List<PakEntry>();
                List<string> fileNames = new List<string>();
                

                foreach (string entry in entries)
                {
                    if (File.Exists(entry))
                    {
                        byte[] fileData;
                        string relPath = GetRelPath(folderPath, entry);
                        string qbName;
                        if (Path.GetExtension(entry) == DOT_Q)
                        {
                            List<QBItem> qBItems;
                            try
                            {
                                qBItems = ParseQFile(entry);
                                
                            }
                            catch (QFileParseException ex)
                            {
                                Console.WriteLine($"{relPath}: {ex.Message}");
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Q File compilation failed");
                                Console.WriteLine($"{relPath}: {ex.Message}");
                                throw;
                            }
                            //AddConsoleExt(ref relPath);
                            relPath += "b";
                            qbName = isPs2 ? DebugReader.Ps2PakString(relPath) : relPath;
                            fileData = CompileQbFile(qBItems, qbName, game: Game, console: ConsoleType!);
                        }
                        else if ((Path.GetExtension(entry) == DOT_QB))
                        {
                            //AddConsoleExt(ref relPath);
                            fileData = File.ReadAllBytes(entry);
                        }
                        else
                        {
                            fileData = File.ReadAllBytes(entry);
                        }
                        PakEntry pakEntry = new PakEntry(fileData, ConsoleType, AssetContext);
                        
                        pakEntry.SetFullFlagPath(relPath);
                        pakEntry.SetNameNoExt(GetFileNoExt(Path.GetFileName(relPath)));
                        pakEntry.SetExtension(GetFileExt(relPath));
                        pakEntry.SetFlags();
                        pakEntry.SetNames(IsQb);

                        if (!fileNames.Contains(pakEntry.NameNoExt))
                        {
                            fileNames.Add(pakEntry.NameNoExt);
                        }
                        else
                        {

                        }
                        PakEntries.Add(pakEntry);

                    }
                }
                
                var (pakData, pabData) = CompilePakEntries(PakEntries);
                
                return (pakData, pabData);

            }
            public (byte[], byte[]?) CompilePakEntries(List<PakEntry> PakEntries)
            {
                PakEntries.Add(new PakEntry(ConsoleType, Game, AssetContext)); // Last entry
                byte[] pakData;
                byte[]? pabData;
                using (MemoryStream pak = new MemoryStream())
                using (MemoryStream pab = new MemoryStream())
                {

                    int pakSize = PakEntries.Sum(item => item.ByteLength);
                    if (ConsoleType == CONSOLE_PS2)
                    {
                        pakSize += 16;
                    }
                    else
                    {
                        pakSize += (4096 - pakSize % 4096);
                    }
                    int bytesPassed = 0;
                    foreach (PakEntry entry in PakEntries)
                    {
                        entry.StartOffset = (uint)(pakSize - bytesPassed + pab.Position);
                        pak.Write(Writer.ValueHex(entry.Extension), 0, 4);
                        pak.Write(Writer.ValueHex(entry.StartOffset), 0, 4);
                        pak.Write(Writer.ValueHex(entry.FileSize), 0, 4);
                        pak.Write(Writer.ValueHex(entry.AssetContext), 0, 4);
                        pak.Write(Writer.ValueHex(entry.FullName), 0, 4);
                        pak.Write(Writer.ValueHex(entry.NameNoExt), 0, 4);
                        pak.Write(Writer.ValueHex(entry.Parent), 0, 4);
                        pak.Write(Writer.ValueHex(entry.Flags), 0, 4);
                        if ((entry.Flags & 0x20) != 0)
                        {
                            ReadWrite.WriteStringBytes(pak, entry.FullFlagPath);
                        }
                        pab.Write(entry.EntryData);
                        Writer.PadStreamTo(pab, 16);
                        bytesPassed += entry.ByteLength;
                    }
                    if (ConsoleType == CONSOLE_PS2)
                    {
                        ReadWrite.WriteStringBytes(pak, "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
                    }
                    else
                    {
                        Writer.PadStreamTo(pak, 4096);
                    }
                    pabData = pab.ToArray();
                    if (!Split)
                    {
                        // Add pabData to pakData and make pabData null
                        pak.Write(pabData);
                        pabData = null;
                    }
                    pakData = pak.ToArray();

                }
                return (pakData, pabData);
            }
            public (byte[], byte[]?) CompilePakFromDictionary(Dictionary<string, PakEntry> pakDict)
            {
                List<PakEntry> pakEntries = new List<PakEntry>();
                foreach (var entry in pakDict)
                {
                    pakEntries.Add(entry.Value);
                }
                var (pakData, pabData) = CompilePakEntries(pakEntries);
                return (pakData, pabData);
            }
            private void AddConsoleExt(ref string filePath)
            {
                if (ConsoleType == CONSOLE_PS2)
                {
                    filePath += ".ps2";
                }
                else
                {
                    filePath += ".xen";
                }
            }
            private string GetFileExt(string path)
            {
                string fileEnd = path.Substring(path.LastIndexOf('.')).ToLower();
                string extension;
                switch (fileEnd)
                {
                    case DOTNGC:
                    case DOTPS2:
                    case DOTPS3:
                    case DOTXEN:
                        extension = Path.GetExtension(Path.GetFileNameWithoutExtension(path)).ToLower();
                        break;
                    case DOTEN:
                    case DOTDE:
                    case DOTES:
                    case DOTIT:
                    case DOTFR:
                        if (path.IndexOf(DOT_QS) != -1)
                        {
                            extension = $"{DOT_QS}{fileEnd}";
                        }
                        else
                        {
                            extension = Path.GetExtension(path).ToLower();
                        }
                        //extension = $"{DOT_QS}{fileEnd}";
                        break;
                    default:
                        extension = Path.GetExtension(path).ToLower();
                        break;
                }
                if (ConsoleType == CONSOLE_PS2 && extension == DOT_QB && !IsQb)
                {
                    if (path.IndexOf("_scripts") != -1 && path.IndexOf("song_scripts") == -1)
                    {
                        extension = DOT_SQB;
                    }
                    else
                    {
                        extension = DOT_MQB;
                    }
                }
                return extension;
            }
            private string GetFileNoExt(string path)
            {
                string noExt = Path.GetFileNameWithoutExtension(path);
                while (Path.GetFileNameWithoutExtension(noExt) != noExt)
                {
                    noExt = Path.GetFileNameWithoutExtension(noExt);
                }
                return noExt;
            }
            private string GetRelPath(string folderPath, string entry)
            {
                string relPath = Path.GetRelativePath(folderPath, entry).ToLower();
                if (ConsoleType == CONSOLE_PS2)
                {
                    return relPath;
                }
                if (relPath.EndsWith(".xen") || relPath.EndsWith(".ps3"))
                {
                    relPath = relPath.Substring(0, relPath.Length - 4);
                }


                return relPath; 
            }
        }
        public static (string pakSavePath, bool doubleKick) CreateSongPackage(string midiPath, 
            string savePath, 
            string songName, 
            string game, 
            string gameConsole,  
            string skaPath = "", 
            string perfOverride = "", 
            string songScripts = "", 
            string skaSource = "GHWT", 
            string venueSource = "",
            int hopoThreshold = 170,
            int hopoType = 0,
            bool rhythmTrack = false, 
            bool overrideBeat = false,
            bool isSteven = false,
            bool easyOpens = false,
            Dictionary<string, int>? diffs = null
            )
        {
            var midiFile = new SongQbFile(
                midiPath, 
                songName: songName, 
                game: game, 
                console: gameConsole, 
                hopoThreshold: hopoThreshold, 
                perfOverride: perfOverride, 
                songScriptOverride: songScripts, 
                venueSource:venueSource, 
                rhythmTrack: rhythmTrack, 
                overrideBeat: overrideBeat, 
                hopoType: hopoType,
                easyOpens: easyOpens,
                skaPath: skaPath);

            var saveName = Path.Combine(savePath, $"{songName}_{gameConsole}");
            string pakFolder = gameConsole == CONSOLE_PS2 ? "data\\songs" : "songs";
            var songFolder = Path.Combine(saveName, pakFolder);
            string consoleExt = gameConsole == CONSOLE_PS2 ? DOTPS2 : DOTXEN;
            var qbSave = Path.Combine(songFolder, songName + $".mid.qb{consoleExt}");

            byte[]? songScriptsQb;

            Directory.CreateDirectory(songFolder);

            var midQb = midiFile.ParseMidiToQb();
            // Check for song scripts override and make one if we're not compiling a PS2 song
            if (gameConsole != CONSOLE_PS2)
            {
                bool isNew = game == GAME_GH5 || game == GAME_GHWOR;
                string scriptName = isNew ? ".perf.xml" : "_song_scripts";
                string songScriptsSave = Path.Combine(songFolder, songName + $"{scriptName}.qb{consoleExt}");
                songScriptsQb = midiFile.MakeSongScripts();
                if (songScriptsQb != null)
                {
                    File.WriteAllBytes(songScriptsSave, songScriptsQb);
                }
            }
            if (game == GAME_GHWOR || game == GAME_GH5)
            {
                var noteBytes = midiFile.MakeGh5Notes();
                var perfBytes = midiFile.MakeGh5Perf();
                string noteSave = Path.Combine(songFolder, songName + ".note" + consoleExt);
                string perfSave = Path.Combine(songFolder, songName + ".perf" + consoleExt);
                File.WriteAllBytes(noteSave, noteBytes);
                File.WriteAllBytes(perfSave, perfBytes);
                if (diffs != null)
                {
                    midiFile.SetEmptyTracksToDiffZero(diffs);
                }
            }

            var errors = midiFile.GetErrorListAsString();
            var warnings = midiFile.GetWarningListAsString();

            if (!string.IsNullOrEmpty(errors))
            {
                throw new MidiCompileException(errors);
            }
            if (!string.IsNullOrEmpty(warnings))
            {
                Console.WriteLine("WARNINGS:");
                Console.WriteLine(warnings);
            }

            File.WriteAllBytes(qbSave, midQb);


            bool ps2SkaProcessed = false;
            byte[]? skaScripts = null;
            if (Directory.Exists(skaPath))
            {
                float skaMultiplier;
                if (game == skaSource)
                {
                    skaMultiplier = 1.0f;
                }
                else if (game == GAME_GH3)
                {
                    skaMultiplier = 0.5f;
                }
                else
                {
                    skaMultiplier = skaSource == GAME_GH3 ? 2.0f : 1.0f;
                }
                var skaFiles = Directory.GetFiles(skaPath);
                string skaEndian = gameConsole == CONSOLE_XBOX ? "big" : "little";
                foreach (var skaFile in skaFiles)
                {
                    var skaTest = new SkaFile(skaFile, "big");

                    string skaPatternGuit = @"\d+b\.ska(\.xen)?$";
                    string skaPatternSing = @"\d\.ska(\.xen)?$";

                    bool isGuitarist = Regex.IsMatch(skaFile, skaPatternGuit);

                    string skaType;
                    switch (game)
                    {
                        case GAME_GH3:
                            if (isGuitarist && gameConsole != CONSOLE_PS2)
                            {
                                skaType = SKELETON_GH3_GUITARIST;
                            }
                            else if (Regex.IsMatch(skaFile, skaPatternSing))
                            {
                                skaType = gameConsole == CONSOLE_PS2 ? SKELETON_GH3_SINGER_PS2 : SKELETON_GH3_SINGER;
                            }
                            else
                            {
                                continue;
                            }
                            break;
                        case GAME_GHA:
                            skaType = isGuitarist ? SKELETON_GH3_GUITARIST : (isSteven ? SKELETON_STEVE : SKELETON_GHA_SINGER);
                            break;
                        default:
                            skaType = SKELETON_WT_ROCKER;
                            break;
                    }
                    
                    byte[] convertedSka;
                    string skaSave;
                    if (game == GAME_GH3 || game == GAME_GHA)
                    {
                        if (gameConsole == CONSOLE_PS2)
                        {
                            if (!ps2SkaProcessed)
                            {
                                skaScripts = midiFile.MakePs2SkaScript();
                                ps2SkaProcessed = true;
                            }
                            convertedSka = skaTest.WritePs2StyleSka();
                            string skaFolderPs2 = Path.Combine(savePath, "PS2 SKA Files");
                            skaSave = Path.Combine(skaFolderPs2, Path.GetFileName(skaFile).Replace(DOTXEN, DOTPS2));
                            Directory.CreateDirectory(skaFolderPs2);
                        }
                        else
                        {
                            convertedSka = skaTest.WriteGh3StyleSka(skaType, skaMultiplier);
                            skaSave = Path.Combine(saveName, Path.GetFileName(skaFile));
                        }
                    }
                    else
                    {
                        convertedSka = skaTest.WriteModernStyleSka(skaType, game, skaMultiplier);
                        skaSave = Path.Combine(saveName, Path.GetFileName(skaFile));
                    }
                    if (convertedSka.Length > 0)
                    {
                        File.WriteAllBytes(skaSave, convertedSka);
                    }
                }

                if (skaScripts != null)
                {
                    string ps2SkaScriptSave = Path.Combine(songFolder, songName + $"_song_scripts.qb{consoleExt}");
                    File.WriteAllBytes(ps2SkaScriptSave, skaScripts);
                }
            }

            // Make qs file for GHWT+
            var qsList = midiFile.QsList;
            if (qsList.Count > 0)
            {
                List<string> qsSaves = new List<string>();
                if (game == GAME_GHWT)
                {
                    qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs{consoleExt}"));
                }
                else
                {
                    qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.de{consoleExt}"));
                    qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.en{consoleExt}"));
                    qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.es{consoleExt}"));
                    qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.fr{consoleExt}"));
                    qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.it{consoleExt}"));
                }

                var sortedKeys = qsList.OrderBy(entry => entry.Value)
                                               .Select(entry => entry.Key)
                                               .ToList();

                foreach (string qsSave in qsSaves)
                {
                    // Creating a StreamWriter to write to the file with UTF-16 encoding
                    using (StreamWriter writer = new StreamWriter(qsSave, false, Encoding.Unicode))
                    {
                        // Setting the newline character to only '\n'
                        writer.NewLine = "\n";

                        foreach (var key in sortedKeys)
                        {
                            // Formatting the key as specified
                            string modifiedKey = key.Substring(2).PadLeft(8, '0');

                            // Building the line with the modified key and its value
                            string line = $"{modifiedKey} \"{qsList[key]}\"";

                            // Writing the line to the file
                            writer.WriteLine(line);
                        }

                        // These are needed otherwise the game will crash.
                        writer.WriteLine();
                        writer.WriteLine();
                    }
                }
            }

            bool doubleKick = midiFile.DoubleKick;
            string? assetContext = game == GAME_GHWOR ? songName : null;
            var pakCompiler = new PAK.PakCompiler(game: game, console: gameConsole, assetContext: assetContext);
            var (pakData, pabData) = pakCompiler.CompilePAK(saveName);
            string songPrefix = gameConsole == CONSOLE_PS2 ? "" : "_song";
            var pakSave = Path.Combine(savePath, songName + $"{songPrefix}.pak{consoleExt}");
            File.WriteAllBytes(pakSave, pakData);

            // Clean up the compile folder, just in case they want to compile for other games
            // The games are grossly incompatible with each other
            // Deleting the folder is safer
            Directory.Delete(saveName, true);

            return (pakSave, doubleKick);
        }
    }
}
