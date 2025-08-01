﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using GH_Toolkit_Core.Debug;
using GH_Toolkit_Core.Methods;
using static GH_Toolkit_Core.PAK.PAK;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.Checksum.CRC;
using static GH_Toolkit_Core.PAK.PAKVariables;
using static System.Net.Mime.MediaTypeNames;
using static GH_Toolkit_Core.Methods.GlobalVariables;
using static GH_Toolkit_Core.Methods.GlobalHelpers;
using System.Reflection.PortableExecutable;
using GH_Toolkit_Core.MIDI;
using GH_Toolkit_Core.SKA;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.Xml;
using System.Text;
using static GH_Toolkit_Core.Methods.Exceptions;
using GH_Toolkit_Core.QB;
using GH_Toolkit_Core.Checksum;
using Melanchall.DryWetMidi.Interaction;
using static GH_Toolkit_Core.Debug.DebugReader;
using Microsoft.VisualBasic;
using NAudio.Lame;
using System.IO;
using System.Net.NetworkInformation;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;
using Instances.Exceptions;

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
            public string ConsoleType { get; set; }
            private string Game { get; set; }
            public bool isPs2orWii
            {
                get
                {
                    return ConsoleType == CONSOLE_PS2 || ConsoleType == CONSOLE_WII;
                }
            }
            public int ByteLength { get; set; } = 32;
            private string PakName
            {
                get
                {
                    return FullName == FLAGBYTE ? AssetContext! : FullName!;
                }
            }
            public PakEntry(string game = "GH3")
            {

            }
            public PakEntry(string console, string game, string? assetContext = null)
            {
                if ((console == CONSOLE_PS2 || console == CONSOLE_WII) && (game == GAME_GH3))
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
            public void SetExtension(string extension, bool isNq = false)
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
            public void SetAssetContext(string asset)
            {
                AssetContext = QBKey(asset);
            }
            public void SetGame(string game)
            {
                Game = game;
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
                if (isPs2orWii && !FullFlagPath!.StartsWith(HEXSTART))
                {
                    if ((Flags & 0x20) != 0)
                    {
                        if (FullFlagPath!.ToLower().IndexOf(DOTPS2) == -1 && FullFlagPath!.ToLower().IndexOf(DOTNGC) == -1)
                        {
                            if (ConsoleType == CONSOLE_PS2)
                            {
                                FullFlagPath += DOTPS2;
                            }
                            else
                            {
                                FullFlagPath += DOTNGC;
                            }
                        }
                        FullName = FullFlagPath;
                        FullFlagPath = FullFlagPath.PadRight(160, '\0');
                        ByteLength += 160;
                    }
                    if (!isQb && (FullFlagPath.EndsWith(DOTPS2) || FullFlagPath.EndsWith(DOTNGC)))
                    {
                        FullName = FullFlagPath.Substring(0, FullFlagPath.Length - 4);
                        if (FullName.IndexOf(DOT_MID_QB) != -1)
                        {
                            FullName = FullFlagPath.Substring(0, FullFlagPath.IndexOf(DOT_QB));
                        }
                    }
                    else if (!(FullFlagPath.EndsWith(DOTPS2) || FullFlagPath.EndsWith(DOTNGC)))
                    {
                        FullName = FullFlagPath;
                    }

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
                    if (isPs2orWii)
                    {
                        if (FullFlagPath!.ToLower().IndexOf(DOTPS2) == -1 && FullFlagPath!.ToLower().IndexOf(DOTNGC) == -1)
                        {
                            if (ConsoleType == CONSOLE_PS2)
                            {
                                FullFlagPath += DOTPS2;
                            }
                            else
                            {
                                FullFlagPath += DOTNGC;
                            }
                        }
                        FullFlagPath = FullFlagPath.PadRight(160, '\0');
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

                if (!isPs2orWii && AssetContext == null)
                {
                    AssetContext = FLAGBYTE;
                }
            }
            public void SetFlags()
            {
                if (isPs2orWii && Game != GAME_GHWOR)
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
            public void OverwriteFlags(int flagValue)
            {
                Flags = flagValue;
            }
            public void OverwriteData(byte[] data)
            {
                EntryData = data;
                FileSize = (uint)EntryData.Length;
            }
            public string GetFullName()
            {
                if (FullName.StartsWith(HEXSTART))
                {
                    var nameSplit = FullName.Split(".");
                    return $"{nameSplit[0]}";
                }
                else
                {
                    return FullName;
                }
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
        public static Dictionary<string, PakEntry> PakEntryQbDictFromFile(string file)
        {
            var pakEntries = PakEntriesFromFilepath(file);
            Dictionary<string, PakEntry> pakDict = new Dictionary<string, PakEntry>();
            foreach (var entry in pakEntries)
            {
                pakDict.Add(QBKey(entry.FullName), entry);
            }
            return pakDict;
        }
        public static List<PakEntry>? PakEntriesFromFilepath(string file)
        {
            string fileName = Path.GetFileName(file);
            if (fileName.IndexOf(".pab", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                return null;
            }
            if (fileName.IndexOf(".pak", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                throw new Exception("Invalid File");
            }

            string fileNoExt = fileName.Substring(0, fileName.ToLower().IndexOf(".pak"));
            string vramFile = fileNoExt + "_vram.pak.ps3".ToUpper();
            string fileExt = Path.GetExtension(file).ToLower();
            Console.WriteLine($"Processing {fileNoExt}");
            string folderPath = Path.GetDirectoryName(file);
            string NewFolderPath = Path.Combine(folderPath, fileNoExt);
            string vramPath = Path.Combine(folderPath, vramFile);
            byte[]? vramBytes = null;
            if (File.Exists(vramPath) && fileExt == DOTPS3)
            {
                vramBytes = File.ReadAllBytes(vramPath);
            }
            string songCheck = "_song";
            string songCheck2 = "_s";
            string songName = fileNoExt;
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
            bool isWiiorPS2 = fileName.EndsWith(".ngc", StringComparison.InvariantCultureIgnoreCase);
            string endian;
            var pakExtractor = new PakExtractor(fileExt, songName);
            if (fileExt == ".ps2")
            {
                endian = "little";
                isWiiorPS2 = true;
            }
            else
            {
                endian = "big";
                fileExt = ".xen";
            }
            
            try
            {
                pakEntries = pakExtractor.ExtractPAK(test_pak, test_pab, vramBytes);
            }
            catch (Exception ex)
            {
                try
                {
                    test_pak = Compression.DecompressData(test_pak);
                }
                catch
                {
                    // PAK file can be compressed, but doesn't need to be. If ZLIB decompression fails, try to extract as is.
                }
                if (test_pab != null)
                {
                    test_pab = Compression.DecompressData(test_pab);
                }
                pakEntries = pakExtractor.ExtractPAK(test_pak, test_pab, vramBytes);
            }
            return pakEntries;
        }
        public static void MakeQsFilesForSplitPak(string folderPath, string saveToFolder, string console, string game, Dictionary<uint, string> qsStrings, bool edat = false)
        {
            if (qsStrings == null || qsStrings.Count == 0)
            {
                Console.WriteLine("No QS strings provided.");
                return;
            }

            folderPath = folderPath += "_qs_files_temp";
            string extension = GetConsoleExtension(console);

            string qsName = "qs";
            string qsNameF = "qs_f";
            string qsNameG = "qs_g";
            string qsNameI = "qs_i";
            string qsNameS = "qs_s";

            string qsSaveName = "qs.pak" + extension;
            string qsSaveNameF = "qs_f.pak" + extension;
            string qsSaveNameG = "qs_g.pak" + extension;
            string qsSaveNameI = "qs_i.pak" + extension;
            string qsSaveNameS = "qs_s.pak" + extension;

            string qsPath = Path.Join(folderPath, qsName);
            string qsPathF = Path.Join(folderPath, qsNameF);
            string qsPathG = Path.Join(folderPath, qsNameG);
            string qsPathI = Path.Join(folderPath, qsNameI);
            string qsPathS = Path.Join(folderPath, qsNameS);

            if (console == "PS3")
            {
                if (edat)
                {
                    qsName += ".edat";
                    qsNameF += ".edat";
                    qsNameG += ".edat";
                    qsNameI += ".edat";
                    qsNameS += ".edat";
                }
                qsPath = Path.Join(folderPath, qsName.ToUpper());
                qsPathF = Path.Join(folderPath, qsNameF.ToUpper());
                qsPathG = Path.Join(folderPath, qsNameG.ToUpper());
                qsPathI = Path.Join(folderPath, qsNameI.ToUpper());
                qsPathS = Path.Join(folderPath, qsNameS.ToUpper());
            }


            var qsDictF = new Dictionary<uint, string>();
            var qsDictG = new Dictionary<uint, string>();
            var qsDictI = new Dictionary<uint, string>();
            var qsDictS = new Dictionary<uint, string>();

            foreach (var qsString in qsStrings)
            {
                var valF = GetQsKeyFromDict(qsString.Key, QsDict.Fr) ?? qsString.Value;
                var valG = GetQsKeyFromDict(qsString.Key, QsDict.De) ?? qsString.Value;
                var valI = GetQsKeyFromDict(qsString.Key, QsDict.It) ?? qsString.Value;
                var valS = GetQsKeyFromDict(qsString.Key, QsDict.Es) ?? qsString.Value;

                qsDictF[qsString.Key] = valF;
                qsDictG[qsString.Key] = valG;
                qsDictI[qsString.Key] = valI;
                qsDictS[qsString.Key] = valS;
            }

            var qsFileSave = Path.Combine(qsPath, "english.qs.xen");
            var qsFileSaveF = Path.Combine(qsPathF, "french.qs.xen");
            var qsFileSaveG = Path.Combine(qsPathG, "german.qs.xen");
            var qsFileSaveI = Path.Combine(qsPathI, "italian.qs.xen");
            var qsFileSaveS = Path.Combine(qsPathS, "spanish.qs.xen");
            try
            {
                ReadWrite.WriteQsFileFromDict(qsFileSave, qsStrings);
                ReadWrite.WriteQsFileFromDict(qsFileSaveF, qsDictF);
                ReadWrite.WriteQsFileFromDict(qsFileSaveG, qsDictG);
                ReadWrite.WriteQsFileFromDict(qsFileSaveI, qsDictI);
                ReadWrite.WriteQsFileFromDict(qsFileSaveS, qsDictS);

                var qsCompiler = new PakCompiler(game, console, null, false, false);
                var pakString = ".pak" + extension;
                if (console == "PS3")
                {
                    pakString = pakString.ToUpper();
                }

                foreach (var qsFolder in new[] { qsPath, qsPathF, qsPathG, qsPathI, qsPathS })
                {
                    var (qsPak, _, _) = qsCompiler.CompilePAK(qsFolder, console);
                    string toSave = qsFolder + pakString;
                    using (FileStream qsPakFile = new FileStream(toSave, FileMode.Create, FileAccess.Write))
                    {
                        qsPakFile.Write(qsPak);
                    }
                    // Move the qsPak file to the rootFolder
                    var newQsSave = Path.Combine(saveToFolder, Path.GetFileName(toSave));
                    File.Move(toSave, newQsSave, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing QS files: " + ex.Message);
                throw;
            }
            finally
            {
                // Clean up the directories if they were created
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
            }
        }
        private static void CheckForQsFilesInPak(List<PakEntry> entries) 
        {
            
            foreach (PakEntry entry in entries)
            {
                if (entry.Extension.Contains(DOT_QS, StringComparison.CurrentCultureIgnoreCase))
                {
                    string tempSavePath = Path.Combine(ExeRootFolder, entry.FullName);
                    if (!Directory.Exists(Path.GetDirectoryName(tempSavePath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(tempSavePath)!);
                    }
                    File.WriteAllBytes(tempSavePath, entry.EntryData);
                    QsDict dictMod;
                    switch (Path.GetExtension(entry.Extension))
                    {
                        case DOTFR:
                            dictMod = QsDict.Fr;
                            break;
                        case DOTDE:
                            dictMod = QsDict.De;
                            break;
                        case DOTIT:
                            dictMod = QsDict.It;
                            break;
                        case DOTES:
                            dictMod = QsDict.Es;
                            break;
                        case DOTEN:
                        default:
                            dictMod = QsDict.En;
                            break;
                    }
                    AddToQsDictTemp(tempSavePath, dictMod);
                    File.Delete(tempSavePath);
                }
            }
        }
        private static 
            (
            Dictionary<uint, string>, 
            Dictionary<uint, string>, 
            Dictionary<uint, string>, 
            Dictionary<uint, string>, 
            Dictionary<uint, string>
            ) 
        CheckForQsFilesInFolder(string[] entries, out bool localQsFiles)
        {
            localQsFiles = false;
            var qsEn = new Dictionary<uint, string>();
            var qsFr = new Dictionary<uint, string>();
            var qsDe = new Dictionary<uint, string>();
            var qsIt = new Dictionary<uint, string>();
            var qsEs = new Dictionary<uint, string>();
            if (entries == null || entries.Length == 0)
            {
                return (qsEn, qsFr, qsDe, qsIt, qsEs);
            }

            Dictionary<uint, string>? currDict = null;

            foreach (string entry in entries)
            {
                if (File.Exists(entry) && entry.Contains(DOT_QS, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (entry.Contains(DOT_QS + DOTEN, StringComparison.CurrentCultureIgnoreCase))
                    {
                        currDict = qsEn;
                    }
                    else if (entry.Contains(DOT_QS + DOTFR, StringComparison.CurrentCultureIgnoreCase))
                    {
                        currDict = qsFr;
                    }
                    else if (entry.Contains(DOT_QS + DOTDE, StringComparison.CurrentCultureIgnoreCase))
                    {
                        currDict = qsDe;
                    }
                    else if (entry.Contains(DOT_QS + DOTIT, StringComparison.CurrentCultureIgnoreCase))
                    {
                        currDict = qsIt;
                    }
                    else if (entry.Contains(DOT_QS + DOTES, StringComparison.CurrentCultureIgnoreCase))
                    {
                        currDict = qsEs;
                    }
                    else
                    {
                        currDict = qsEn;
                    }
                    var textLines = File.ReadAllLines(entry, Encoding.BigEndianUnicode);
                    foreach (var line in textLines)
                    {
                        var newLine = line.TrimEnd('\n').Split(new[] { ' ' }, 2);

                        if (newLine.Length != 2) continue;

                        try
                        {
                            var key = Convert.ToUInt32(newLine[0], 16);
                            var value = newLine[1].Replace("\"", "");
                            currDict[key] = value;
                        }
                        catch
                        {
                            // If an exception occurs, ignore and continue processing the next line.
                        }
                    }
                }

                // Find all keys in other dictionaries that are not in qsEn
                var allKeys = new HashSet<uint>(qsEn.Keys);
                var otherDicts = new[] { qsFr, qsDe, qsIt, qsEs };

                foreach (var dict in otherDicts)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (!allKeys.Contains(key))
                        {
                            allKeys.Add(key);
                        }
                    }
                }

                // Ensure all dictionaries have all keys
                foreach (var key in allKeys)
                {
                    // If qsEn doesn't have this key, add it with an empty string
                    if (!qsEn.ContainsKey(key))
                    {
                        if (qsFr.ContainsKey(key))
                        {
                            qsEn[key] = qsFr[key];
                        }
                        else if (qsDe.ContainsKey(key))
                        {
                            qsEn[key] = qsDe[key];
                        }
                        else if (qsIt.ContainsKey(key))
                        {
                            qsEn[key] = qsIt[key];
                        }
                        else if (qsEs.ContainsKey(key))
                        {
                            qsEn[key] = qsEs[key];
                        }
                        else
                        {
                            qsEn[key] = string.Empty; // Default to empty string if no value found
                            // This should not happen though...
                        }
                    }

                    // Ensure all other dictionaries have this key
                    foreach (var dict in otherDicts)
                    {
                        if (!dict.ContainsKey(key))
                        {
                            dict[key] = qsEn[key];
                        }
                    }
                }
            }

            return (qsEn, qsFr, qsDe, qsIt, qsEs);
        }
        public static void ProcessPAKFromFile(string file, bool convertQ = true, string game = "")
        {
            file = Path.GetFullPath(file);
            string fileName = Path.GetFileName(file);
            string fileNoExt;

            try
            {
                fileNoExt = fileName.Substring(0, fileName.ToLower().IndexOf(".pak"));
            }
            catch
            {
                Console.WriteLine("Invalid File, skipping");
                return;
            }
            string fileExt = Path.GetExtension(file);
            string consoleType = fileExt.Replace(".", "");
            string folderPath = Path.GetDirectoryName(file);
            string NewFolderPath = Path.Combine(folderPath, fileNoExt);
            if (NewFolderPath.EndsWith("_VRAM", StringComparison.InvariantCultureIgnoreCase))
            {
                return; // Skip VRAM files, they are already looked for
            }
            bool debugFile = fileName.Contains("dbg.pak");
            string masterFilePath = Path.Combine(NewFolderPath, "master.txt");

            var pakEntries = PakEntriesFromFilepath(file);
            var parents = new Dictionary<string, string>();
            var flags = new Dictionary<string, int>();
            var assetContexts = new Dictionary<string, string>();

            if (fileNoExt.ToUpper() != "QS")
            {
                CheckForQsFilesInPak(pakEntries);
            }
            

            foreach (PakEntry entry in pakEntries)
            {
                if (entry.Parent != 0)
                {
                    var fullName = entry.GetFullName();
                    var parentString = DebugReader.DbgString(entry.Parent);
                    parents.Add(fullName, parentString);
                }
                if (entry.Flags != 0)
                {
                    flags.Add(entry.GetFullName(), entry.Flags);
                }
                if (entry.AssetContext != null && entry.AssetContext != FLAGBYTE)
                {
                    assetContexts.Add(entry.GetFullName(), entry.AssetContext);
                }
                string pakFileName = entry.FullName;
                bool convToQ = ((entry.Extension == DOT_QB || entry.Extension == DOT_NQB) && convertQ) ? true : false;

                if (convToQ)
                {
                    string addN = entry.Extension == DOT_NQB ? "n" : "";
                    pakFileName = pakFileName.Substring(0, pakFileName.LastIndexOf('.')) + $".{addN}q";
                }
                else if (!pakFileName.EndsWith(fileExt, StringComparison.CurrentCultureIgnoreCase))
                {
                    pakFileName += fileExt;
                }

                if (!OperatingSystem.IsWindows()) //Without this block it will write files such as "engine\folder\whatever.q" instead of creating folders on mac/linux
                {
                    pakFileName = Regex.Replace(pakFileName, @"\\", "/");
                }

                var uri = new Uri(Path.Combine(NewFolderPath, pakFileName));
                string saveName = uri.LocalPath;

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
                    List<QBItem> qBItems = DecompileQb(entry.EntryData, GetEndian(fileExt), songHeader, game, consoleType);
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
                                string stringData = line.Substring(line.IndexOf(" ") + 1);
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
            var configFolder = Path.Combine(NewFolderPath, "pak_config");
            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }
            if (parents.Count > 0)
            {
                var parentFile = Path.Combine(configFolder, "parents.config");
                using (StreamWriter masterFileWriter = File.CreateText(parentFile))
                {
                    foreach (var entry in parents)
                    {
                        masterFileWriter.WriteLine($"{entry.Key}\t{entry.Value}");
                    }
                }
                var fileOrder = Path.Combine(configFolder, "file_order.config");
                using (StreamWriter masterFileWriter = File.CreateText(fileOrder))
                {
                    foreach (var entry in pakEntries)
                    {
                        if (entry.Extension == DOT_NQB)
                        {
                            entry.SetFullName(entry.FullName.Replace(DOT_NQB, DOT_QB)); 
                        }
                        masterFileWriter.WriteLine($"{entry.GetFullName()}");
                    }
                }
            }
            if (flags.Count > 0)
            {
                var flagFile = Path.Combine(configFolder, "flags.config");
                using (StreamWriter masterFileWriter = File.CreateText(flagFile))
                {
                    foreach (var entry in flags)
                    {
                        masterFileWriter.WriteLine($"{entry.Key}\t{entry.Value}");
                    }
                }
            }
            if (assetContexts.Count > 0)
            {
                var assetFile = Path.Combine(configFolder, "asset_contexts.config");
                using (StreamWriter masterFileWriter = File.CreateText(assetFile))
                {
                    foreach (var entry in assetContexts)
                    {
                        masterFileWriter.WriteLine($"{entry.Key}\t{entry.Value}");
                    }
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
        public static byte[] DeflateData(string filepath)
        {
            byte[] magicBytes = new byte[] { 0x43, 0x48, 0x4E, 0x4B }; // "CHNK" in ASCII

            byte[] data = File.ReadAllBytes(filepath);

            try
            {
                byte[]? deflated = null;
                if (data.Length >= 4 && data[0] == magicBytes[0] && data[1] == magicBytes[1] && data[2] == magicBytes[2] && data[3] == magicBytes[3])
                {
                    deflated = Compression.DecompressWTPak(data);
                }
                else
                {
                    deflated = Compression.DecompressData(data);
                }
                return deflated;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not deflate file. Is it already uncompressed?");
            }
            return data;
        }
        public class PakExtractor
        {
            public string Platform { get; set; }
            public string Extension { get; set; }
            public string Endian { get; set; }
            public string PakName { get; set; }
            public bool IsWiiOrPs2
            {
                get
                {
                    return Platform == CONSOLE_PS2 || Extension == CONSOLE_WII;
                }
            }
            public PakExtractor(string ext, string pakName)
            {
                Extension = ext;
                PakName = pakName;
                switch (ext.ToLower())
                {
                    case DOTPS2:
                        Platform = CONSOLE_PS2;
                        Endian = "little";
                        break;
                    case DOTNGC:
                        Platform = CONSOLE_WII;
                        Endian = "big";
                        break;
                    case DOTPS3:
                        Platform = CONSOLE_PS3;
                        Endian = "big";
                        break;
                    case DOTXEN:
                        Platform = CONSOLE_XBOX;
                        Endian = "big";
                        break;
                }
            }
            public List<PakEntry> ExtractPAK(byte[] pakBytes, byte[]? pabBytes, byte[]? vramBytes = null)
            {
                ReadWrite reader = new ReadWrite(Endian);
                bool pakComp = Compression.isChnkCompressed(pakBytes);
                if (pakComp)
                {
                    pakBytes = Compression.DecompressWTPak(pakBytes);
                    if (vramBytes != null && vramBytes.Length > 0)
                    {
                        if (Compression.isChnkCompressed(vramBytes))
                        {
                            vramBytes = Compression.DecompressWTPak(vramBytes);
                        }
                    }
                }

                List<PakEntry> pakList = new List<PakEntry>();
                List<PakEntry> vramList = new List<PakEntry>();
                if (pabBytes != null)
                {
                    bool pabComp = Compression.isChnkCompressed(pabBytes);
                    uint pabStart = CheckPabType(pakBytes, Endian);
                    if (pabStart != 0 && pabStart < pakBytes.Length)
                    {
                        var newPak = new byte[pabStart];
                        Array.Copy(pakBytes, 0, newPak, 0, pabStart);
                        pakBytes = newPak;
                    }
                    if (pabStart != 0 && pabComp)
                    {
                        pabBytes = Compression.DecompressWTPak(pabBytes);
                    }

                    switch (pabStart)
                    {
                        case 0:
                            pakList = GetNewPakEntries(pakBytes, Endian, PakName);
                            var lastEntry = pakList[pakList.Count - 1];
                            var pabTest = pabBytes;
                            if (pabComp)
                            {
                                pabTest = Compression.DecompressWTPak(pabBytes);
                            }
                            bool isLarger = pabTest.Length >= (lastEntry.StartOffset + lastEntry.FileSize);
                            if (isLarger) // If this is the case, the entire pab file should be decompressed first
                            {
                                GetPakDataNew(pakList, pabTest);
                            }
                            else // If pak is not compressed, the pab file should be loaded in chunks
                            {
                                DecompressNewData(pakList, pabBytes);
                            }
                            //pakList = ExtractNewPak(pakBytes, pabBytes, endian, songName);
                            break;
                        case uint size when size >= pakBytes.Length:
                            try
                            {
                                byte[] bytes = new byte[pabStart + pabBytes.Length];
                                Array.Copy(pakBytes, 0, bytes, 0, pakBytes.Length);
                                Array.Copy(pabBytes, 0, bytes, pabStart, pabBytes.Length);
                                pakList = ExtractOldPak(bytes);
                                if (vramBytes != null && !IsWiiOrPs2)
                                {
                                    vramList = ExtractOldPak(vramBytes, vram: true);
                                }
                            }
                            catch (Exception ex)
                            {
                                var lzss = new Lzss();
                                // For lzss-compressed files (GH3 and GHA on PS3 only)
                                var pakEntries = GetNewPakEntries(pakBytes, Endian, PakName);
                                if (pakEntries == null)
                                {
                                    // PAK file is lzss compressed also
                                    pakBytes = lzss.Decompress(pakBytes);
                                }
                                pabBytes = lzss.Decompress(pabBytes);
                                pabStart = CheckPabType(pakBytes, Endian);
                                byte[] bytes = new byte[pabStart + pabBytes.Length];
                                Array.Copy(pakBytes, 0, bytes, 0, pakBytes.Length);
                                Array.Copy(pabBytes, 0, bytes, pabStart, pabBytes.Length);
                                pakList = ExtractOldPak(bytes);
                                if (vramBytes != null && !IsWiiOrPs2)
                                {
                                    vramBytes = lzss.Decompress(vramBytes);
                                    vramList = ExtractOldPak(vramBytes, vram: true);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    pakList = ExtractOldPak(pakBytes);
                    if (vramBytes != null && !IsWiiOrPs2)
                    {
                        vramList = ExtractOldPak(vramBytes, vram: true);
                    }
                }
                if (vramList.Count > 0)
                {
                    foreach (var entry in vramList)
                    {
                        pakList.Add(entry);
                    }
                }

                return pakList;
            }
            public List<PakEntry> ExtractOldPak(byte[] pakBytes, bool skipNameFlag = false, bool vram = false)
            {
                ReadWrite reader = new ReadWrite(Endian);
                
                List<PakEntry> PakList = new List<PakEntry>();
                Dictionary<uint, string> headers = DebugReader.MakeDictFromName(PakName);

                bool TryGH3 = false;
                bool TryLzss = false;
                using (MemoryStream stream = new MemoryStream(pakBytes))
                {
                    while (true)
                    {
                        uint header_start = (uint)stream.Position; // To keep track of which entry since the offset in the header needs to be added to the StartOffset below
                        PakEntry? entry = GetPakEntry(stream, reader, headers, header_start, IsWiiOrPs2, skipNameFlag);
                        if (entry == null)
                        {
                            break;
                        }
                        try
                        {
                            if (vramExts.Contains(entry.Extension) && Platform == CONSOLE_PS3 && !vram)
                            {
                                continue;
                            }
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
                            PakList.Clear();
                            if (skipNameFlag == false)
                            {
                                skipNameFlag = true;
                                stream.Position = 0;
                            }
                            else if (TryGH3 == true)
                            {
                                Console.WriteLine(ex.Message);
                                throw new Exception("Could not extract PAK file.");
                            }
                            else
                            {
                                Console.WriteLine("Could not find last entry. Trying Guitar Hero 3 Compression.");
                                pakBytes = Compression.DecompressData(pakBytes);
                                stream.Position = 0;
                                TryGH3 = true;
                            }
                        }
                    }
                }
                return PakList;
            }

        }
        public static List<string> GetFilesFromFolder(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
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
        public static List<PakEntry>? GetNewPakEntries(byte[] pakBytes, string endian, string songName = "")
        {
            ReadWrite reader = new ReadWrite(endian);
            
            List<PakEntry> PakList = new List<PakEntry>();
            Dictionary<uint, string> headers = DebugReader.MakeDictFromName(songName);

            using (MemoryStream stream = new MemoryStream(pakBytes))
            {
                while (true)
                {
                    PakEntry? entry = GetPakEntry(stream, reader, headers, 0, game: GAME_GHWOR);
                    if (entry == null)
                    {
                        break;
                    }
                    if (stream.Position >= pakBytes.Length)
                    {
                        return null;
                    }
                    PakList.Add(entry);
                }
            }
            return PakList;
        }
        public static void GetPakDataNew(List<PakEntry> entries, byte[] pabBytes)
        {
            foreach (var entry in entries)
            {
                entry.EntryData = new byte[entry.FileSize];
                Array.Copy(pabBytes, entry.StartOffset, entry.EntryData, 0, entry.FileSize);
            }
        }
        public static void DecompressNewData(List<PakEntry> entries, byte[] pabBytes)
        {
            foreach (var entry in entries)
            {
                byte[]? newData = null;
                if ((entry.Flags & 0x0200) != 0)
                {
                    newData = new byte[entry.FileSize];
                    Array.Copy(pabBytes, entry.StartOffset, newData, 0, entry.FileSize);
                    entry.EntryData = Compression.DecompressWTPak(newData);
                }
                else
                {
                    entry.EntryData = new byte[entry.FileSize];
                    Array.Copy(pabBytes, entry.StartOffset, entry.EntryData, 0, entry.FileSize);
                }
            }
        }
        private static PakEntry? GetPakEntry(MemoryStream stream, ReadWrite reader, Dictionary<uint, string> headers, uint header_start = 0, bool isWiiorPS2 = false, bool skipFlagName = false, string game = "")
        {
            PakEntry entry = new PakEntry(game);
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
            if (isWiiorPS2)
            {
                (entry.AssetContext, entry.FullName) = (entry.FullName, entry.AssetContext);
            }
            entry.NameNoExt = DebugReader.DebugCheck(headers, name);
            if (entry.FullName.StartsWith("0x"))
            {
                entry.FullName = $"{entry.FullName}.{entry.NameNoExt}";
            }
            if (entry.FullName.IndexOf(entry.Extension, StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                GetCorrectExtension(entry);
            }

            uint parent = reader.ReadUInt32(stream);
            entry.Parent = parent;
            int flags = reader.ReadInt32(stream);
            entry.Flags = flags;
            if ((flags & 0x20) != 0 && !skipFlagName)
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
                entry.FullName = tempString.Replace(DOTPS2, "", StringComparison.InvariantCultureIgnoreCase).Replace(DOTNGC, "", StringComparison.InvariantCultureIgnoreCase);

                stream.Position = skipTo;
            }
            return entry;
        }

        private static void GetCorrectExtension(PakEntry entry)
        {
            if (entry.FullName.IndexOf(DOT_MID_QS) != -1)
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
            else if (entry.Extension.IndexOf(DOT_NQB) != -1)
            {
                entry.SetFullName(entry.FullName.Replace(DOT_QB, DOT_NQB));
            }
            else if (entry.Extension.IndexOf(DOT_STEX) != -1)
            {
                entry.SetFullName(entry.FullName.Replace(DOT_TEX, DOT_STEX));
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
            public bool IsQb { get; set; } = false;
            public bool Split { get; set; } = false;
            public bool ZeroOffset
            {
                get
                {
                    return (IsQb || Split) && ConsoleType != CONSOLE_WII;
                }
            }
            public bool IsNewGame { get; private set; } = false;
            public string? AssetContext { get; set; }
            public List<PakEntry> PakEntries = new List<PakEntry>();
            private ReadWrite Writer { get; set; }
            public PakCompiler(string game, bool isQb = false, bool split = false)
            {
                SetGame(game);
                IsQb = isQb; // Meaning qb.pak, really only used for PS2 to differentiate .qb files from .mqb files
                Split = split;
            }
            public PakCompiler(string game, string console, string? assetContext = null, bool isQb = false, bool split = false)
            {
                SetGame(game);
                IsQb = isQb; // Meaning qb.pak, really only used for PS2 to differentiate .qb files from .mqb files
                Split = split;
                ConsoleType = console;
                SetWriter();
                AssetContext = assetContext;
            }
            private void SetGame(string game)
            {
                if (game.Contains("wor", StringComparison.InvariantCultureIgnoreCase))
                {
                    game = GAME_GHWOR;
                }
                Game = game;
                if (Game == GAME_GHWOR || Game == GAME_GH5)
                {
                    IsNewGame = true;
                }
            }
            private void SetConsole(string filePath)
            {
                string ext = Path.GetExtension(filePath).ToLower();
                if (ext == DOTPS2)
                {
                    ConsoleType = CONSOLE_PS2;
                }
                else if (ext == DOTNGC)
                {
                    ConsoleType = CONSOLE_WII;
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
            private static Dictionary<string, string> ReadStringConfig(string configFile)
            {
                var configDict = new Dictionary<string, string>();
                if (!File.Exists(configFile))
                {
                    return configDict;
                }
                var lines = File.ReadAllLines(configFile);
                // Full Name -> Parent String separated by tab
                foreach (var line in lines)
                {
                    if (line.Contains("\t"))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length == 2)
                        {
                            var fullName = parts[0].Trim().Replace("/", "\\").Replace(DOT_NQB, DOT_QB);
                            var configString = parts[1].Trim().Replace("/", "\\").Replace(DOT_NQB, DOT_QB);
                            if (!configDict.ContainsKey(fullName))
                            {
                                configDict.Add(fullName, configString);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Invalid line in parents.config: {line}");
                        }
                    }
                }
                return configDict;
            }
            private static Dictionary<string, int> ReadIntConfig(string configFile)
            {
                var configDict = new Dictionary<string, int>();
                if (!File.Exists(configFile))
                {
                    return configDict;
                }
                var lines = File.ReadAllLines(configFile);
                foreach (var line in lines)
                {
                    if (line.Contains("\t"))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int flagValue))
                        {
                            configDict.Add(parts[0].Trim().Replace("/", "\\").Replace(DOT_NQB, DOT_QB), flagValue);
                        }
                    }
                }
                return configDict;
            }
            private void AddParentsToPakEntries(Dictionary<string, string> parents)
            {
                if (parents.Count == 0) return;

                var parentStrings = new List<string>(parents.Values).ToList();
                var parentEntries = new List<PakEntry>();
                var childEntries = new Dictionary<string, List<PakEntry>>(); // key is the parent
                var otherEntries = new List<PakEntry>();
                var addedToNewList = new List<string>();

                foreach (var entry in PakEntries)
                {
                    var isOther = true;
                    if (parentStrings.Contains(entry.FullName))
                    {
                        isOther = false;
                        parentEntries.Add(entry);
                    }
                    if (parents.ContainsKey(entry.FullName))
                    {
                        isOther = false;
                        if (!childEntries.ContainsKey(parents[entry.FullName]))
                        {
                            childEntries[parents[entry.FullName]] = new List<PakEntry>()
                            {
                                entry
                            };
                        }
                        else
                        {
                            childEntries[parents[entry.FullName]].Add(entry);
                        }
                    }
                    if (isOther)
                    {
                        otherEntries.Add(entry);
                    }
                }

                var parentNoChild = new List<PakEntry>();
                var parentAndChild = new List<PakEntry>();
                var childOnly = new List<PakEntry>();

                // PakEntries.Clear();
                foreach (var entry in parentEntries)
                {
                    foreach (var childEntry in childEntries[entry.FullName])
                    {
                        var qbkey = QBKey(entry.FullName);
                        childEntry.Parent = QBKeyUInt(entry.FullName);
                    }
                }

                foreach (var entry in parentEntries)
                { 
                    if (entry.Parent == 0)
                    {
                        parentNoChild.Add(entry);
                    }
                    else
                    {
                        parentAndChild.Add(entry);
                    }
                }
                foreach (var entry in PakEntries)
                {
                    if (!parentStrings.Contains(entry.FullName))
                    {
                        if (entry.Parent != 0)
                        {
                            childOnly.Add(entry);
                        }
                    }
                }

                PakEntries.Clear();
                PakEntries.AddRange(parentNoChild);
                PakEntries.AddRange(parentAndChild);
                PakEntries.AddRange(childOnly);
                PakEntries.AddRange(otherEntries);

            }
            private void AddAssetContextsToPakEntries(Dictionary<string, string> assets)
            {
                bool allAssets = true;
                foreach (var entry in PakEntries)
                {
                    if (assets.ContainsKey(entry.FullName))
                    {
                        entry.SetAssetContext(assets[entry.FullName]);
                    }
                    else
                    {
                        allAssets = false;
                    }
                }
                if (allAssets)
                {
                    AssetContext = PakEntries[0].AssetContext;
                }
            }
            private void AddFlagsToPakEntries(Dictionary<string, int> flags)
            {
                foreach (var entry in PakEntries)
                {
                    if (flags.ContainsKey(entry.FullName))
                    {
                        entry.OverwriteFlags(flags[entry.FullName]);
                    }
                }
            }
            private void ReorderFiles(List<string> fileOrder)
            {
                var newOrder = new List<PakEntry>();
                foreach (var file in fileOrder)
                {
                    var entry = PakEntries.FirstOrDefault(e => e.GetFullName() == file);
                    if (entry != null)
                    {
                        newOrder.Add(entry);
                    }
                }
                if (newOrder.Count < PakEntries.Count)
                {
                    // If some entries were not found in the file order, add them at the end
                    var remainingEntries = PakEntries.Except(newOrder).ToList();
                    newOrder.AddRange(remainingEntries);
                }
                PakEntries = newOrder;
            }
            public (byte[]? itemData, byte[]? otherData, Dictionary<uint, string>? qsStrings) CompilePAK(string folderPath, string console = "")
            {
                if (!Directory.Exists(folderPath))
                {
                    throw new NotSupportedException("Argument given is not a folder.");
                }
                string[] entriesRaw = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                List<string> rootFiles = new List<string>();
                List<string> otherFiles = new List<string>();
                string pakConfigFiles = "pak_config";
                string parentsFile = Path.Combine(folderPath, pakConfigFiles, "parents.config");
                string assetsFile = Path.Combine(folderPath, pakConfigFiles, "asset_contexts.config");
                string flagsFile = Path.Combine(folderPath, pakConfigFiles, "flags.config");
                string fileOrderFile = Path.Combine(folderPath, pakConfigFiles, "file_order.config");
                Dictionary<string, string> parents = new Dictionary<string, string>();
                Dictionary<string, string> assetConfigs = new Dictionary<string, string>();
                Dictionary<string, int> flags = new Dictionary<string, int>();
                var fileOrder = new List<string>();
                if (File.Exists(parentsFile))
                {
                    parents = ReadStringConfig(parentsFile);
                }
                if (File.Exists(assetsFile))
                {
                    assetConfigs = ReadStringConfig(assetsFile);
                }
                if (File.Exists(flagsFile))
                {
                    flags = ReadIntConfig(flagsFile);
                }
                if (File.Exists(fileOrderFile))
                {
                    fileOrder = File.ReadAllLines(fileOrderFile).ToList();
                }
                foreach (string entry in entriesRaw)
                {
                    if (entry == parentsFile) continue;
                    if (entry == assetsFile) continue;
                    if (entry == flagsFile) continue;
                    if (entry == fileOrderFile) continue;

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
                bool isPs2orWii = (ConsoleType == CONSOLE_PS2 || ConsoleType == CONSOLE_WII);
                
                List<string> fileNames = new List<string>();
                var qsMaster = new Dictionary<uint, string>();
                bool localQsFiles = false;

                var (qsEn, qsFr, qsDe, qsIt, qsEs) = CheckForQsFilesInFolder(entries, out localQsFiles);

                foreach (string entry in entries)
                {
                    if (File.Exists(entry))
                    {
                        CreatePakEntry(entry, folderPath, PakEntries, fileNames, qsMaster, isPs2orWii, qsEn);
                    }
                }

                AddParentsToPakEntries(parents);
                AddAssetContextsToPakEntries(assetConfigs);
                AddFlagsToPakEntries(flags);
                ReorderFiles(fileOrder);

                if (Game != GAME_GH3 && Game != GAME_GHA && console != CONSOLE_WII && !Split && qsMaster.Count > 0)
                {
                    string tempName = "MoreStrings";
                    if (Game == GAME_GH5 || Game == GAME_GHWOR)
                    {
                        string tempQsOrig = Path.Combine(folderPath, $"{tempName}.qs");
                        foreach (var locale in new[] { DOTEN, DOTFR, DOTDE, DOTIT, DOTES })
                        {
                            var tempQs = tempQsOrig + locale + ".xen";
                            QsDict dictMod;
                            switch (Path.GetExtension(locale))
                            {
                                case DOTFR:
                                    dictMod = QsDict.Fr;
                                    break;
                                case DOTDE:
                                    dictMod = QsDict.De;
                                    break;
                                case DOTIT:
                                    dictMod = QsDict.It;
                                    break;
                                case DOTES:
                                    dictMod = QsDict.Es;
                                    break;
                                case DOTEN:
                                default:
                                    dictMod = QsDict.En;
                                    break;
                            }
                            try
                            {
                                ReadWrite.TranslateAndWriteQsFile(tempQs, qsMaster, dictMod);
                                CreatePakEntry(tempQs, folderPath, PakEntries, fileNames, qsMaster, isPs2orWii);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error writing QS file: {ex.Message}");
                                throw new Exception("Could not write QS file. Please check the folder path and try again.");
                            }
                            finally
                            {
                                File.Delete(tempQs);
                            }
                        }
                    }
                    else
                    {
                        string tempQs = Path.Combine(folderPath, $"{tempName}.qs.xen");
                        try
                        {
                            ReadWrite.WriteQsFileFromDict(tempQs, qsMaster);
                            CreatePakEntry(tempQs, folderPath, PakEntries, fileNames, qsMaster, isPs2orWii);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error writing QS file: {ex.Message}");
                            throw new Exception("Could not write QS file. Please check the folder path and try again.");
                        }
                        finally
                        {
                            File.Delete(tempQs);
                        }
                    }
                    
                    qsMaster = null; // QS files are included in pak so don't need to be returned.
                }

                var (pakData, pabData) = CompilePakEntries(PakEntries);

                return (pakData, pabData, qsMaster);

            }

            
            private void CreatePakEntry(string entry, string folderPath, List<PakEntry> PakEntries, List<string> fileNames, Dictionary<uint, string> qsMaster, bool isPs2orWii, Dictionary<uint, string>? includedQs = null)
            {
                if (includedQs == null)
                {
                    includedQs = new Dictionary<uint, string>();
                }
                bool isQFile = Path.GetExtension(entry) == DOT_Q || Path.GetExtension(entry) == DOT_NQ;
                byte[] fileData;
                string relPath = GetRelPath(folderPath, entry);
                string relPathForExtension = relPath;
                Console.WriteLine($"Processing {relPath}");
                string qbName;
                if (isQFile)
                {
                    if (Path.GetExtension(entry) == DOT_NQ)
                    {
                        relPath = relPath.Replace(DOT_NQ, DOT_Q);
                    }
                    List<QBItem> qBItems;
                    Dictionary<uint, string> qsItems;
                    try
                    {
                        (qBItems, qsItems) = ParseQFile(entry, ConsoleType, Game);
                        if (qsItems != null)
                        {
                            foreach (var item in qsItems)
                            {
                                if (!includedQs.ContainsKey(item.Key) && !qsMaster.ContainsKey(item.Key))
                                {
                                    qsMaster.Add(item.Key, item.Value);
                                }
                            }
                        }
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
                    relPathForExtension += "b";
                    //qbName = (isPs2orWii && Game == GAME_GH3) ? DebugReader.Ps2PakString(relPath) : relPath;
                    qbName = (isPs2orWii && Game == GAME_GH3) ? $"c:/gh3c/data/{relPath}" : relPath;
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
                pakEntry.SetGame(Game);
                pakEntry.SetFullFlagPath(relPath);
                pakEntry.SetNameNoExt(GetFileNoExt(Path.GetFileName(relPath)));
                pakEntry.SetExtension(GetFileExt(relPathForExtension));
                pakEntry.SetFlags();
                pakEntry.SetNames(IsQb);

                if (!fileNames.Contains(pakEntry.NameNoExt))
                {
                    fileNames.Add(pakEntry.NameNoExt);
                }
                PakEntries.Add(pakEntry);
            }
            public (byte[], byte[]?) CompilePakEntries(List<PakEntry> PakEntries)
            {
                int padToFull = 256;
                int padToEntry = 32;
                PakEntries.Add(new PakEntry(ConsoleType, Game, AssetContext)); // Last entry
                bool splitWor = IsNewGame && ZeroOffset;
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
                    else if (splitWor)
                    {
                        pakSize = 0;
                    }
                    else if (pakSize % padToFull != 0)
                    {
                        pakSize += (padToFull - (pakSize % padToFull));
                    }
                    int bytesPassed = 0;
                    foreach (PakEntry entry in PakEntries)
                    {
                        entry.StartOffset = (uint)(pakSize - bytesPassed + pab.Position);
                        pak.Write(Writer.ValueHex(entry.Extension, false), 0, 4);
                        pak.Write(Writer.ValueHex(entry.StartOffset), 0, 4);
                        pak.Write(Writer.ValueHex(entry.FileSize), 0, 4);
                        if (ConsoleType == CONSOLE_WII || ConsoleType == CONSOLE_PS2)
                        {
                            pak.Write(Writer.ValueHex(entry.FullName, false), 0, 4);
                            pak.Write(Writer.ValueHex(entry.AssetContext, false), 0, 4);
                        }
                        else
                        {
                            pak.Write(Writer.ValueHex(entry.AssetContext, false), 0, 4);
                            pak.Write(Writer.ValueHex(entry.FullName, false), 0, 4);
                        }
                        pak.Write(Writer.ValueHex(entry.NameNoExt, false), 0, 4);
                        pak.Write(Writer.ValueHex(entry.Parent), 0, 4);
                        pak.Write(Writer.ValueHex(entry.Flags), 0, 4);
                        if ((entry.Flags & 0x20) != 0)
                        {
                            ReadWrite.WriteStringBytes(pak, entry.FullFlagPath);
                        }
                        pab.Write(entry.EntryData);
                        Writer.PadStreamTo(pab, padToEntry);
                        if (!splitWor)
                        {
                            bytesPassed += entry.ByteLength;
                        }
                    }
                    if (ConsoleType == CONSOLE_PS2)
                    {
                        ReadWrite.WriteStringBytes(pak, "\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
                    }
                    else
                    {
                        Writer.PadStreamTo(pak, padToFull);
                    }
                    pabData = pab.ToArray();
                    if (!Split || ConsoleType == CONSOLE_WII)
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
        public static (string pakSavePath, bool doubleKick, string? pakSavePathExpertPlus) CreateSongPackage(string midiPath,
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
            Dictionary<string, int>? diffs = null,
            string gender = "Male",
            bool gh3Plus = false
            )
        {
            var midiHopoType = (MidiDefs.HopoType)hopoType;
            if (gender.ToLower() == "none")
            {
                gender = "none";
            }
            else if (gender.ToLower() != "female")
            {
                gender = "Male";
            }

            // Process MIDI file
            var midiExt = Path.GetExtension(midiPath);
            SongQbFile midiFile;
            byte[] midQb;
            byte[]? midQb_xplus = null;
            if (midiExt == ".mid" || midiExt == ".chart")
            {
                bool fromChart = false;
                if (midiExt == ".chart")
                {
                    var chart = new Chart(midiPath);
                    chart.ConvertChartToMid();
                    midiPath = chart.GetMidiPath();
                    chart.WriteMidToFile();
                    hopoThreshold = chart.GetHopoResolution();
                    midiHopoType = MidiDefs.HopoType.GH3;
                    fromChart = true;
                }
                midiFile = new SongQbFile(
                midiPath,
                songName: songName,
                game: game,
                console: gameConsole,
                hopoThreshold: hopoThreshold,
                perfOverride: perfOverride,
                songScriptOverride: songScripts,
                venueSource: venueSource,
                rhythmTrack: rhythmTrack,
                overrideBeat: overrideBeat,
                hopoType: midiHopoType,
                easyOpens: easyOpens,
                skaPath: skaPath,
                fromChart: fromChart,
                gh3Plus: gh3Plus);

                midQb = midiFile.ParseMidiToQb();
                if (midiFile.Drums.HasExpertPlus && game == GAME_GHWT)
                {
                    midQb_xplus = midiFile.ParseMidiToQb(true);
                }
            }
            else if (midiExt == ".q")
            {
                midiFile = new SongQbFile(songName, midiPath, songScripts, game: game, console: gameConsole);
                midQb = midiFile.MakeConsoleQb();
            }
            else
            {
                throw new Exception("Invalid file type. Must be .mid or .q");
            }

            // Process errors and warnings
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


            // Create first PAK
            string mainPakPath = CreatePakForMidiData(
                savePath,
                songName,
                game,
                gameConsole,
                midiFile,
                midQb,
                skaPath,
                skaSource,
                gender,
                isSteven,
                diffs);

            if (diffs != null)
            {
                midiFile.SetEmptyTracksToDiffZero(diffs);
            }

            // Create second PAK for Expert+ if it exists
            string? xplusPakPath = null;
            if (midQb_xplus != null)
            {
                string xplusSongName = songName + "_expertplus";
                xplusPakPath = CreatePakForMidiData(
                    savePath,
                    xplusSongName,
                    game,
                    gameConsole,
                    midiFile,
                    midQb_xplus,
                    skaPath,
                    skaSource,
                    gender,
                    isSteven,
                    diffs);
            }

            bool doubleKick = midiFile.DoubleKick;
            return (mainPakPath, doubleKick, xplusPakPath);
        }

        private static string CreatePakForMidiData(
        string savePath,
        string songName,
        string game,
        string gameConsole,
        SongQbFile midiFile,
        byte[] midQb,
        string skaPath,
        string skaSource,
        string gender,
        bool isSteven,
        Dictionary<string, int>? diffs)
        {
            var saveName = Path.Combine(savePath, $"{songName}_{gameConsole}");
            string pakFolder = gameConsole == CONSOLE_PS2 ? "data\\songs" : "songs";
            var songFolder = Path.Combine(saveName, pakFolder);
            string consoleExt = gameConsole == CONSOLE_PS2 ? DOTPS2 : DOTXEN;
            var qbSave = Path.Combine(songFolder, songName + $".mid.qb{consoleExt}");

            byte[]? songScriptsQb;

            Directory.CreateDirectory(songFolder);

            // Save MIDI QB file
            File.WriteAllBytes(qbSave, midQb);

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

            }

            bool ps2SkaProcessed = false;
            byte[]? skaScripts = null;
            if (Directory.Exists(skaPath))
            {
                float skaMultiplier;
                if (gameConsole == CONSOLE_PS2)
                {
                    switch (skaSource)
                    {
                        case GAME_GH3:
                            switch (game)
                            {
                                case GAME_GH3:
                                    skaMultiplier = 2.0f;
                                    break;
                                case GAME_GHA:
                                    skaMultiplier = 1.0f;
                                    break;
                                default:
                                    skaMultiplier = 1.0f;
                                    break;
                            }
                            break;
                        case GAME_GHA:
                            skaMultiplier = 1.0f;
                            break;
                        default:
                            skaMultiplier = 2.0f;
                            break;
                    }
                }
                else
                {
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
                }

                var skaFiles = Directory.GetFiles(skaPath).ToList();
                var skaFilesToCompress = Path.Combine(skaPath, "Compress");
                if (Directory.Exists(skaFilesToCompress))
                {
                    skaFiles.AddRange(Directory.GetFiles(skaFilesToCompress));
                }
                var readSkaFiles = new Dictionary<string, SkaFile>();
                string skaEndian = gameConsole == CONSOLE_XBOX ? "big" : "little";
                foreach (var skaFile in skaFiles)
                {
                    SkaFile skaTest;
                    try
                    {
                        skaTest = new SkaFile(skaFile, "big");
                        readSkaFiles.Add(skaFile, skaTest);
                    }
                    catch (Exception ex)
                    {
                        var fileSka = Path.GetFileNameWithoutExtension(skaFile);
                        Console.WriteLine($"{fileSka}: {ex.Message}\n");
                        continue;
                    }
                }

                foreach (var skaParsed in readSkaFiles)
                {
                    string skaPatternGuit = @"\d+b\.ska(\.xen)?$";
                    string skaPatternSing = @"\d\.ska(\.xen)?$";

                    var skaFileName = Path.GetFileName(skaParsed.Key);
                    if (skaFileName.ToLower().StartsWith("0x"))
                    {
                        skaFileName = skaFileName.Substring(0, skaFileName.IndexOf('.'));
                    }

                    bool isGuitarist = Regex.IsMatch(skaParsed.Key, skaPatternGuit) || midiFile.GtrSkaAnims.Contains(skaFileName);

                    string skaType;
                    switch (game)
                    {
                        case GAME_GH3:
                            if (isGuitarist && gameConsole != CONSOLE_PS2)
                            {
                                skaType = SKELETON_GH3_GUITARIST;
                            }
                            else
                            {
                                skaType = gameConsole == CONSOLE_PS2 ? SKELETON_GH3_SINGER_PS2 : SKELETON_GH3_SINGER;
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

                    try
                    {
                        if (game == GAME_GH3 || game == GAME_GHA)
                        {
                            if (gameConsole == CONSOLE_PS2)
                            {
                                if (!ps2SkaProcessed)
                                {
                                    if (gender.ToLower() != "none")
                                    {
                                        skaScripts = SongQbFile.MakePs2SkaScript(gender, songName);
                                    }
                                    ps2SkaProcessed = true;
                                }

                                if (midiFile.GtrSkaAnims.Contains(skaFileName))
                                {
                                    continue;
                                }
                                convertedSka = skaParsed.Value.WritePs2StyleSka(skaMultiplier);
                                string skaFolderPs2 = Path.Combine(savePath, "PS2 SKA Files");
                                skaSave = Path.Combine(skaFolderPs2, Path.GetFileName(skaParsed.Key).Replace(DOTXEN, DOTPS2));
                                Directory.CreateDirectory(skaFolderPs2);
                            }
                            else
                            {
                                convertedSka = skaParsed.Value.WriteGh3StyleSka(skaType, skaMultiplier);
                                skaSave = Path.Combine(saveName, Path.GetFileName(skaParsed.Key));
                            }
                        }
                        else
                        {
                            if (skaParsed.Value.IsSingleFrame)
                            {
                                if (game != GAME_GHWT)
                                {
                                    Console.WriteLine($"{Path.GetFileNameWithoutExtension(skaParsed.Key)}: Single frame SKA files not supported for {game}.\n");
                                    continue;
                                }
                                convertedSka = File.ReadAllBytes(skaParsed.Key);
                            }
                            else
                            {
                                convertedSka = skaParsed.Value.WriteModernStyleSka(skaType, game, skaMultiplier);
                            }

                            skaSave = Path.Combine(saveName, Path.GetFileName(skaParsed.Key));
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"{Path.GetFileNameWithoutExtension(skaParsed.Key)}: Could not convert ska file.\n");
                        continue;
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
                bool addQuotes = false;
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
                    addQuotes = true;
                }

                var sortedKeys = qsList.OrderBy(entry => entry.Value)
                                           .Select(entry => entry.Key)
                                           .ToList();

                foreach (string qsSave in qsSaves)
                {
                    using (StreamWriter writer = new StreamWriter(qsSave, false, Encoding.Unicode))
                    {
                        writer.NewLine = "\n";

                        foreach (var key in sortedKeys)
                        {
                            string modifiedKey = key.Substring(2).PadLeft(8, '0');
                            string line = addQuotes ? $"{modifiedKey} \"{qsList[key]}\"" : $"{modifiedKey} {qsList[key]}";
                            writer.WriteLine(line);
                        }

                        writer.WriteLine();
                        writer.WriteLine();
                    }
                }
            }

            string? assetContext = game == GAME_GHWOR ? songName : null;
            var pakCompiler = new PAK.PakCompiler(game: game, console: gameConsole, assetContext: assetContext);
            var (pakData, pabData, qsStrings) = pakCompiler.CompilePAK(saveName);
            string songPrefix = gameConsole == CONSOLE_PS2 ? "" : "_song";
            var pakSave = Path.Combine(savePath, songName + $"{songPrefix}.pak{consoleExt}");
            File.WriteAllBytes(pakSave, pakData);

            // Clean up the compile folder
            Directory.Delete(saveName, true);

            return pakSave;
        }

        public static byte[] lzssDecompTest(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            var lzss = new Lzss();
            byte[] decomp = lzss.Decompress(fileBytes);
            return decomp;
        }
    }
}
