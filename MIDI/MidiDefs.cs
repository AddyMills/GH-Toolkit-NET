using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidiTheory = Melanchall.DryWetMidi.MusicTheory;

namespace GH_Toolkit_Core.MIDI
{
    public class MidiDefs
    {
        public const int GH3FORCE = 32;

        public const string GUITAR_NAME = "";
        public const string RHYTHM_NAME = "_rhythm";
        public const string GUITARCOOP_NAME = "_guitarcoop";
        public const string RHYTHMCOOP_NAME = "_rhythmcoop";
        public const string AUX_NAME = "_aux";
        public const string DRUMS_NAME = "_drum";

        // Rock Band crowd names
        public const string INTENSE = "intense";
        public const string NORMAL = "normal";
        public const string MELLOW = "mellow";
        public const string REALTIME = "realtime";
        public const string CLAP = "clap";
        public const string NOCLAP = "noclap";

        // Guitar Hero 4 crowd names
        public const string CROWD_SWELL = "swell";
        public const string CROWD_WHISTLE = "whistle";
        public const string CROWD_QUIET = "quiet";
        public const string CROWD_LOUD = "loud";
        public const string SURGE_FAST_SMALL = "surge_fast_small";
        public const string SURGE_FAST_BIG = "surge_fast_big";
        public const string SURGE_MEDIUM = "surge_medium";
        public const string SURGE_SLOW_BIG = "surge_slow_big";


        // Guitar Hero 3 crowd names
        public const string SURGE_FAST = "surge_fast";
        public const string SURGE_SLOW = "surge_slow";

        // Common Crowd names
        public const string APPLAUSE = "applause";
        public const string MUSIC_END = "music_end";
        public const string MUSIC_START = "music_start";
        public const string CODA = "coda";
        public const string THE_END = "end";

        public static Dictionary<MidiTheory.NoteName, int> Gh3Notes = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.C, 1 },
            { MidiTheory.NoteName.CSharp, 2 },
            { MidiTheory.NoteName.D, 4 },
            { MidiTheory.NoteName.DSharp, 8 },
            { MidiTheory.NoteName.E, 16 }
        };

        public static Dictionary<MidiTheory.NoteName, int> Gh4Notes = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.B, 32 },
            { MidiTheory.NoteName.C, 1 },
            { MidiTheory.NoteName.CSharp, 2 },
            { MidiTheory.NoteName.D, 4 },
            { MidiTheory.NoteName.DSharp, 8 },
            { MidiTheory.NoteName.E, 16 }
            
        };

        public static Dictionary<MidiTheory.NoteName, int> Gh4Drums = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.B, 64 }, // Expert +
            { MidiTheory.NoteName.C, 32 },
            { MidiTheory.NoteName.CSharp, 2 },
            { MidiTheory.NoteName.D, 4 },
            { MidiTheory.NoteName.DSharp, 8 },
            { MidiTheory.NoteName.E, 16 },
            { MidiTheory.NoteName.F, 1 }
        };

        public static Dictionary<int, int> leftHandGtr_gh3 = new Dictionary<int, int>
        {
            {40, 127},
            {41, 127},
            {42, 126},
            {43, 126},
            {44, 125},
            {45, 125},
            {46, 124},
            {47, 124},
            {48, 123},
            {49, 123},
            {50, 122},
            {51, 122},
            {52, 121},
            {53, 121},
            {54, 120},
            {55, 120},
            {56, 119},
            {57, 119},
            {58, 118},
            {59, 118}
        };
        public static Dictionary<int, int> leftHandBass_gh3 = new Dictionary<int, int>
        {
            {40, 110},
            {41, 110},
            {42, 109},
            {43, 109},
            {44, 108},
            {45, 108},
            {46, 107},
            {47, 107},
            {48, 106},
            {49, 106},
            {50, 105},
            {51, 105},
            {52, 104},
            {53, 104},
            {54, 103},
            {55, 103},
            {56, 102},
            {57, 102},
            {58, 101},
            {59, 101}
        };

        // GHA only
        public static Dictionary<int, int> leftHandRhythm = new Dictionary<int, int>
        {
            {40, 93},
            {41, 93},
            {42, 92},
            {43, 92},
            {44, 91},
            {45, 91},
            {46, 90},
            {47, 90},
            {48, 89},
            {49, 89},
            {50, 88},
            {51, 88},
            {52, 87},
            {53, 87},
            {54, 86},
            {55, 86},
            {56, 85},
            {57, 85},
            {58, 84},
            {59, 84}
        };

        public static Dictionary<string, Dictionary<int, int>> leftHandMappingsGh3 = new Dictionary<string, Dictionary<int, int>>
        {
            {GUITAR_NAME, leftHandGtr_gh3},
            {GUITARCOOP_NAME, leftHandGtr_gh3},
            {RHYTHM_NAME, leftHandBass_gh3},
            {RHYTHMCOOP_NAME, leftHandBass_gh3},
            {AUX_NAME, leftHandRhythm}
        };

        public static Dictionary<int, int> leftHandGtr_wt = new Dictionary<int, int>
        {
            {40, 127},
            {41, 126},
            {42, 125},
            {43, 124},
            {44, 123},
            {45, 122},
            {46, 121},
            {47, 120},
            {48, 119},
            {49, 119},
            {50, 118},
            {51, 117},
            {52, 116},
            {53, 115},
            {54, 114},
            {55, 113},
            {56, 112},
            {57, 111},
            {58, 110},
            {59, 109}
        };

        public static Dictionary<int, int> leftHandBass_wt = new Dictionary<int, int>
        {
            {40, 103},
            {41, 102},
            {42, 101},
            {43, 100},
            {44, 99},
            {45, 98},
            {46, 97},
            {47, 96},
            {48, 95},
            {49, 95},
            {50, 94},
            {51, 93},
            {52, 92},
            {53, 91},
            {54, 90},
            {55, 89},
            {56, 88},
            {57, 87},
            {58, 86},
            {59, 85}
        };

        public static Dictionary<string, Dictionary<int, int>> leftHandMappingsWt = new Dictionary<string, Dictionary<int, int>>
        {
            {GUITAR_NAME, leftHandGtr_wt},
            {RHYTHM_NAME, leftHandBass_wt}
        };

        public static Dictionary<int, int> drumKeyMapRB_gh3 = new Dictionary<int, int>
        {
            {51, 49},
            {50, 37},
            {49, 50},
            {48, 38},
            {47, 51},
            {46, 39},
            {45, 45},
            {44, 45},
            {43, 43},
            {42, 55},
            {41, 57},
            {40, 56},
            {39, 57},
            {38, 57},
            {37, 56},
            {36, 56},
            {35, 44},
            {34, 44},
            {32, 50},
            {31, 53},
            {30, 41},
            {29, 52},
            {28, 40},
            {27, 52},
            {26, 40},
            {24, 48},
            {23, 36},
            {22, 70}
        };

        public static Dictionary<int, int> drumKeyMapRB_wt = new Dictionary<int, int>
        {
            {51, 74},
            {50, 74},
            {49, 75},
            {48, 75},
            {47, 76},
            {46, 76},
            {45, 82},
            {44, 82},
            {43, 80},
            {42, 80},
            {41, 82},
            {40, 81},
            {39, 82},
            {38, 82},
            {37, 81},
            {36, 81},
            {35, 81},
            {34, 81},
            {32, 75},
            {31, 78},
            {30, 78},
            {29, 77},
            {28, 77},
            {27, 77},
            {26, 77},
            {24, 73},
            {23, 73},
            {22, 83},
            {21, 65},
            {20, 83},
            {19, 70}
        };

        public static Dictionary<int, int> drumKeyMapRB_wor = new Dictionary<int, int>
        {
            {51, 32},
            {50, 2},
            {49, 33},
            {48, 3},
            {47, 34},
            {46, 4},
            {45, 15},
            {44, 15},
            {43, 12},
            {42, 42},
            {41, 50},
            {40, 49},
            {39, 45},
            {38, 45},
            {37, 44},
            {36, 44},
            {35, 14},
            {34, 14},
            {32, 41},
            {31, 39},
            {30, 9},
            {29, 36},
            {28, 6},
            {27, 35},
            {26, 5},
            {24, 30},
            {23, 0},
            {22, 109},
            {21, 95}
        };

        public static Dictionary<string, int> crowdMapGh3 = new Dictionary<string, int>
        {
            {APPLAUSE, 75},
            {SURGE_FAST, 76},
            {SURGE_SLOW, 77},
            {MUSIC_END, 75},
            {MUSIC_START, 76},
            {CODA, 79},
            {INTENSE, 75},
            {NORMAL, 76},
            {MELLOW, 77},
            {REALTIME, 78},
            {CLAP, 75},
            {NOCLAP, 77},
            // These are "backports" from WT
            {SURGE_FAST_SMALL, 76},
            {SURGE_FAST_BIG, 76},
            {SURGE_MEDIUM, 77},
            {SURGE_SLOW_BIG, 77}
        };

        public static Dictionary<string, int> crowdMapWt = new Dictionary<string, int>
        {
            {APPLAUSE, 83},
            {SURGE_FAST, 87},
            {SURGE_SLOW, 89},
            {MUSIC_END, 84},
            {MUSIC_START, 76},
            {CODA, 87},
            {INTENSE, 87},
            {NORMAL, 88},
            {MELLOW, 89},
            {REALTIME, 83},
            {CLAP, 83},
            {NOCLAP, 87},
            {CROWD_SWELL, 82},
            {CROWD_WHISTLE, 85},
            {CROWD_QUIET, 95},
            {CROWD_LOUD, 96},
            {SURGE_FAST_SMALL, 86},
            {SURGE_FAST_BIG, 87},
            {SURGE_MEDIUM, 88},
            {SURGE_SLOW_BIG, 89}
        };

        public static Dictionary<int, int> blendLookup = new Dictionary<int, int> // Used for emulating Note 107 mode in GH3
            {
                {1000, 53},
                {900, 52},
                {800, 51},
                {700, 50},
                {600, 49},
                {500, 48},
                {400, 47},
                {300, 46},
                {250, 45},
                {200, 44},
                {150, 43},
                {100, 42},
                {50, 41},
                {0, 40}
            };

        public enum Colours
        {
            Green = 1,
            Red = 2,
            Yellow = 4,
            Blue = 8,
            Orange = 16,
            Purple = 32,
            Double = 64
        }
        

        // This helper function is necessary since DryWetMidi has issues with notes on different channels.
        public static List<Chord> GroupNotes(IEnumerable<Note> notes, long tickThreshold)
        {
            var sortedNotes = notes.OrderBy(n => n.Time).ToList();
            List<Chord> chords = new List<Chord>();
            List<Note> currentChordNotes = new List<Note>();

            foreach (var note in sortedNotes)
            {
                if (currentChordNotes.Count == 0 || IsNoteInChord(note, currentChordNotes.Last(), tickThreshold))
                {
                    currentChordNotes.Add(note);
                }
                else
                {
                    if (currentChordNotes.Count > 0)
                    {
                        chords.Add(new Chord(currentChordNotes));
                        currentChordNotes.Clear();
                    }
                    currentChordNotes.Add(note);
                }
            }

            // Add the last chord if it's not empty
            if (currentChordNotes.Count > 0)
            {
                chords.Add(new Chord(currentChordNotes));
            }

            return chords;
        }

        public static bool IsNoteInChord(Note n1, Note n2, long tickThreshold)
        {
            return Math.Abs(n1.Time - n2.Time) <= tickThreshold;
        }

    }
}
