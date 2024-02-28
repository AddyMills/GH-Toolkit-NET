using System.Linq;
using GH_Toolkit_Core.Checksum;

namespace GH_Toolkit_Core.Debug
{
    public class DebugHeaders
    {
        private static readonly string[] playableParts = { "", "rhythm", "guitarcoop", "rhythmcoop", "drum", "aux" };
        private static readonly string[] difficulties = { "Easy", "Medium", "Hard", "Expert" };
        private static readonly string[] charts = { "song", "Star", "StarBattleMode", "Tapping", "WhammyController", "SoloMarkers" };
        private static readonly string[] faceOff = { "FaceOffP1", "FaceOffP2", "FaceOffStar" };
        private static readonly string[] others = {"_BossBattleP1", "_BossBattleP2", "_timesig", "_fretbars", "_markers",
          "_scripts_notes", "_anim_notes", "_triggers_notes", "_cameras_notes", "_lightshow_notes", "_crowd_notes",
          "_drums_notes", "_performance_notes", "_scripts", "_anim", "_triggers", "_cameras", "_lightshow", "_crowd",
          "_drums", "_performance", "_song_drums_expertplus"};
        private static readonly string[] markersWT = { "_guitar_markers", "_rhythm_markers", "_drum_markers" };
        private static readonly string[] othersWT = { "_facial", "_localized_strings", "_scriptevents", "_song_startup", "_vox_sp", "_ghost_notes", "_double_kick" };
        private static readonly string[] drumWT = { "DrumFill", "DrumUnmute" };
        private static readonly string[] vocalsWT = { "_freeform", "_phrases", "_note_range", "_markers" };
        private static readonly string[] songsFolder = { ".mid.qb", "_song_scripts.qb", ".mid.qs", ".note", ".perf", ".perf.xml.qb", ".qs.de", ".qs.en", ".qs.es", ".qs.fr", ".qs.it", "_rms.qd" };
        private static readonly string[] qsExtensions = { ".qs.de", ".qs.en", ".qs.es", ".qs.fr", ".qs.it" };
        private static readonly string[] animsPre = { "car_female_anim_struct_", "car_male_anim_struct_", "car_female_alt_anim_struct_", "car_male_alt_anim_struct_" };

        private static readonly string[] dlcDownloadFolder = { "download_song", "songlist" };
        private static readonly string[] dlcSongsFolder = { ".mid_text.qb" };

        public static Dictionary<uint, string> CreateHeaderDict(string filename)
        {
            List<string> headers = new List<string>();
            Dictionary<uint, string> headerDict = new Dictionary<uint, string>();

            foreach (var x in playableParts)
            {
                foreach (var z in charts)
                {
                    foreach (var y in difficulties)
                    {
                        if (z == "song")
                        {
                            if (string.IsNullOrEmpty(x))
                                headers.Add($"{filename}_{z}_{y}");
                            else
                                headers.Add($"{filename}_{z}_{x}_{y}");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(x))
                                headers.Add($"{filename}_{y}_{z}");
                            else
                                headers.Add($"{filename}_{x}_{y}_{z}");
                        }
                    }
                }

                foreach (var z in faceOff)
                {
                    if (string.IsNullOrEmpty(x))
                        headers.Add($"{filename}_{z}");
                    else
                        headers.Add($"{filename}_{x}_{z}");
                }
            }

            var listsToProcess = new List<string[]> { others, othersWT, markersWT };

            foreach (var list in listsToProcess)
            {
                foreach (var x in list)
                {
                    headers.Add($"{filename}{x}");
                }
            }

            foreach (var x in drumWT)
            {
                foreach (var y in difficulties)
                {
                    headers.Add($"{filename}_{y}_{x}");
                }
            }

            foreach (var x in vocalsWT)
            {
                headers.Add($"{filename}_vocals{x}");
            }

            headers.Add($"{filename}_song_vocals");
            headers.Add($"{filename}_lyrics");

            foreach (var x in songsFolder)
            {
                headers.Add($"songs/{filename}{x}");
            }

            foreach (var x in animsPre)
            {
                headers.Add($"{x}{filename}");
            }

            foreach (var x in headers)
            {
                string hexVal = CRC.QBKey(x);
                headerDict[Convert.ToUInt32(hexVal, 16)] = x;

            }

            return headerDict;
        }
        public static Dictionary<uint, string> CreateDlcDict(string filename)
        {
            List<string> headers = new List<string>();
            Dictionary<uint, string> headerDict = new Dictionary<uint, string>();

            foreach (var x in dlcDownloadFolder)
            {
                headers.Add($"download\\{x}{filename}.qb");
            }
            foreach (var x in dlcSongsFolder)
            {
                headers.Add($"songs\\{x}{filename}.qb");
            }

            foreach (var x in headers)
            {
                string hexVal = CRC.QBKey(x);
                headerDict[Convert.ToUInt32(hexVal, 16)] = x;

            }

            return headerDict;
        }
    }
}
