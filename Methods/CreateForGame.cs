using static GH_Toolkit_Core.PAK.PAK;
using static GH_Toolkit_Core.QB.QB;
using GH_Toolkit_Core.QB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBStruct;
using GH_Toolkit_Core.PS360;
using System.Diagnostics;
using GH_Toolkit_Core.Checksum;
using System.Text.RegularExpressions;

namespace GH_Toolkit_Core.Methods
{
    public class CreateForGame
    {
        public class GhMetadata
        {
            public string Checksum { get; set; } = "";
            public string CompileFolder { get; set; } = "";
            public string Title { get; set; } = "";
            public string Artist { get; set; } = "";
            public string ArtistTextSelect { get; set; } = "";
            public string ArtistTextCustom { get; set; } = "";
            public int Year { get; set; }
            public string CoverArtist { get; set; } = "";
            public int CoverYear { get; set; }
            public string Genre { get; set; } = "";
            public string ChartAuthor { get; set; } = "";
            public string Bassist { get; set; } = "";
            public string Singer { get; set; } = "";
            public bool IsArtistFamousBy { get; set; }
            public string AerosmithBand { get; set; } = "";
            public int Beat8thLow { get; set; }
            public int Beat8thHigh { get; set; }
            public int Beat16thLow { get; set; }
            public int Beat16thHigh { get; set; }
            public bool OverrideBeatLines { get; set; }
            public bool CoopAudioCheck { get; set; }
            public bool P2RhythmCheck { get; set; }
            public float BandVol { get; set; }
            public float GtrVol { get; set; }
            public string Countoff { get; set; } = "hihat01";
            public float HopoThreshold { get; set; } // Specifically Neversoft Hopo Threshold, not HMX
            public QBStructData GenerateGh3SongListEntry(string game, string platform)
            {
                string STEVEN = "Steven Tyler";

                var entry = new QBStructData();
                string pString = platform == CONSOLE_PS2 ? STRING : WIDESTRING; // Depends on the platform
                bool artistIsOther = ArtistTextSelect == "Other";
                string artistText = !artistIsOther ? $"artist_text_{ArtistTextSelect.ToLower().Replace(" ", "_")}" : ArtistTextCustom;
                string artistType = artistIsOther ? pString : POINTER;
                string gender = (Singer == STEVEN) ? "male" : Singer;

                entry.AddVarToStruct("checksum", Checksum, QBKEY);
                entry.AddVarToStruct("name", Checksum, STRING);
                entry.AddVarToStruct("title", Title, pString);
                entry.AddVarToStruct("artist", Artist, pString);
                entry.AddVarToStruct("year", $", {Year}", pString);
                entry.AddVarToStruct("artist_text", artistText, artistType);
                entry.AddIntToStruct("original_artist", IsArtistFamousBy ? 0 : 1);
                entry.AddVarToStruct("version", "gh3", QBKEY);
                entry.AddIntToStruct("leaderboard", 1);
                entry.AddIntToStruct("gem_offset", 0);
                entry.AddIntToStruct("input_offset", 0);
                entry.AddVarToStruct("singer", gender, QBKEY);
                if (game == GAME_GHA)
                {
                    string band = Singer == STEVEN ? AerosmithBand : "default_band";
                    entry.AddVarToStruct("band", band, QBKEY);
                    if (Singer == STEVEN)
                    {
                        entry.AddVarToStruct("guitarist_checksum", "aerosmith", QBKEY);
                    }
                    if (OverrideBeatLines)
                    {
                        if (Beat8thLow != 1)
                        {
                            entry.AddIntToStruct("thin_fretbar_8note_params_low_bpm", Beat8thLow);
                        }
                        if (Beat8thHigh != 180)
                        {
                            entry.AddIntToStruct("thin_fretbar_8note_params_high_bpm", Beat8thHigh);
                        }
                        if (Beat16thLow != 1)
                        {
                            entry.AddIntToStruct("thin_fretbar_16note_params_low_bpm", Beat16thLow);
                        }
                        if (Beat16thHigh != 120)
                        {
                            entry.AddIntToStruct("thin_fretbar_16note_params_high_bpm", Beat16thHigh);
                        }
                    }
                }
                entry.AddVarToStruct("keyboard", "false", QBKEY);
                entry.AddFloatToStruct("band_playback_volume", BandVol);
                entry.AddFloatToStruct("guitar_playback_volume", GtrVol);
                entry.AddVarToStruct("countoff", Countoff, STRING);
                entry.AddIntToStruct("rhythm_track", P2RhythmCheck ? 1 : 0);
                if (CoopAudioCheck)
                {
                    entry.AddFlagToStruct("use_coop_notetracks", QBKEY);
                }
                entry.AddFloatToStruct("hammer_on_measure_scale", HopoThreshold);
                if (Bassist != "Default")
                {
                    entry.AddVarToStruct("bassist", Bassist, QBKEY);
                }
                return entry;
            }
            public void CreateConsolePackage(string game, string platform, string compilePath, string resources, string onyxPath)
            {
                string toCopyTo;
                string[] onyxArgs;
                string fileType;
                bool hasAudio = false;
                bool hasDat = game == GAME_GH3 ? false : true;
                if (platform == CONSOLE_PS3)
                {
                    fileType = "PKG";
                    Console.WriteLine("Compiling PKG file using Onyx CLI");
                    toCopyTo = Path.Combine(compilePath, "PS3");
                    string gameFiles = Path.Combine(toCopyTo, "USRDIR", Checksum.ToUpper());
                    Directory.CreateDirectory(gameFiles);
                    string ps3Resources = Path.Combine(resources, "PS3");
                    string currGameResources = Path.Combine(ps3Resources, game);
                    string vramFile = Path.Combine(ps3Resources, $"VRAM_{game}");
                    if (!Directory.Exists(ps3Resources) || !Directory.Exists(currGameResources))
                    {
                        throw new Exception("Cannot find PS3 Resource folder.\n\nThis should be included with your toolkit.\nPlease re-download the toolkit.");
                    }
                    string[] filesToCopy = Directory.GetFiles(compilePath);
                    foreach (string file in filesToCopy)
                    {
                        // Check if each file has an extension, if not skip it
                        if (!file.Contains("."))
                        {
                            continue;
                        }
                        if (file.ToLower().EndsWith(".fsb"))
                        {
                            hasAudio = true;
                        }
                        if (file.ToLower().EndsWith(".dat"))
                        {
                            hasDat = true;
                        }
                        File.Copy(file, Path.Combine(gameFiles, Path.GetFileName(file).ToUpper() + ".PS3"), true);
                        string fileExtension = Path.GetExtension(file);
                        string fileNoExt = Path.GetFileNameWithoutExtension(file).ToLower();
                        bool localeFile = fileNoExt.Contains("_text") && !fileNoExt.EndsWith("_text");
                        if (fileExtension.ToLower() == ".pak" && !localeFile)
                        {
                            File.Copy(vramFile, Path.Combine(gameFiles, $"{fileNoExt}_VRAM.PAK.PS3").ToUpper(), true);
                        }
                    }
                    foreach (string file in Directory.GetFiles(currGameResources))
                    {
                        File.Copy(file, Path.Combine(toCopyTo, Path.GetFileName(file)), true);
                    }
                    string pkgSave = Path.Combine(CompileFolder, $"{Checksum}.pkg".ToUpper());
                    string contentID = FileCreation.GetPs3Key(game) + $"-{Checksum.ToUpper().Replace("_", "").PadLeft(16, '0')}";
                    onyxArgs = ["pkg", contentID, toCopyTo, "--to", pkgSave];
                }
                else
                {
                    fileType = "STFS";
                    Console.WriteLine("Compiling STFS file using Onyx CLI");
                    string packageName = $"{Title} by {Artist}";
                    CreateOnyxStfsFolder(game, resources, compilePath, packageName);
                    toCopyTo = Path.Combine(compilePath, "360");
                    string[] filesToCopy = Directory.GetFiles(compilePath);
                    foreach (string file in filesToCopy)
                    {
                        // Check if each file has an extension, if not skip it
                        if (!file.Contains("."))
                        {
                            continue;
                        }
                        if (file.ToLower().EndsWith(".fsb"))
                        {
                            hasAudio = true;
                        }
                        if (file.ToLower().EndsWith(".dat"))
                        {
                            hasDat = true;
                        }
                        File.Copy(file, Path.Combine(toCopyTo, Path.GetFileName(file) + ".xen"), true);
                    }
                    string stfsSave = Path.Combine(CompileFolder, Checksum.ToUpper());
                    onyxArgs = ["stfs", toCopyTo, "--to", stfsSave];

                }

                if (!hasAudio || !hasDat)
                {
                    throw new Exception($"Missing audio or dat file for {fileType} creation. Please compile all files first!");
                }
                CompileWithOnyx(onyxPath, onyxArgs);
            }
        }
        public static (PakEntry, Dictionary<string, QBItem>, QBArrayNode, QBStructData) GetSongListPak(Dictionary<string, PakEntry> qbPak)
        {
            var songList = qbPak[songlistRef];
            var songListEntries = QbEntryDictFromBytes(songList.EntryData, "big");
            var dlSongList = songListEntries[gh3Songlist].Data as QBArrayNode;
            var dlSongListProps = songListEntries[permanentProps].Data as QBStructData;
            return (songList, songListEntries, dlSongList, dlSongListProps);
        }

        public static (PakEntry, Dictionary<string, QBItem>, QBStructData) GetDownloadPak(Dictionary<string, PakEntry> qbPak)
        {
            var downloadQb = qbPak[downloadRef];
            var downloadQbEntries = QbEntryDictFromBytes(downloadQb.EntryData, "big");
            var downloadlist = downloadQbEntries[gh3DownloadSongs].Data as QBStructData;
            return (downloadQb, downloadQbEntries, downloadlist);
        }
        public static (byte[] pakData, byte[] pabData) AddToDownloadList(string qbPakLocation, string platform, List<QBStructData> songListEntry)
        {
            var pakCompiler = new PakCompiler(GAME_GH3, platform, split: true);
            var qbPak = PakEntryDictFromFile(qbPakLocation);
            var (songList, songListEntries, dlSongList, dlSongListProps) = GetSongListPak(qbPak);

            var (downloadQb, downloadQbEntries, downloadlist) = GetDownloadPak(qbPak);
            var tier1 = downloadlist["tier1"] as QBStructData;
            var songArray = tier1["songs"] as QBArrayNode;

            foreach (var song in songListEntry)
            {
                string checksum = (string)song["checksum"];
                var songIndex = dlSongList.GetItemIndex(checksum, QBKEY);
                if (songIndex == -1)
                {
                    dlSongList.AddQbkeyToArray(checksum);
                    dlSongListProps.AddStructToStruct(checksum, song);
                }
                else
                {
                    dlSongListProps[checksum] = song;
                }

                if (songArray.GetItemIndex(checksum, QBKEY) == -1)
                {
                    songArray.AddQbkeyToArray(checksum);
                    tier1["defaultunlocked"] = songArray.Items.Count;
                }
            }

            byte[] songlistBytes = CompileQbFromDict(songListEntries, songlistRef,  GAME_GH3, platform);
            songList.OverwriteData(songlistBytes);
           
            byte[] downloadQbBytes = CompileQbFromDict(downloadQbEntries, downloadRef, GAME_GH3, platform);
            downloadQb.OverwriteData(downloadQbBytes);

            var (pakData, pabData) = pakCompiler.CompilePakFromDictionary(qbPak);
            return (pakData, pabData);
        }
        public static void OverwriteSplitPak(string pakLocation, byte[] pakData, byte[] pabData, string dotX = DOTXEN)
        {
            if (!File.Exists(pakLocation))
            {
                throw new FileNotFoundException("The QB.PAK file was not found at the specified location.");
            }
            string pabLocation = pakLocation.Replace(DOT_PAK + dotX, DOT_PAB + dotX);
            File.WriteAllBytes(pakLocation, pakData);
            File.WriteAllBytes(pabLocation, pabData);
        }
        public static void CreateConsoleDownloadFiles(uint checksum, string game, string platform, string compilePath, string resources, List<QBStruct.QBStructData> songListEntry)
        {
            if(!Directory.Exists(compilePath))
            {
                Directory.CreateDirectory(compilePath);
            }
            var gh3Resource = Path.Combine(resources, "GH3");

            // Text files
            var textChecksum = $"download\\download_song{checksum}.qb";
            var pakCompiler = new PakCompiler(game, platform, split: false);
            if (!Directory.Exists(gh3Resource))
            {
                throw new Exception("Cannot find GH3 Resource folder.\n\nThis should be included with your toolkit.\nPlease re-download the toolkit.");
            }
            string[] locales = ["", "_f", "_g", "_i", "_s"];
            foreach (string locale in locales)
            {
                string songlistPath = Path.Combine(gh3Resource, $"blank_songlist{locale}.q");
                var songlist = ParseQFromFile(songlistPath);
                FileCreation.AddToSonglistGh3(songlist, songListEntry);
                string localeDirectory = Path.Combine(compilePath, $"dl{checksum}_text{locale}");
                string qbFile = Path.Combine(localeDirectory, textChecksum);
                string qbDirectory = Path.GetDirectoryName(qbFile);
                Directory.CreateDirectory(qbDirectory);
                var qbBytes = CompileQbFile(songlist, textChecksum, game, platform);
                File.WriteAllBytes(qbFile, qbBytes);
                var (textData, _) = pakCompiler.CompilePAK(localeDirectory, platform);
                var textSave = $"{localeDirectory}.pak";
                File.WriteAllBytes(textSave, textData);
                Directory.Delete(localeDirectory, true);
            }

            // Non-text files (scripts)
            var scriptChecksum = $"download\\dl{checksum}.q";
            string scriptPath = Path.Combine(gh3Resource, "script_override.q");
            string pakPath = Path.Combine(compilePath, $"dl{checksum}");
            string savePath = Path.Combine(pakPath, scriptChecksum);
            string saveDirectory = Path.GetDirectoryName(savePath);
            Directory.CreateDirectory(saveDirectory);
            File.Copy(scriptPath, savePath, true);
            var (scriptData, _) = pakCompiler.CompilePAK(pakPath, platform);
            var scriptSave = $"{pakPath}.pak";
            File.WriteAllBytes(scriptSave, scriptData);
            Directory.Delete(pakPath, true);

            if (platform == "360")
            {
                CreateOnyxStfsFolder("GH3", resources, compilePath, $"dl{checksum}", true);
            }
        }

        public static uint MakeConsoleChecksum(string[] toCombine)
        {
            int minNum = 1000000000;
            string qbString = string.Concat(toCombine);
            var qbKey = CRC.QBKeyUInt(qbString);
            return (uint)(minNum + (qbKey % minNum));
        }

        public static void CreateOnyxStfsFolder(string game, string resource, string compilePath, string packageName, bool altPath = false)
        {
            string yaml = YAML.CreateOnyxYaml(game, packageName);
            if (yaml == "Fail")
            {
                throw new Exception("Could not find YAML template.\n\nFailed to create Onyx YAML file.");
            }
            string onyxResource = Path.Combine(resource, "Onyx");
            string thumbnailResource = Path.Combine(onyxResource, $"{game}-thumbnail.png");
            string onyxRepack;
            if (altPath)
            {
                onyxRepack = Path.Combine(compilePath, "onyx-repack");
            }
            else
            {
                onyxRepack = Path.Combine(compilePath, "360", "onyx-repack");
            }
            Directory.CreateDirectory(onyxRepack);
            string yamlPath = Path.Combine(onyxRepack, $"repack-stfs.yaml");
            string thumbnailPath = Path.Combine(onyxRepack, "thumbnail.png");
            string titleThumbnailPath = Path.Combine(onyxRepack, "title-thumbnail.png");

            File.WriteAllText(yamlPath, yaml);
            File.Copy(thumbnailResource, thumbnailPath, true);
            File.Copy(thumbnailResource, titleThumbnailPath, true);
        }
        public static string ReplaceNonAlphanumeric(string input)
        {
            return Regex.Replace(input, "[^a-zA-Z0-9]", "_");
        }
        public static void CompileWithOnyx(string onyxPath, string[] onyxArgs)
        {
            string onyxExe = Path.Combine(onyxPath, "onyx.exe");
            if (onyxArgs.Length == 0)
            {
                throw new Exception("No arguments were provided for Onyx compilation.");
            }
            else if (!File.Exists(onyxExe))
            {
                throw new FileNotFoundException("Onyx executable not found at the specified location.");
            }

            string compileType = onyxArgs[0];
            string compileDir;
            if (compileType == "stfs")
            {
                compileDir = onyxArgs[1];
                onyxArgs[1] = $"\"{onyxArgs[1]}\"";
                onyxArgs[3] = $"\"{onyxArgs[3]}\"";
            }
            else
            {
                compileDir = onyxArgs[2];
                onyxArgs[2] = $"\"{onyxArgs[2]}\"";
                onyxArgs[4] = $"\"{onyxArgs[4]}\"";
            }
            ProcessStartInfo startInfo = new ProcessStartInfo(onyxExe);
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;
            // startInfo.RedirectStandardOutput = true;

            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.Arguments = string.Join(" ", onyxArgs);
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = new Process())
                {
                    exeProcess.StartInfo = startInfo;
                    exeProcess.Start();

                    // StreamReader reader = exeProcess.StandardOutput;
                    // string output = reader.ReadToEnd();
                    exeProcess.WaitForExit();

                    // Console.WriteLine(output);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                Directory.Delete(compileDir, true);
            }
        }
    }
}
