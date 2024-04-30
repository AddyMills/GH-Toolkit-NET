using static GH_Toolkit_Core.PAK.PAK;
using static GH_Toolkit_Core.QB.QB;
using GH_Toolkit_Core.QB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QBConstants;
using GH_Toolkit_Core.PS360;

namespace GH_Toolkit_Core.Methods
{
    public class CreateForGame
    {
        public class Gh3SongEntry
        {
            public string Checksum { get; set; } = "";
            public string Title { get; set; } = "";
            public string Artist { get; set; } = "";
            public string ArtistTextSelect { get; set; } = "";
            public string ArtistTextCustom { get; set; } = "";
            public int Year { get; set; }
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
            public QBStruct.QBStructData GenerateGh3SongListEntry(string game, string platform)
            {
                string STEVEN = "Steven Tyler";

                var entry = new QBStruct.QBStructData();
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
        }
        public static (byte[] pakData, byte[] pabData) AddToDownloadList(string qbPakLocation, string platform, string checksum, QBStruct.QBStructData songListEntry)
        {
            var pakCompiler = new PakCompiler(GAME_GH3, platform, split: true);
            var qbPak = PakEntryDictFromFile(qbPakLocation);
            var songList = qbPak[songlistRef];
            var songListEntries = QbEntryDictFromBytes(songList.EntryData, "big");
            var dlSongList = songListEntries[gh3Songlist].Data as QBArray.QBArrayNode;
            var dlSongListProps = songListEntries[permanentProps].Data as QBStruct.QBStructData;
            var songIndex = dlSongList.GetItemIndex(checksum, QBKEY);
            if (songIndex == -1)
            {
                dlSongList.AddQbkeyToArray(checksum);
                dlSongListProps.AddStructToStruct(checksum, songListEntry);
            }
            else
            {
                dlSongListProps[checksum] = songListEntry;
            }
            byte[] songlistBytes = CompileQbFromDict(songListEntries, songlistRef,  GAME_GH3, platform);
            songList.OverwriteData(songlistBytes);

            var downloadQb = qbPak[downloadRef];
            var downloadQbEntries = QbEntryDictFromBytes(downloadQb.EntryData, "big");
            var downloadlist = downloadQbEntries[gh3DownloadSongs].Data as QBStruct.QBStructData;
            var tier1 = downloadlist["tier1"] as QBStruct.QBStructData;
            var songArray = tier1["songs"] as QBArray.QBArrayNode;


            if (songArray.GetItemIndex(checksum, QBKEY) == -1)
            {
                songArray.AddQbkeyToArray(checksum);
                tier1["defaultunlocked"] = songArray.Items.Count;
            }
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
        }
    }
}
