using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GH_Toolkit_Core.Methods;
using IniParser;
using IniParser.Model;

namespace GH_Toolkit_Core.INI
{
    public class iniParser
    {
        public static IniData ReadIniFromPath(string path)
        {
            var parser = new IniParser.Parser.IniDataParser();
            var textData = ReadWrite.ReadFileContent(path);
            IniData data = parser.Parse(textData);
            return data;
        }
        public static SongIniData ParseSongIni(IniData ini, string iniSection)
        {
            var songData = new SongIniData();
            songData.HopoFrequency = 170;

            foreach (var key in ini[iniSection])
            {
                switch (key.KeyName)
                {
                    case "name":
                        songData.Title = key.Value;
                        break;
                    case "artist":
                        songData.Artist = key.Value;
                        break;
                    case "charter":
                        songData.Charter = key.Value;
                        break;
                    case "frets":
                        if (string.IsNullOrEmpty(songData.Charter))
                        {
                            songData.Charter = key.Value;
                        }
                        break;
                    case "genre":
                        songData.Genre = key.Value;
                        break;
                    case "checksum":
                        songData.Checksum = key.Value;
                        break;
                    case "year":
                        if (int.TryParse(key.Value, out var year))
                            songData.Year = year;
                        break;
                    case "diff_band":
                        songData.BandTier = ChDiffToGh(key.Value);
                        break;
                    case "diff_guitar":
                        songData.GuitarTier = ChDiffToGh(key.Value);
                        break;
                    case "diff_bass":
                        songData.BassTier = ChDiffToGh(key.Value);
                        break;
                    case "diff_drums":
                        songData.DrumsTier = ChDiffToGh(key.Value);
                        break;
                    case "diff_vocals":
                        songData.VocalsTier = ChDiffToGh(key.Value);
                        break;
                    case "sustain_cutoff_threshold":
                        if (decimal.TryParse(key.Value, out var threshold))
                            songData.SustainCutoffThreshold = threshold / 480;
                        break;
                    case "hopo_frequency":
                        if (int.TryParse(key.Value, out var hopo))
                            songData.HopoFrequency = hopo;
                        break;
                    case "preview_start_time":
                        if (int.TryParse(key.Value, out var previewStart) && previewStart >= 0)
                            songData.PreviewStartTime = previewStart;
                        break;
                    case "preview_end_time":
                        if (int.TryParse(key.Value, out var previewEnd) && previewEnd >= 0)
                            songData.PreviewEndTime = previewEnd;
                        break;
                    case "use_beat_track":
                        songData.UseBeatTrack = bool.Parse(key.Value);
                        break;
                    case "low_8_bars":
                        if (int.TryParse(key.Value, out var low8))
                            songData.Low8Bars = low8;
                        break;
                    case "high_8_bars":
                        if (int.TryParse(key.Value, out var hi8))
                            songData.High8Bars = hi8;
                        break;
                    case "low_16_bars":
                        if (int.TryParse(key.Value, out var low16))
                            songData.Low16Bars = low16;
                        break;
                    case "high_16_bars":
                        if (int.TryParse(key.Value, out var hi16))
                            songData.High16Bars = hi16;
                        break;
                    case "countoff":
                        songData.Countoff = key.Value;
                        break;
                    case "drumkit":
                        songData.Drumkit = key.Value;
                        break;
                    case "vocal_gender":
                        songData.Gender = key.Value;
                        break;
                    case "vocalist":
                        songData.Vocalist = key.Value;
                        break;
                    case "aerosmith":
                        songData.Aerosmith = key.Value;
                        break;
                    case "bassist":
                        songData.Bassist = key.Value;
                        break;
                    case "guitar_volume":
                        if (float.TryParse(key.Value, out var gtrVol))
                            songData.GuitarVolume = gtrVol;
                        break;
                    case "band_volume":
                        if (float.TryParse(key.Value, out var bandVol))
                            songData.BandVolume = bandVol;
                        break;
                    case "scroll_speed":
                        if (float.TryParse(key.Value, out var scrollSpeed))
                            songData.ScrollSpeed = scrollSpeed;
                        break;
                    case "tuning_cents":
                        if (int.TryParse(key.Value, out var tuningCents))
                            if (tuningCents >= -50 && tuningCents <= 50)
                            {
                                songData.TuningCents = tuningCents;
                            }
                            else
                            {
                                Console.WriteLine("Tuning cents must be between -50 and 50. Defaulting to 0.");
                                songData.TuningCents = 0;
                            }
                        break;
                    case "volume":
                        if (float.TryParse(key.Value, out var volume))
                            songData.Volume = volume;
                        break;
                    case "guitar_mic":
                        songData.GuitarMic = bool.Parse(key.Value);
                        break;
                    case "bass_mic":
                        songData.BassMic = bool.Parse(key.Value);
                        break;
                    case "easy_opens":
                        songData.EasyOpens = bool.Parse(key.Value);
                        break;
                    case "lipsync_source":
                        songData.LipsyncSource = key.Value;
                        break;
                    case "ska_source":
                        songData.SkaSource = key.Value;
                        break;
                    case "hopo_type":
                        songData.HopoType = key.Value;
                        break;
                    case "venue_source":
                        songData.VenueSource = key.Value;
                        break;
                    case "wtde_game_icon":
                        songData.WtdeGameIcon = key.Value;
                        break;
                    case "wtde_game_category":
                        songData.WtdeGameCategory = key.Value;
                        break;
                    case "wtde_band":
                        songData.WtdeBand = key.Value;
                        break;
                    case "gskeleton":
                        songData.Gskeleton = key.Value;
                        break;
                    case "bskeleton":
                        songData.Bskeleton = key.Value;
                        break;
                    case "dskeleton":
                        songData.Dskeleton = key.Value;
                        break;
                    case "vskeleton":
                        songData.Vskeleton = key.Value;
                        break;
                    case "use_new_clips":
                        songData.UseNewClips = bool.Parse(key.Value);
                        break;
                    case "modern_strobes":
                        songData.ModernStrobes = bool.Parse(key.Value);
                        break;
                }
            }

            return songData;
        }
        public static FileAssignment AssignFiles(string folder, string game)
        {
            var assignment = new FileAssignment();
            foreach (string file in Directory.GetFileSystemEntries(folder))
            {
                string fileNoExt = Path.GetFileNameWithoutExtension(file).ToLower();
                string fileExt = Path.GetExtension(file).ToLower();
                string audioRegex = ".*\\.(mp3|ogg|flac|wav)$";
                string midiRegex = ".*\\.(mid|midi)$";

                if (Regex.IsMatch(fileExt, audioRegex))
                {
                    if (game == "GH3" || game == "GHA")
                    {
                        // GH3/GHA file assignments
                        switch (fileNoExt)
                        {
                            case "guitar":
                                if (assignment.BackingTracks.Count != 0)
                                {
                                    assignment.Guitar = file;
                                }
                                else
                                {
                                    assignment.BackingTracks.Add(file);
                                }
                                break;
                            case "rhythm":
                                if (!string.IsNullOrEmpty(assignment.Rhythm))
                                {
                                    assignment.BackingTracks.Add(file);
                                }
                                else
                                {
                                    assignment.Rhythm = file;
                                }
                                break;
                            case "bass":
                                if (!string.IsNullOrEmpty(assignment.Rhythm))
                                {
                                    assignment.BackingTracks.Add(assignment.Rhythm);
                                }
                                assignment.Rhythm = file;
                                break;
                            case "crowd":
                                assignment.Crowd = file;
                                break;
                            case "preview":
                                assignment.RenderedPreview = true;
                                assignment.Preview = file;
                                break;
                            case "song":
                                bool removeGtr = false;
                                if (assignment.BackingTracks.Count != 0)
                                {
                                    foreach (var gtrCheck in assignment.BackingTracks)
                                    {
                                        if (gtrCheck.ToLower().Contains("guitar"))
                                        {
                                            assignment.Guitar = gtrCheck;
                                            removeGtr = true;
                                        }
                                    }
                                    if (removeGtr)
                                    {
                                        assignment.BackingTracks.Remove(assignment.Guitar);
                                    }
                                }
                                assignment.BackingTracks.Add(file);
                                break;
                            default:
                                assignment.BackingTracks.Add(file);
                                break;
                        }
                    }
                    else
                    {
                        // Modern GH file assignments
                        switch (fileNoExt)
                        {
                            case "drums_1":
                                assignment.KickDrum = file;
                                break;
                            case "drums_2":
                                assignment.SnareDrum = file;
                                break;
                            case "drums_3":
                                assignment.Toms = file;
                                break;
                            case "drums_4":
                                assignment.Cymbals = file;
                                break;
                            case "guitar":
                                if (assignment.BackingTracks.Count != 0)
                                {
                                    assignment.Guitar = file;
                                }
                                else
                                {
                                    assignment.BackingTracks.Add(file);
                                }
                                break;
                            case "bass":
                                if (!string.IsNullOrEmpty(assignment.Bass))
                                {
                                    assignment.BackingTracks.Add(assignment.Bass);
                                }
                                assignment.Bass = file;
                                break;
                            case "rhythm":
                                if (!string.IsNullOrEmpty(assignment.Bass))
                                {
                                    assignment.BackingTracks.Add(file);
                                }
                                else
                                {
                                    assignment.Bass = file;
                                }
                                break;
                            case "vocals":
                                assignment.Vocals = file;
                                break;
                            case "crowd":
                                assignment.Crowd = file;
                                break;
                            case "song":
                                bool removeGtr = false;
                                if (assignment.BackingTracks.Count != 0)
                                {
                                    foreach (var gtrCheck in assignment.BackingTracks)
                                    {
                                        if (gtrCheck.ToLower().Contains("guitar"))
                                        {
                                            assignment.Guitar = gtrCheck;
                                            removeGtr = true;
                                        }
                                    }
                                    if (removeGtr)
                                    {
                                        assignment.BackingTracks.Remove(assignment.Guitar);
                                    }
                                }
                                assignment.BackingTracks.Add(file);
                                break;
                            case "preview":
                                assignment.RenderedPreview = true;
                                assignment.Preview = file;
                                break;
                            default:
                                assignment.BackingTracks.Add(file);
                                break;
                        }
                    }
                }
                else if (Regex.IsMatch(fileExt, midiRegex) && fileNoExt == "notes")
                {
                    assignment.MidiFile = file;
                }
                else if (fileNoExt == "perf_override" && Directory.Exists(file))
                {
                    Console.WriteLine("Found perf_override folder.");
                    assignment.PerfOverride = file;
                }
                else if (fileNoExt == "song_scripts" && Directory.Exists(file))
                {
                    Console.WriteLine("Found song_scripts folder.");
                    assignment.SongScripts = file;
                }
                else if (fileNoExt == "lipsync" && Directory.Exists(file))
                {
                    Console.WriteLine("Found lipsync folder.");
                    assignment.LipsyncFiles = file;
                }
                else if (fileNoExt == "ska" && Directory.Exists(file))
                {
                    Console.WriteLine("Found ska folder.");
                    assignment.SkaFiles = file;
                }
            }

            return assignment;
        }
        public static int ChDiffToGh(string chDiff)
        {
            float chParsed = float.Parse(chDiff) + 1;
            if (chParsed <= 0)
            {
                return 0;
            }
            int ghDiff = (int)Math.Round(chParsed * 10f / 7f);
            return Math.Clamp(ghDiff, 1, 10);
        }

    }
}
