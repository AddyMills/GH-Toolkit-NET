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
using static GH_Toolkit_Core.Checksum.CRC;
using static GH_Toolkit_Core.INI.SongIniData;
using GH_Toolkit_Core.PS360;
using System.Diagnostics;
using GH_Toolkit_Core.Checksum;
using System.Text.RegularExpressions;
using GH_Toolkit_Core.INI;
using GH_Toolkit_Core.MIDI;
using System.Globalization;
using FFMpegCore.Builders.MetaData;

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
            public string AlbumTitle { get; set; } = "If you find this text... Hi!";
            public int? Year { get; set; }
            public string CoverArtist { get; set; } = "";
            public int? CoverYear { get; set; }
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
            public float Volume { get; set; }
            public string Countoff { get; set; } = "hihat01";
            public float HopoThreshold { get; set; } // Specifically Neversoft Hopo Threshold, not HMX
            public int HmxHopoThreshold { get; set; }
            public bool DoubleKick { get; set; } = false;
            public string DrumKit { get; set; } = "";
            public float VocalScrollSpeed { get; set; }
            public int VocalTuningCents { get; set; }
            public float SustainThreshold { get; set; }
            public bool GuitarMic { get; set; }
            public bool BassMic { get; set; }
            public int GuitarTier { get; set; } = 1;
            public int BassTier { get; set; } = 1;
            public int DrumsTier { get; set; } = 1;
            public int VocalsTier { get; set; } = 1;
            public int BandTier { get; set; } = 1;
            public int Duration { get; set; }
            public string Game { get; set; }
            public string PackageName { get 
                { 
                    return $"{Title} by {Artist}".Replace("\\L", "");
                } }
            private StringBuilder AnimLoadScript;
            public GhMetadata()
            {

            }
            public GhMetadata(SongIniData songData)
            {
                Title = songData.Title;
                Artist = songData.Artist;
                if (!songData.IsCover)
                {
                    ArtistTextSelect = "By";
                }
                else
                {
                    ArtistTextSelect = "As Made Famous By";
                    CoverArtist = songData.CoverArtist ?? "";
                    CoverYear = songData.CoverYear;
                }
                Year = songData.Year;
                Genre = songData.Genre ?? "Rock";
                ChartAuthor = songData.Charter;
                Bassist = songData.Bassist ?? "Default";
                Singer = songData.Vocalist ?? "male";
                IsArtistFamousBy = songData.IsCover;
                AerosmithBand = songData.Aerosmith ?? "aerosmith";
                Beat8thLow = songData.Low8Bars;
                Beat8thHigh = songData.High8Bars;
                Beat16thLow = songData.Low16Bars;
                Beat16thHigh = songData.High16Bars;
                OverrideBeatLines = songData.UseBeatTrack;
                CoopAudioCheck = false;
                P2RhythmCheck = false;
                BandVol = songData.BandVolume;
                GtrVol = songData.GuitarVolume;
                Volume = songData.Volume;
                HopoThreshold = 500f;
                HmxHopoThreshold = songData.HopoFrequency ?? 170;
                //DoubleKick = songData.EasyOpens;
                DrumKit = songData.Drumkit ?? "hihat01";
                VocalScrollSpeed = songData.ScrollSpeed ?? 1.0f;
                VocalTuningCents = songData.TuningCents;
                SustainThreshold = (float?)songData.SustainCutoffThreshold ?? 0.45f;
                Checksum = songData.Checksum ?? $"{CreateChecksum(Title)}";
            }

            public QBStructData GenerateGh3SongListEntry(string game, string platform)
            {
                string STEVEN = "Steven Tyler";

                var entry = new QBStructData();
                string pString = (platform == CONSOLE_PS2 || platform == CONSOLE_WII) ? STRING : WIDESTRING; // Depends on the platform
                bool artistIsOther = ArtistTextSelect == "Other";
                string artistText = !artistIsOther ? GetArtistText() : ArtistTextCustom;
                string artistType = artistIsOther ? pString : POINTER;
                string gender = (Singer == STEVEN) ? "male" : Singer;

                entry.AddVarToStruct("checksum", Checksum, QBKEY);
                entry.AddVarToStruct("name", Checksum, STRING);
                entry.AddVarToStruct("title", Title, pString);
                entry.AddVarToStruct("artist", Artist, pString);
                if (CoverArtist != "")
                {
                    entry.AddVarToStruct("covered_by", CoverArtist, pString);
                }
                if (CoverYear != 0)
                {
                    entry.AddVarToStruct("cover_year", $", {CoverYear}", pString);
                }
                if (Year > 0)
                {
                    entry.AddVarToStruct("year", $", {Year}", pString);
                }
                else
                {
                    entry.AddVarToStruct("year", "", pString);
                }
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
                if (HopoThreshold.ToString() != "2.95")
                {
                    entry.AddFloatToStruct("hammer_on_measure_scale", HopoThreshold);
                }
                
                if (Bassist != "Default")
                {
                    entry.AddVarToStruct("bassist", Bassist, QBKEY);
                }
                return entry;
            }
            public (List<QBItem>, string[]) GenerateGh5SongListEntry()
            {
                var entries = new List<QBItem>();
                var songlist = new QBArrayNode();
                var propsMaster = new QBStructData();
                var props = new QBStructData();
                var qsStrings = new List<string>();
                string qbName = Game == GAME_GH5 ? "gh5_dlc_songlist" : "gh6_dlc_songlist";

                string drumKit = DrumKit.Replace(" ", "");

                songlist.AddQbkeyToArray(Checksum);

                props.AddQbKeyToStruct("checksum", Checksum);
                props.AddStringToStruct("name", Checksum);
                props.AddQsKeyToStruct("title", Title);
                qsStrings.Add(Title);
                props.AddQsKeyToStruct("artist", Artist);
                qsStrings.Add(Artist);
                props.AddPointerToStruct("artist_text", GetArtistText());
                props.AddIntToStruct("original_artist", GetOrigArtist());
                props.AddIntToStruct("year", (int)Year);
                props.AddQsKeyToStruct("album_title", AlbumTitle);
                qsStrings.Add(AlbumTitle);
                props.AddQbKeyToStruct("singer", Singer);
                props.AddQbKeyToStruct("genre", Genre);  
                props.AddIntToStruct("leaderboard", 1);
                props.AddIntToStruct("duration", Duration);
                if (GuitarMic || BassMic)
                {
                    props.AddArrayToStruct("parts_with_mic", GetPartsWithMic());
                }
                props.AddIntToStruct("flags", 0);
                props.AddIntToStruct("double_kick", DoubleKick ? 1 : 0);
                props.AddIntToStruct("thin_fretbar_8note_params_low_bpm", Beat8thLow);
                props.AddIntToStruct("thin_fretbar_8note_params_high_bpm", Beat8thHigh);
                props.AddIntToStruct("thin_fretbar_16note_params_low_bpm", Beat16thLow);
                props.AddIntToStruct("thin_fretbar_16note_params_high_bpm", Beat16thHigh);
                props.AddIntToStruct("guitar_difficulty_rating", GuitarTier);
                props.AddIntToStruct("bass_difficulty_rating", BassTier);
                props.AddIntToStruct("drums_difficulty_rating", DrumsTier);
                props.AddIntToStruct("vocals_difficulty_rating", VocalsTier);
                props.AddIntToStruct("band_difficulty_rating", BandTier);
                props.AddStringToStruct("snare", drumKit);
                props.AddStringToStruct("kick", drumKit);
                props.AddStringToStruct("hihat", drumKit);
                props.AddStringToStruct("cymbal", drumKit);
                props.AddStringToStruct("tom1", drumKit);
                props.AddStringToStruct("tom2", drumKit);
                props.AddStringToStruct("drum_kit", drumKit);
                props.AddStringToStruct("countoff", Countoff);
                if (VocalTuningCents != 0)
                {
                    props.AddStructToStruct("vocals_pitch_score_shift", GetVocalPitch());
                }
                props.AddFloatToStruct("whammy_cutoff", SustainThreshold);
                props.AddFloatToStruct("overall_song_volume", BandVol);

                propsMaster.AddStructToStruct(Checksum, props);
                
                entries.Add(new QBItem(qbName, songlist));
                entries.Add(new QBItem($"{qbName}_props", propsMaster));

                return (entries, qsStrings.ToArray());
            }
            public string GetArtistText()
            {
                if (ArtistTextSelect == "As Made Famous By")
                {
                    return "artist_text_as_made_famous_by";
                }
                else if (ArtistTextSelect == "From")
                {
                    return "artist_text_from";
                }
                else
                {
                    return "artist_text_by";
                }
            }
            public int GetOrigArtist()
            {
               return IsArtistFamousBy ? 0 : 1;
            }
            public QBStructData GetVocalPitch()
            {
                var pitch = new QBStructData();
                pitch.AddIntToStruct("cents", VocalTuningCents);
                return pitch;
            }
            public QBArrayNode GetPartsWithMic()
            {
                var mics = new QBArrayNode();
                if (GuitarMic)
                {
                    mics.AddQbkeyToArray("guitarist");
                }
                if (BassMic)
                {
                    mics.AddQbkeyToArray("bassist");
                }
                return mics;
            }
            public void CreateConsolePackage(string game, string platform, string compilePath, string resources, string onyxPath)
            {
                string toCopyTo;
                string[] onyxArgs;
                string fileType;
                bool hasAudio = false;
                bool hasDat = game == GAME_GH3 ? false : true;
                string packageName;
                packageName = PackageName;
                var packageHash = PackageNameHashFormat(packageName);
                if (platform == CONSOLE_PS3)
                {
                    
                    fileType = "PKG";
                    Console.WriteLine("Compiling PKG file using Onyx CLI");
                    toCopyTo = Path.Combine(compilePath, "PS3");
                    string gameFiles = Path.Combine(toCopyTo, "USRDIR", packageHash);
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
                    string pkgSave = Path.Combine(CompileFolder, $"{packageHash}.pkg".ToUpper());
                    string contentPart2 = $"{Checksum}{packageHash}".ToUpper().Replace("_", "").Replace("\\L","").PadLeft(27, '0')[..27];
                    string contentID = FileCreation.GetPs3Key(game) + $"-{contentPart2}";
                    onyxArgs = ["pkg", contentID, toCopyTo, "--to", pkgSave];
                }
                else
                {
                    fileType = "STFS";
                    Console.WriteLine("Compiling STFS file using Onyx CLI");

                    CreateOnyxStfsFolder(game, resources, compilePath, packageHash);
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
                    string stfsSave = Path.Combine(CompileFolder, packageHash.Replace(" ","_"));
                    onyxArgs = ["stfs", toCopyTo, "--to", stfsSave];

                }

                if (!hasAudio || !hasDat)
                {
                    throw new Exception($"Missing audio or dat file for {fileType} creation. Please compile all files first!");
                }
                CompileWithOnyx(onyxPath, onyxArgs);
                Directory.Delete(compilePath, true);
            }
            public void MakePs2ScriptHeader()
            {
                AnimLoadScript = new StringBuilder();
                AnimLoadScript.AppendLine($"script animload_Singer_{Singer}_{Checksum} \\{{LoadFunction = LoadAnim}}");
                AnimLoadScript.Append("\tif ");
            }
            public void AddPs2ScriptEntry(string animPath)
            {
                if (AnimLoadScript == null)
                {
                    MakePs2ScriptHeader();
                }
                var animName = Path.GetFileNameWithoutExtension(animPath);
                AnimLoadScript.AppendLine($"<LoadFunction> <...> Name = '{animPath}' descChecksum = {animName}");
            }
            public void SavePs2Script(string savePath)
            {
                if (AnimLoadScript == null)
                {
                    Console.WriteLine("No entries to save. Skipping");
                    return;
                }
                AnimLoadScript.AppendLine("endif");
                AnimLoadScript.AppendLine("endscript");

                File.WriteAllText(savePath, AnimLoadScript.ToString());
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
        public static void CreateConsoleDownloadFilesGh3(uint checksum, string game, string platform, string compilePath, string resources, List<QBStruct.QBStructData> songListEntry)
        {
            if(!Directory.Exists(compilePath))
            {
                Directory.CreateDirectory(compilePath);
            }
            var gh3Resource = Path.Combine(resources, game);

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
        public static void CreateConsoleDownloadFilesGh5(uint checksum, string game, string platform, string compilePath, string resources, List<QBItem> songlistQb, string[] qsStrings, string manifest)
        {
            if (!Directory.Exists(compilePath))
            {
                Directory.CreateDirectory(compilePath);
            }

            var gameResource = Path.Combine(resources, game);
            uint toIncrement = checksum + 1;
            // Text files
            var textChecksum = $"download{checksum}\\download_songlist.qb";
            var pakCompiler = new PakCompiler(game, platform, split: false);
            if (!Directory.Exists(gameResource))
            {
                throw new Exception($"Cannot find {game} Resource folder.\n\nThis should be included with your toolkit.\nPlease re-download the toolkit.");
            }
            string packageHash = PackageNameHashFormat(manifest);
            string manifestQbName = QBKey(packageHash).Substring(2).TrimStart('0');
            string manifestFolder = Path.Combine(compilePath, $"cmanifest_{manifestQbName}");
            string manifestName = $"download{checksum}\\0xb2a7df81.qb";

            var manifestQb = MakeDlcManifest(checksum, packageHash);
            var blankScripts = "blank_scripts.qb";
            var dlcScriptsPath = Path.Combine(gameResource, blankScripts);
            var dlcScriptsFolder = Path.Combine(compilePath, $"cdl{checksum}");

            var dlcTextqb = CompileQbFile(songlistQb, textChecksum, game, platform);
            var dlcTextFolder = Path.Combine(compilePath, $"cdl{checksum}_text");

            var downloadFolder = Path.GetDirectoryName(Path.Combine(dlcTextFolder, textChecksum));
            var blankQs = "blank_qs.qs";
            var dlcTextQs = Path.Combine(gameResource, blankQs);

            Directory.CreateDirectory(dlcScriptsFolder);
            Directory.CreateDirectory(downloadFolder);
            Directory.CreateDirectory(Path.Combine(manifestFolder, $"download{checksum}"));

            var manifestQbBytes = CompileQbFile(manifestQb, manifestName, game, platform);
            File.WriteAllBytes(Path.Combine(manifestFolder, manifestName), manifestQbBytes);

            File.Copy(dlcScriptsPath, Path.Combine(dlcScriptsFolder, "0xb1392214.0x179eac5.qb"), true);
            File.WriteAllBytes(Path.Combine(dlcTextFolder, textChecksum), dlcTextqb);

            string[] locales = ["en", "fr", "de", "it", "es"];
            foreach (string locale in locales)
            {
                var tempFolder = $"download{toIncrement}";
                var tempLocale = Path.Combine(dlcTextFolder, tempFolder);
                Directory.CreateDirectory(tempLocale);
                var qsSave = Path.Combine(tempLocale, $"download_songlist.qs.{locale}");
                // Creating a StreamWriter to write to the file with UTF-16 encoding
                using (StreamWriter writer = new StreamWriter(qsSave, false, Encoding.Unicode))
                {
                    // Setting the newline character to only '\n'
                    writer.NewLine = "\n";

                    foreach (var key in qsStrings)
                    {
                        // Formatting the key as specified
                        string modifiedKey = QBKeyQs(key).Substring(2).PadLeft(8, '0');

                        // Building the line with the modified key and its value
                        string line = $"{modifiedKey} \"{key}\"";

                        // Writing the line to the file
                        writer.WriteLine(line);
                    }

                    // These are needed otherwise the game will crash.
                    writer.WriteLine();
                    writer.WriteLine();
                }
                File.Copy(dlcTextQs, Path.Combine(tempLocale, $"0x179eac5.qs.{locale}"), true);
                toIncrement++;
            }

            var (scriptsPak, _) = pakCompiler.CompilePAK(dlcScriptsFolder, platform);
            var (textPak, _) = pakCompiler.CompilePAK(dlcTextFolder, platform);
            var (manifestPak, _) = pakCompiler.CompilePAK(manifestFolder, platform);

            File.WriteAllBytes($"{dlcScriptsFolder}.pak", scriptsPak);
            File.WriteAllBytes($"{dlcTextFolder}.pak", textPak);
            File.WriteAllBytes($"{manifestFolder}.pak", manifestPak);

            Directory.Delete(dlcScriptsFolder, true);
            Directory.Delete(dlcTextFolder, true);
            Directory.Delete(manifestFolder, true);

            for (int i = 1; i < 4; i++)
            {
                string audio = Path.Combine(compilePath, $"adlc{checksum}_{i}.fsb");
                if (!File.Exists(audio))
                {
                    throw new FileNotFoundException($"Missing audio file {audio} for download file creation.");
                }
            }
            if (!File.Exists(Path.Combine(compilePath, $"adlc{checksum}_preview.fsb")))
            {
                throw new FileNotFoundException($"Missing preview audio file for download file creation.");
            }
        }
        public static string PackageNameHashFormat(string text)
        {
            // Transform each character based on the given conditions
            string TransformChar(Match match)
            {
                char c = match.Value[0];
                byte[] cBytes = Encoding.UTF8.GetBytes(new[] { c });
                StringBuilder newChar = new StringBuilder();

                foreach (byte letter in cBytes)
                {
                    char chrLetter = (char)letter;
                    if (Regex.IsMatch(chrLetter.ToString(), "[a-zA-Z0-9]"))
                    {
                        newChar.Append(chrLetter);
                    }
                    else
                    {
                        newChar.Append('_');
                    }
                }
                return newChar.ToString();
            }

            // Use regex replace to apply the transformation
            string transformedText = Regex.Replace(text, ".", new MatchEvaluator(TransformChar));

            // Truncate the result to 42 characters
            if (transformedText.Length > 42)
            {
                transformedText = transformedText.Substring(0, 42);
            }

            return transformedText;
        }
        private static List<QBItem> MakeDlcManifest(uint checksum, string packageName)
        {
            List<QBItem> manifest = new List<QBItem>();
            QBItem manifest1 = new QBItem();
            manifest1.CreateQBItem("0xe57c7c6d", "2", INTEGER);
            QBItem manifest2 = new QBItem();
            manifest2.CreateQBItem("0x53a97911", "0", INTEGER);
            QBArrayNode manifestArray = new QBArrayNode(); // Big array

            QBStructData manifestStruct = new QBStructData(); // First (and only) entry in the array

            QBArrayNode packageNames = new QBArrayNode(); // Array of package names
            packageNames.AddQbkeyToArray(packageName);
            manifestStruct.AddArrayToStruct("package_name_checksums", packageNames);

            manifestStruct.AddQbKeyToStruct("format", "gh5_dlc");
            manifestStruct.AddStringToStruct("song_pak_stem", $"cdl{checksum}");

            QBArrayNode songNums = new QBArrayNode(); // Array of song numbers
            songNums.AddIntToArray((int)checksum);
            manifestStruct.AddArrayToStruct("songs", songNums);

            manifestArray.AddStructToArray(manifestStruct);

            manifest.Add(manifest1);
            manifest.Add(manifest2);
            manifest.Add(new QBItem("dlc_manifest", manifestArray));

            return manifest;
        }
        public static uint MakeConsoleChecksum(string[] toCombine)
        {
            int minNum = 1000000000;
            string qbString = string.Concat(toCombine);
            var qbKey = CRC.QBKeyUInt(qbString);
            return (uint)(minNum + (qbKey % minNum));
        }
        public static string CreateChecksum(string toChecksum)
        {
            // Normalize the string to get the diacritics separated
            string formD = toChecksum.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char ch in formD)
            {
                // Keep the char if it is a letter and not a diacritic
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            // Remove non-alphabetic characters
            string alphanumericOnly = Regex.Replace(sb.ToString(), "[^A-Za-z]", "").ToLower();

            // Return the normalized string without diacritics and non-alphabetic characters
            return alphanumericOnly;
        }
        public static string MakeConsoleChecksumHex(string[] toCombine)
        {
            int minNum = 1000000000;
            string qbString = string.Concat(toCombine);
            var qbKey = CRC.QBKeyUInt(qbString);
            return (minNum + (qbKey % minNum)).ToString("X");
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
