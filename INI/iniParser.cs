using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GH_Toolkit_Core.Methods;
using IniParser;
using IniParser.Model;
using System.Globalization;

namespace GH_Toolkit_Core.INI
{
    public partial class iniParser
    {
        public static readonly CultureInfo enUs = CultureInfo.GetCultureInfo("en-US");
        
        private static readonly Regex AudioRegex = new Regex(@"\.(mp3|ogg|flac|wav|opus)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MidiRegex = new Regex(@"\.(mid|midi)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ChartRegex = new Regex(@"\.chart$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
                        if (decimal.TryParse(key.Value, NumberStyles.Any, enUs, out var threshold))
                            songData.SustainCutoffThreshold = threshold / 480;
                        break;
                    case "hopo_frequency":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var hopo))
                            songData.HopoFrequency = hopo;
                        break;
                    case "preview_start_time":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var previewStart) && previewStart >= 0)
                            songData.PreviewStartTime = previewStart;
                        break;
                    case "preview_end_time":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var previewEnd) && previewEnd >= 0)
                            songData.PreviewEndTime = previewEnd;
                        break;
                    case "use_beat_track":
                        songData.UseBeatTrack = bool.Parse(key.Value);
                        break;
                    case "low_8_bars":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var low8))
                            songData.Low8Bars = low8;
                        break;
                    case "high_8_bars":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var hi8))
                            songData.High8Bars = hi8;
                        break;
                    case "low_16_bars":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var low16))
                            songData.Low16Bars = low16;
                        break;
                    case "high_16_bars":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var hi16))
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
                        if (float.TryParse(key.Value, NumberStyles.Any, enUs, out var gtrVol))
                            songData.GuitarVolume = gtrVol;
                        break;
                    case "band_volume":
                        if (float.TryParse(key.Value, NumberStyles.Any, enUs, out var bandVol))
                            songData.BandVolume = bandVol;
                        break;
                    case "scroll_speed":
                        if (float.TryParse(key.Value, NumberStyles.Any, enUs, out var scrollSpeed))
                            songData.ScrollSpeed = scrollSpeed;
                        break;
                    case "tuning_cents":
                        if (int.TryParse(key.Value, NumberStyles.Integer, enUs, out var tuningCents))
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
                        if (float.TryParse(key.Value, NumberStyles.Any, enUs, out var volume))
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
                        songData.VenueSource = key.Value.ToUpper();
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
            songData.SetDefaults();
            return songData;
        }
        public static FileAssignment AssignFiles(string folder, string game)
        {
            var assignment = new FileAssignment();
            foreach (string file in Directory.GetFileSystemEntries(folder))
            {
                string fileName = Path.GetFileName(file);
                string fileNoExt = Path.GetFileNameWithoutExtension(fileName);
                string fileExt = Path.GetExtension(fileName);
                
                var fileNoExtLower = fileNoExt.AsSpan();
                var fileExtLower = fileExt.AsSpan();

                if (AudioRegex.IsMatch(fileExt))
                {
                    if (game == "GH3" || game == "GHA")
                    {
                        ProcessGH3AudioFile(assignment, file, fileNoExtLower);
                    }
                    else
                    {
                        ProcessModernGHAudioFile(assignment, file, fileNoExtLower);
                    }
                }
                else if (MidiRegex.IsMatch(fileExt) && fileNoExtLower.Equals("notes", StringComparison.OrdinalIgnoreCase))
                {
                    assignment.MidiFile = file;
                }
                else if (ChartRegex.IsMatch(fileExt) && fileNoExtLower.Equals("notes", StringComparison.OrdinalIgnoreCase))
                {
                    assignment.ChartFile = file;
                }
                else if (fileNoExtLower.Equals("perf_override", StringComparison.OrdinalIgnoreCase) && fileExtLower.Equals(".q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Found perf_override folder.");
                    assignment.PerfOverride = file;
                }
                else if (fileNoExtLower.Equals("song_scripts", StringComparison.OrdinalIgnoreCase) && fileExtLower.Equals(".q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Found song_scripts folder.");
                    assignment.SongScripts = file;
                }
                else if (fileNoExtLower.Equals("lipsync", StringComparison.OrdinalIgnoreCase) && Directory.Exists(file))
                {
                    Console.WriteLine("Found lipsync folder.");
                    assignment.LipsyncFiles = file;
                }
                else if (fileNoExtLower.Equals("ska", StringComparison.OrdinalIgnoreCase) && Directory.Exists(file))
                {
                    Console.WriteLine("Found ska folder.");
                    assignment.SkaFiles = file;
                }
            }

            PostProcessAssignments(assignment, game);
            return assignment;
        }

        private static void PostProcessAssignments(FileAssignment assignment, string game)
        {
            bool hasOtherAudioTracks = assignment.BackingTracks.Count > 0 ||
                                        !string.IsNullOrEmpty(assignment.Crowd) ||
                                        !string.IsNullOrEmpty(assignment.Vocals);

            if (game == "GH3" || game == "GHA")
            {
                hasOtherAudioTracks |= !string.IsNullOrEmpty(assignment.Rhythm);
            }
            else
            {
                hasOtherAudioTracks |= !string.IsNullOrEmpty(assignment.Bass) ||
                                        !string.IsNullOrEmpty(assignment.KickDrum) ||
                                        !string.IsNullOrEmpty(assignment.SnareDrum) ||
                                        !string.IsNullOrEmpty(assignment.Toms) ||
                                        !string.IsNullOrEmpty(assignment.Cymbals);
            }

            if (!string.IsNullOrEmpty(assignment.Guitar) && !hasOtherAudioTracks)
            {
                assignment.BackingTracks.Add(assignment.Guitar);
                assignment.Guitar = string.Empty;
            }
        }

        private static void ProcessGH3AudioFile(FileAssignment assignment, string file, ReadOnlySpan<char> fileNoExt)
        {
            if (fileNoExt.Equals("guitar", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assignment.Guitar))
                {
                    assignment.BackingTracks.Add(file);
                }
                else
                {
                    assignment.Guitar = file;
                }
            }
            else if (fileNoExt.Equals("rhythm", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assignment.Rhythm))
                {
                    assignment.BackingTracks.Add(file);
                }
                else
                {
                    assignment.Rhythm = file;
                }
            }
            else if (fileNoExt.Equals("bass", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assignment.Rhythm))
                {
                    assignment.BackingTracks.Add(assignment.Rhythm);
                }
                assignment.Rhythm = file;
            }
            else if (fileNoExt.Equals("crowd", StringComparison.OrdinalIgnoreCase))
            {
                assignment.Crowd = file;
            }
            else if (fileNoExt.Equals("preview", StringComparison.OrdinalIgnoreCase))
            {
                assignment.RenderedPreview = true;
                assignment.Preview = file;
            }
            else if (fileNoExt.Equals("song", StringComparison.OrdinalIgnoreCase))
            {
                ProcessSongFile(assignment, file);
            }
            else
            {
                assignment.BackingTracks.Add(file);
            }
        }

        private static void ProcessModernGHAudioFile(FileAssignment assignment, string file, ReadOnlySpan<char> fileNoExt)
        {
            if (fileNoExt.Equals("drums_1", StringComparison.OrdinalIgnoreCase))
            {
                assignment.KickDrum = file;
            }
            else if (fileNoExt.Equals("drums_2", StringComparison.OrdinalIgnoreCase))
            {
                assignment.SnareDrum = file;
            }
            else if (fileNoExt.Equals("drums_3", StringComparison.OrdinalIgnoreCase))
            {
                assignment.Toms = file;
            }
            else if (fileNoExt.Equals("drums_4", StringComparison.OrdinalIgnoreCase))
            {
                assignment.Cymbals = file;
            }
            else if (fileNoExt.Equals("guitar", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assignment.Guitar))
                {
                    assignment.BackingTracks.Add(file);
                }
                else
                {
                    assignment.Guitar = file;
                }
            }
            else if (fileNoExt.Equals("bass", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assignment.Bass))
                {
                    assignment.BackingTracks.Add(assignment.Bass);
                }
                assignment.Bass = file;
            }
            else if (fileNoExt.Equals("rhythm", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assignment.Bass))
                {
                    assignment.BackingTracks.Add(file);
                }
                else
                {
                    assignment.Bass = file;
                }
            }
            else if (fileNoExt.Equals("vocals", StringComparison.OrdinalIgnoreCase))
            {
                assignment.Vocals = file;
            }
            else if (fileNoExt.Equals("crowd", StringComparison.OrdinalIgnoreCase))
            {
                assignment.Crowd = file;
            }
            else if (fileNoExt.Equals("song", StringComparison.OrdinalIgnoreCase))
            {
                ProcessSongFile(assignment, file);
            }
            else if (fileNoExt.Equals("preview", StringComparison.OrdinalIgnoreCase))
            {
                assignment.RenderedPreview = true;
                assignment.Preview = file;
            }
            else
            {
                assignment.BackingTracks.Add(file);
            }
        }

        private static void ProcessSongFile(FileAssignment assignment, string file)
        {
            if (assignment.BackingTracks.Count != 0)
            {
                for (int i = 0; i < assignment.BackingTracks.Count; i++)
                {
                    if (assignment.BackingTracks[i].Contains("guitar", StringComparison.OrdinalIgnoreCase))
                    {
                        assignment.Guitar = assignment.BackingTracks[i];
                        assignment.BackingTracks.RemoveAt(i);
                        break;
                    }
                }
            }
            assignment.BackingTracks.Add(file);
        }
        public static int ChDiffToGh(string chDiff)
        {
            float chParsed = float.Parse(chDiff, enUs) + 1;
            if (chParsed <= 0)
            {
                return 0;
            }
            int ghDiff = (int)Math.Round(chParsed * 10f / 7f);
            return Math.Clamp(ghDiff, 1, 10);
        }

    }
}
