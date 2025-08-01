﻿using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidiTheory = Melanchall.DryWetMidi.MusicTheory;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.MIDI.AnimStruct;

/*
 * * This file is intended to be a collection of constants and helper functions for use in creating songs from a MIDI file
 * 
 * 
 * Author: AddyMills
 * */

namespace GH_Toolkit_Core.MIDI
{
    public partial class MidiDefs
    {
        public enum VocalPhraseType
        {
            Lyrics,
            Freeform
        }

        public enum HopoType
        {
            RB = 0,
            MoonScraper = 1,
            GH3 = 2,
            GHWT = 3
        }
        public const int GH3FORCE = 32;
        public const int GH5FORCE = 1 << 6;

        // Score Stuff

        public const int BASENOTE = 50;
        public const float POINTSPERBEAT = 25.0f;
        public const float WHAMMYSHORTENGH3 = 0.25f;
        public const float WHAMMYSHORTEN = 0.0075f;

        // Define constants for note ranges
        public const int EasyNoteMin = 60;
        public const int EasyNoteMax = 64;
        public const int MediumNoteMin = 72;
        public const int MediumNoteMax = 76;
        public const int HardNoteMin = 84;
        public const int HardNoteMax = 88;
        public const int ExpertNoteMin = 96;
        public const int ExpertNoteMax = 100;
        public const int SoloNote = 103;
        public const int TapNote = 104;
        public const int FaceOffP1Note = 105;
        public const int FaceOffP2Note = 106;
        public const int FaceOffStarNote = 107;
        public const int BattleStarNote = 115;
        public const int StarPowerNote = 116;
        public const int VocalMin = 36;
        public const int VocalMax = 84;
        public const int VocalTalkie = 26;
        public const int PhraseMin = 105;
        public const int PhraseMax = 106;
        public const int FreeformNote = 104;

        public const int GuitarAnimStart = 40;
        public const int GuitarAnimEnd = 59;

        public const int DrumAnimStart = 20;
        public const int DrumHiHatOpen = 25;
        public const int DrumHiHatRight = 31;
        public const int DrumHiHatLeft = 30;
        public const int DrumAnimEnd = 51;

        public const int FreeformChannel = 0;
        public const int HypeChannel = 1;

        public const int DownbeatNote = 12;
        public const int UpbeatNote = 13;

        public const string GUITAR_NAME = "";
        public const string RHYTHM_NAME = "_rhythm";
        public const string GUITARCOOP_NAME = "_guitarcoop";
        public const string RHYTHMCOOP_NAME = "_rhythmcoop";
        public const string AUX_NAME = "_aux";
        public const string DRUMS_NAME = "_drum";
        public const string VOCALS_NAME = "vocals";

        public const string PARTDRUMS = "PART DRUMS";
        public const string PARTGUITAR = "PART GUITAR";
        public const string PARTRHYTHM = "PART RHYTHM";
        public const string PARTGUITARCOOP = "PART GUITAR COOP";
        public const string PARTBASS = "PART BASS";
        public const string PARTAUX = "PART AUX";
        public const string PARTVOCALS = "PART VOCALS";
        public const string EVENTS = "EVENTS";
        public const string BEAT = "BEAT";
        public const string CAMERAS = "CAMERAS";
        public const string LIGHTSHOW = "LIGHTSHOW";
        public const string DRUMS = "DRUMS";
        public const string CROWD = "CROWD";
        public const string ANIMS = "ANIMS";

        public const string GUITARIST = "guitarist";
        public const string BASSIST = "bassist";
        public const string DRUMMER = "drummer";
        public const string VOCALIST = "vocalist";
        public const string RHYTHM = "rhythm";

        public const string GH5DRUMS = "drums";
        public const string GH5GUITAR = "guitar";
        public const string GH5BASS = "bass";
        public const string GH5VOCALS = "vocal";
        public const string INSTRUMENT = "instrument";
        public const string STARPOWER = "starpower";
        public const string TAPPING = "tapping";
        public const string DRUMFILL = "drumfill";


        public const string SLIDE_LYRIC = "+";
        public const string JOIN_LYRIC = "-";
        public const string HYPHEN_LYRIC = "=";
        public const string TALKIE_LYRIC = "#";
        public const string TALKIE_LYRIC2 = "^";
        public const string RANGE_SHIFT_LYRIC = "%";
        public const string UNKNOWN_LYRIC = "$";
        public const string LINKED_LYRIC = "§";

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
        public const string SURGE_FAST_OVERRIDE = "surge_fast_override";
        public const string SURGE_SLOW = "surge_slow";
        public const string SURGE_SLOW_OVERRIDE = "surge_slow_override";
        public const string APPLAUSE_OVERRIDE = "applause_override";

        // Common Crowd names
        public const string APPLAUSE = "applause";
        public const string MUSIC_END = "music_end";
        public const string MUSIC_START = "music_start";
        public const string CODA = "coda";
        public const string THE_END = "end";

        // Script Names
        public const string BAND_PLAYANIM = "band_playanim";
        public const string BAND_CHANGESTANCE = "band_changestance";
        public const string BAND_WALKTONODE = "band_walktonode";
        public const string GUITAR_WALK01 = "guitarist_walk01";
        public const string GUITAR_START = "guitarist_start";

        public const string CROWD_STARTLIGHTERS = "crowd_startlighters";
        public const string STARTLIGHTERS = "startlighters";
        public const string CROWD_STOPLIGHTERS = "crowd_stoplighters";
        public const string STOPLIGHTERS = "stoplighters";
        public const string CROWD_STAGEDIVER_JUMP = "crowd_stagediver_jump";
        public const string STAGEDIVER_JUMP = "stagediver_jump";

        public const string BAND_PLAYFACIALANIM = "band_playfacialanim";
        public const string LIGHTSHOW_SETTIME = "lightshow_settime";
        public const string SETBLENDTIME = "setblendtime";
        public const string FOVPULSE = "cameracutseffect_fovpulse";

        public const string ZOOM_IN_QUICK_SMALL = "zoom_in_quick_small";
        public const string ZOOM_IN_QUICK_LARGE = "zoom_in_quick_large";
        public const string ZOOM_OUT_QUICK_SMALL = "zoom_out_quick_small";
        public const string ZOOM_OUT_QUICK_LARGE = "zoom_out_quick_large";
        public const string ZOOM_IN_SLOW_SMALL = "zoom_in_slow_small";
        public const string ZOOM_IN_SLOW_LARGE = "zoom_in_slow_large";
        public const string ZOOM_OUT_SLOW_SMALL = "zoom_out_slow_small";
        public const string ZOOM_OUT_SLOW_LARGE = "zoom_out_slow_large";
        public const string PULSE1 = "pulse1";
        public const string PULSE2 = "pulse2";
        public const string PULSE3 = "pulse3";
        public const string PULSE4 = "pulse4";
        public const string PULSE5 = "pulse5";
        public const string FADEOUTANDIN = "fadeoutandin";
        public const string FADEINANDOUT = "fadeinandout";


        // Guitar Hero 3 Stance Names
        // Stance A and B work on the singer as well, stance C is guitarist only, stance D is GHA only
        public const string STANCE = "stance";
        public const string STANCE_A = "stance_a";
        public const string STANCE_B = "stance_b";
        public const string STANCE_C = "stance_c";
        public const string STANCE_D = "stance_d";

        // Guitar Hero 3 Song Anims
        public const string SPECIAL = "special";
        public const string JUMP = "jump";
        public const string KICK = "kick";
        public const string RELEASE = "release";
        public const string LONG_NOTE = "long_note";
        public const string SOLO = "solo";
        public const string HANDSOFF = "handsoff";
        public const string ENDSTRUM = "endstrum";

        // Guitar Hero 3 Song Anim Flags
        public const string NO_WAIT = "no_wait";
        public const string CYCLE = "cycle";

        // Regular expressions
        public const string CAMERA_FX_REGEX = @"(zoom_(in|out)_(quick|slow)_(small|large)|pulse[1-5]) [0-9]+";

        // GH5+ file definitions
        public const string NOTE = "note";
        public const int NOTE_ID = 0x40C001A3;
        public const string PERF = "perf";
        public const int PERF_ID = 0x40A001A3;
        public const string DRUMLOOPS_ANIMS = "L_DRUM_Loops_Standard_anims";
        public const string DRUMLOOPS_ANIMS_SET = "l_drum_loops_standard_anims_set";

        public static Dictionary<MidiTheory.NoteName, int> Gh3Notes = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.B, 0x80 }, // Open Notes for GH3 Plus
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
        public static Dictionary<string, string> trackNames = new Dictionary<string, string>
        {
            {GUITAR_NAME, PARTGUITAR},
            {RHYTHM_NAME, PARTBASS},
            {GUITARCOOP_NAME, PARTGUITARCOOP},
            {RHYTHMCOOP_NAME, PARTRHYTHM},
            {DRUMS_NAME, PARTDRUMS},
            {VOCALS_NAME, PARTVOCALS},
            {AUX_NAME, PARTAUX}
        };
        public static Dictionary<string, Dictionary<int, int>> leftHandMappingsGh3 = new Dictionary<string, Dictionary<int, int>>
        {
            {GUITAR_NAME, leftHandGtr_gh3},
            {GUITARCOOP_NAME, leftHandGtr_gh3},
            {RHYTHM_NAME, leftHandBass_gh3},
            {RHYTHMCOOP_NAME, leftHandBass_gh3},
            {AUX_NAME, leftHandRhythm}
        };

        public static Dictionary <string, string> ActorNameFromTrack = new Dictionary<string, string>
        {
            {GUITAR_NAME, GUITARIST},
            {RHYTHM_NAME, BASSIST},
            {RHYTHMCOOP_NAME, BASSIST},
            {DRUMS_NAME, DRUMMER},
            {VOCALS_NAME, VOCALIST},
            {AUX_NAME, RHYTHM}
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
            {APPLAUSE_OVERRIDE, 72 },
            {SURGE_FAST, 76},
            {SURGE_FAST_OVERRIDE, 73 },
            {SURGE_SLOW, 77},
            {SURGE_SLOW_OVERRIDE, 74},
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
            {APPLAUSE_OVERRIDE, 83 },
            {SURGE_FAST, 87},
            {SURGE_FAST_OVERRIDE, 87 },
            {SURGE_SLOW, 89},
            {SURGE_SLOW_OVERRIDE, 89},
            {MUSIC_END, 84},
            {MUSIC_START, 86},
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

        public static Dictionary<int, int> gh3_to_gha_cameras = new Dictionary<int, int>()
        {
            {77, 0},
            {78, 1},
            {79, 56},
            {80, 82},
            {81, 39},
            {82, 44},
            {83, 59},
            {84, 35},
            {85, 36},
            {86, 35},
            {87, 47},
            {88, 29},
            {89, 22},
            {90, 40},
            {91, 44},
            {92, 19},
            {93, 34},
            {94, 51},
            {95, 38},
            {96, 35},
            {97, 15},
            {98, 56},
            {99, 31},
            {100, 17},
            {101, 42},
            {102, 69},
            {103, 26},
            {104, 28},
            {105, 72},
            {106, 72},
            {107, 73},
            {108, 74},
            {109, 75},
            {110, 46},
            {111, 60},
            {112, 59},
            {113, 61},
            {114, 64},
            {115, 85},
            {116, 84},
            {117, 57}
        };

        public static Dictionary<int, int> gh3_to_wt_cameras = new Dictionary<int, int>()
        {
            {77,0},
            {78,1},
            {79,38},
            {80,31},
            {81,9},
            {82,66},
            {83,45},
            {84,30},
            {85,29},
            {86,10},
            {87,31},
            {88,15},
            {89,25},
            {90,33},
            {91,31},
            {92,23},
            {93,74},
            {94,75},
            {95,8},
            {96,38},
            {97,20},
            {98,9},
            {99,63},
            {100,64},
            {101,57},
            {102,58},
            {103,74},
            {104,13},
            {105,67},
            {106,66},
            {107,43},
            {108,48},
            {109,68},
            {110,28},
            {111,45},
            {112,45},
            {113,46},
            {114,69},
            {115,18},
            {116,8},
            {117,43}
        };

        public static Dictionary<int, int> gha_to_gh3_cameras = new Dictionary<int, int>()
        {
            // Enable Changes
            {0, 77},
            {1, 78},
            // Rhythm Cameras
            {3, 79},
            {4, 79},
            {5, 79},
            {6, 79},
            {7, 84},
            {8, 86},
            // Bassist Cameras
            {10, 97},
            {11, 97},
            {12, 97},
            {13, 97},
            {14, 84},
            {15, 86},
            // Drummer Cameras
            {17, 100},
            {18, 100},
            {19, 92},
            {20, 92},
            {21, 89},
            {22, 89},
            // Singer Cameras
            {24, 101},
            {25, 101},
            {26, 103},
            {27, 103},
            {28, 104},
            {29, 88},
            // Guitarist Cameras
            {31, 99},
            {32, 99},
            {33, 93},
            {34, 94},
            {35, 95},
            {36, 85},
            // Guitarist Special
            {38, 95},
            {39, 81},
            {40, 81},
            // Stage Cameras
            {42, 101},
            {43, 102},
            {44, 91},
            {45, 82},
            {46, 87},
            {47, 110},
            // Mid Cameras
            {49, 85},
            {50, 85},
            {51, 94},
            {52, 94},
            {53, 85},
            {54, 85},
            // Longshot Cameras
            {56, 91},
            {57, 117},
            // Zoom Cameras
            {59, 112},
            {60, 111},
            {61, 113},
            {62, 113},
            // Pan Cameras
            {64, 114},
            {65, 114},
            // Dolly Cameras
            {67, 101},
            {68, 101},
            {69, 102},
            {70, 102},
            // Special Cameras
            {72, 106},
            {73, 107},
            {74, 108},
            {75, 109},
            // Mocap Cameras
            {77, 106},
            {78, 107},
            {79, 108},
            {80, 109},
            // Audience Cameras
            {82, 80},
            // Boss Battle Cameras
            {84, 116},
            {85, 115},
            // Singer Closeups
            {87, 104},
            {88, 104},
            {89, 104},
            // Stagediver
            {91, 117},
        };

        public static Dictionary<int, int> gha_to_wt_cameras = new Dictionary<int, int>()
        {
            {0,0},
            {1,1},

            {3,31},
            {4,30},
            {5,29},
            {6,28},
            {7,67},
            {8,66},

            {10,65},
            {11,65},
            {12,75},
            {13,74},
            {14,18},
            {15,19},

            {17,64},
            {18,64},
            {19,75},
            {20,74},
            {21,23},
            {22,24},

            {24,62},
            {25,62},
            {26,75},
            {27,74},
            {28,13},
            {29,14},

            {31,63},
            {32,63},
            {33,75},
            {34,74},
            {35,8},
            {36,9},

            {38,33},
            {39,33},
            {40,33},

            {42,58},
            {43,57},
            {44,30},
            {45,29},
            {46,28},
            {47,48},

            {49,24},
            {50,19},
            {51,14},
            {52,9},
            {53,30},
            {54,29},

            {56,2},
            {57,28},

            {59,45},
            {60,45},
            {61,46},
            {62,46},

            {64,66},
            {65,67},

            {67,75},
            {68,74},
            {69,75},
            {70,74},

            {72,25},
            {73,20},
            {74,15},
            {75,10},

            {77,25},
            {78,20},
            {79,15},
            {80,10},

            {82,43},

            {84,18},
            {85,8},

            {87,20},
            {88,18},
            {89,18},

            {91,43}

        };

        public static Dictionary<int, int> wt_to_gh3_cameras = new Dictionary<int, int>()
        {
            {75,94},
            {74,82},
            {69,84},
            {68,89},
            {67,114},
            {66,114},
            {65,101},
            {64,100},
            {63,99},
            {62,102},
            {61,87},
            {60,95},
            {59,97},
            {58,102},
            {57,101},
            {48,110},
            {46,113},
            {45,83},
            {43,84},
            {38,79},
            {36,97},
            {35,92},
            {34,104},
            {33,90},
            {31,87},
            {30,110},
            {29,85},
            {28,91},
            {26,100},
            {25,89},
            {24,100},
            {23,89},
            {21,87},
            {20,85},
            {19,110},
            {18,97},
            {16,103},
            {15,88},
            {14,101},
            {13,104},
            {11,85},
            {10,84},
            {9,87},
            {8,95},
        };

        public static Dictionary<int, int> wt_to_gha_cameras = new Dictionary<int, int>()
        {
            {75,44},
            {74,45},
            {69,64},
            {68,22},
            {67,64},
            {66,65},
            {65,11},
            {64,18},
            {63,32},
            {62,25},
            {61,64},
            {60,65},
            {59,45},
            {58,49},
            {57,50},
            {48,47},
            {46,59},
            {45,60},
            {43,82},
            {38,46},
            {36,14},
            {35,21},
            {34,28},
            {33,38},
            {31,46},
            {30,53},
            {29,54},
            {28,56},
            {26,19},
            {25,20},
            {24,22},
            {23,21},
            {21,12},
            {20,13},
            {19,15},
            {18,14},
            {16,26},
            {15,27},
            {14,29},
            {13,28},
            {11,33},
            {10,34},
            {9,36},
            {8,35}
        };

        public static Dictionary<int, int> wt_to_gh3_lights = new Dictionary<int, int>()
        {
            {107,107},
            {84,76},
            {83,72},
            {82,71},
            {81,70},
            {80,75},
            {79,75},
            {78,74},
            {77,73},
            {76,73},
            {75,74},
            {74,73},
            {73,74},
            {72,71},
            {71,73},
            {60,60},
            {58,58},
            {57,57},
            {53,53},
            {52,52},
            {51,51},
            {50,50},
            {49,49},
            {48,48},
            {47,47},
            {46,46},
            {45,45},
            {44,44},
            {43,43},
            {42,42},
            {41,41},
            {40,40},
            {39,39},
            {37,56},
            {35,56},
            {34,56},
            {33,56},
            {32,56},
            {31,56},
            {30,56},
            {29,56},
            {28,56},
            {27,56},
            {26,56},
            {25,56},
            {24,56},
            {23,56},
            {22,56},
            {21,56},
            {20,56},
            {19,56}
        };

        public static Dictionary<int, int> gh3_to_wt_lights = new Dictionary<int, int>()
        {
            {107,107},
            {39,39},
            {40,40},
            {41,41},
            {42,42},
            {43,43},
            {44,44},
            {45,45},
            {46,46},
            {47,47},
            {48,48},
            {49,49},
            {50,50},
            {51,51},
            {52,52},
            {53,53},
            {56,37},
            {57,57},
            {58,58},
            {60,58},
            {70,81},
            {71,82},
            {72,83},
            {73,75},
            {74,76},
            {75,78},
            {76,84}
        };

        public static Dictionary<string, Dictionary<int, int>> cameraToGh3 = new Dictionary<string, Dictionary<int, int>>()
        {
            {GAME_GHA, gha_to_gh3_cameras},
            {GAME_GHWT, wt_to_gh3_cameras}
        };

        public static Dictionary<string, Dictionary<int, int>> cameraToGha = new Dictionary<string, Dictionary<int, int>>()
        {
            {GAME_GH3, gh3_to_gha_cameras},
            {GAME_GHWT, wt_to_gha_cameras}
        };
        public static Dictionary<string, Dictionary<int, int>> cameraToGhwt = new Dictionary<string, Dictionary<int, int>>()
        {
            {GAME_GH3, gh3_to_wt_cameras},
            {GAME_GHA, gha_to_wt_cameras}
        };

        public static Dictionary<string, Dictionary<int, int>> lightSwapDict = new Dictionary<string, Dictionary<int, int>>()
        {
            {GAME_GH3, wt_to_gh3_lights},
            {GAME_GHWT, gh3_to_wt_lights}
        };

        public static Dictionary<int, int> momentCams = new Dictionary<int, int>()
        {
            {3,10},
            {4,15},
            {5,20},
            {6,25},
            {33,10},
            {34,15},
            {35,20},
            {36,25},
            {90,28},
            {91,29},
            {92,30},
            {93,31},
            {94,10},
            {95,15},
            {96,20},
            {97,74},
            {98,75},
            {99,75}
        };


        public static Dictionary<string, InstrumentAnim> DefaultStructsFemale = new Dictionary<string, InstrumentAnim>
        {
            { "guitar", new InstrumentAnim
                {
                    Pak = "L_GUIT_Judita_Joker_anims",
                    AnimSet = "L_GUIT_Judita_Joker_anims_set",
                    FingerAnims = "guitarist_finger_anims_car_female",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Female_Normal",
                    FacialAnims = "facial_anims_female_rocker",
                    Instrument = "guitar"
                }
            },
            { "bass", new InstrumentAnim("bass")
                {
                    Pak = "L_GUIT_Ginger_Joker_anims",
                    AnimSet = "L_GUIT_Ginger_Joker_anims_set",
                    FingerAnims = "guitarist_finger_anims_car_female",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Female_Normal",
                    FacialAnims = "facial_anims_female_rocker",
                    Instrument = "bass"
                }
            },
            { "drum", new InstrumentAnim("drum")
                {
                    Pak = "L_GUIT_Jeffs_LowKey_anims",
                    AnimSet = "L_GUIT_Jeffs_LowKey_anims_set",
                    FingerAnims = "guitarist_finger_anims_car_female",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Female_Normal",
                    FacialAnims = "facial_anims_female_rocker",
                    Instrument = "drum"
                }
            },
            { "vocals", new InstrumentAnim("vocals")
                {
                    Pak = "L_SING_Rockg_Joker_anims",
                    AnimSet = "L_SING_Rockg_Joker_anims_set",
                    FingerAnims = "guitarist_finger_anims_car_female",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Female_Normal",
                    FacialAnims = "facial_anims_female_rocker",
                    Instrument = "vocals"
                }
            }
        };
        public static Dictionary<string, InstrumentAnim> DefaultStructsMale = new Dictionary<string, InstrumentAnim>
        {
            { "guitar", new InstrumentAnim
                {
                    Pak = "L_GUIT_Dan_Joker_anims",
                    AnimSet = "L_GUIT_Dan_Joker_anims_set",
                    FingerAnims = "guitarist_finger_anims_CAR_Male",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Male_Normal",
                    FacialAnims = "facial_anims_male_rocker",
                    Instrument = "guitar"
                }
            },
            { "bass", new InstrumentAnim
                {
                    Pak = "L_GUIT_ChrisVance_Joker_anims",
                    AnimSet = "L_GUIT_ChrisVance_Joker_anims_set",
                    FingerAnims = "guitarist_finger_anims_CAR_Male",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Male_Normal",
                    FacialAnims = "facial_anims_male_rocker",
                    Instrument = "bass"
                }
            },
            { "drum", new InstrumentAnim
                {
                    Pak = "L_GUIT_Jeffs_LowKey_anims",
                    AnimSet = "L_GUIT_Jeffs_LowKey_anims_set",
                    FingerAnims = "guitarist_finger_anims_CAR_Male",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Male_Normal",
                    FacialAnims = "facial_anims_male_rocker",
                    Instrument = "drum"
                }
            },
            { "vocals", new InstrumentAnim
                {
                    Pak = "L_SING_Rockg_Joker_anims",
                    AnimSet = "L_SING_Rockg_Joker_anims_set",
                    FingerAnims = "guitarist_finger_anims_CAR_Male",
                    FretAnims = "fret_anims_rocker",
                    StrumAnims = "CAR_Male_Normal",
                    FacialAnims = "facial_anims_male_rocker",
                    Instrument = "vocals"
                }
            }
        };
        public static Dictionary<string, Dictionary<string, InstrumentAnim>> DefaultStructs = new Dictionary<string, Dictionary<string, InstrumentAnim>>
        {
            {"female", DefaultStructsFemale},
            {"male", DefaultStructsMale}
        };
        public static List<string> legalLoops = new List<string>()
        {
            "l_guit_chrisvance_bulls",
            "l_guit_chrisv_bulls_f",
            "l_guit_chrisvance_damnit",
            "l_guit_chrisv_damnit_f",
            "l_guit_chrisv_damnit2",
            "l_guit_chrisv_damnit2_f",
            "l_guit_chrisvance_joker",
            "l_guit_chrisv_joker_f",
            "l_guit_davidicus_damnit",
            "l_guit_davdics_damnit_f",
            "l_guit_davidicus_damnit2",
            "l_guit_davdics_damnit2_f",
            "l_guit_davidicus_joker",
            "l_guit_davdics_joker_f",
            "l_guit_davidicus_bulls",
            "l_guit_davdics_bulls_f",
            "l_guit_dan_damnit",
            "l_guit_dan_damnit_f",
            "l_guit_dan_joker",
            "l_guit_dan_joker_f",
            "l_guit_joer_areyou",
            "l_guit_joer_areyou_f",
            "l_guit_joer_hot",
            "l_guit_joer_hot_f",
            "l_guit_jeffs_lowkey",
            "l_guit_jeffs_lowkey_f",
            "l_guit_jeffs_lowkey2",
            "l_guit_jeffs_lowkey2_f",
            "l_guit_joes_areyou",
            "l_guit_joes_areyou_f",
            "l_guit_joes_hot",
            "l_guit_joes_hot_f",
            "l_guit_matt_stillborn",
            "l_guit_matt_stillborn_f",
            "l_guit_matt_bulls",
            "l_guit_matt_bulls_f",
            "l_guit_sam_joker",
            "l_guit_sam_joker_f",
            "l_guit_sam_dammit",
            "l_guit_sam_dammit_f",
            "l_guit_sonny_bulls",
            "l_guit_sonny_bulls_f",
            "l_guit_sonny_stillborn",
            "l_guit_sonny_stillborn_f",
            "l_guit_ginger_areyou",
            "l_guit_ginger_areyou_m",
            "l_guit_ginger_bulls",
            "l_guit_ginger_bulls_m",
            "l_guit_ginger_joker",
            "l_guit_ginger_joker_m",
            "l_guit_judita_areyou",
            "l_guit_judita_areyou_m",
            "l_guit_judita_bulls",
            "l_guit_judita_bulls_m",
            "l_guit_judita_joker",
            "l_guit_judita_joker_m",
            "l_guit_morgan_4horsemen",
            "l_guit_morgan_4horse_m",
            "l_guit_tara_bleedme_f",
            "l_guit_tara_bleedme",
            "l_guit_morgan_bleedingme",
            "l_guit_morgan_bleedme_m",
            "l_guit_chris_stillborn",
            "l_guit_chris_stillborn_f",
            "l_guit_chris_strnglhld",
            "l_guit_chris_strnglhld_f",
            "l_guit_scott_stillborn",
            "l_guit_scott_stillborn_f",
            "l_guit_scott_strnglhld",
            "l_guit_scott_strnglhld_f",
            "l_guit_tara_4horsemen_f",
            "l_guit_tara_4horsemen",
            "l_guit_adam_lowkey",
            "l_guit_adam_lowkey_f",
            "l_guit_tara_notplaying_f",
            "l_guit_tara_notplaying",
            "l_guit_ivy_bleedme_f",
            "l_guit_ivy_bleedme",
            "l_guit_ivy_4horsemen_f",
            "l_guit_ivy_4horsemen",
            "l_sing_jeffs_lowkey",
            "l_sing_adam_hounddog_f",
            "l_sing_patrick_joker_f",
            "l_sing_patrick_areyou_f",
            "l_sing_jason_walk_f",
            "l_sing_jason_walk_mic_f",
            "l_sing_adam_hounddog",
            "l_sing_patrick_joker",
            "l_sing_patrick_areyou",
            "l_sing_jason_areyou",
            "l_sing_jason_areyou_f",
            "l_sing_jason_walk",
            "l_sing_jason_walk_mic",
            "l_sing_jeffs_lowkey_f",
            "l_sing_jeffs_lowkey2",
            "l_sing_jeffs_lowkey2_f",
            "l_sing_joeh_hot",
            "l_sing_joeh_hot_f",
            "l_sing_josh_dammit",
            "l_sing_josh_dammit_f",
            "l_sing_josh_joker",
            "l_sing_josh_joker_f",
            "l_sing_rob_bulls",
            "l_sing_rob_bulls_f",
            "l_sing_rob_stillborn",
            "l_sing_rob_stillborn_f",
            "l_sing_rockg_damnit",
            "l_sing_rockg_damnit_f",
            "l_sing_rockg_damnit2",
            "l_sing_rockg_damnit2_f",
            "l_sing_rockg_joker",
            "l_sing_rockg_joker_f",
            "l_sing_rockg_bulls",
            "l_sing_rockg_bulls_f",
            "l_sing_joeh_areyou",
            "l_sing_joeh_areyou_f",
            "l_sing_natalie_areyou",
            "l_sing_natalie_areyou_m",
            "l_sing_morgan_4horsens_m",
            "l_sing_morgan_4horse_m",
            "l_sing_morgan_4horse",
            "l_sing_morgan_4horsens",
            "l_sing_morgan_bleedingns",
            "l_sing_morgan_bleedns_m",
            "l_sing_randy_stillborn",
            "l_sing_randy_stillborn_f",
            "l_sing_randy_strangle_f",
            "l_sing_randy_strangle",
            "l_sing_amanda_areyou",
            "l_sing_amanda_areyou_m",
            "l_sing_amanda_today",
            "l_sing_amanda_today_m",
            "l_sing_amanda_bulls",
            "l_sing_amanda_bulls_m",
            "l_sing_amanda_dammit",
            "l_sing_patrick_walk",
            "l_sing_patrick_walk_f",
            "l_sing_amanda_joker",
            "l_sing_amanda_joker2",
            "l_sing_amanda_nonsing",
            "l_sing_amanda_nonsing2",
            "l_sing_patrick_hot",
            "l_sing_patrick_hot_f",
            "l_sing_amanda_joker_m",
            "l_sing_amanda_joker2_m",
            "l_sing_amanda_nonsing_m",
            "l_sing_amanda_nonsing2_m",
            "l_sing_amanda_dammit_m",
            "l_sing_jason_joker",
            "l_sing_jason_joker_f",
            "l_sing_debbie_dammit",
            "l_sing_debbie_dammit_m",
            "l_sing_adam_joker",
            "l_sing_adam_joker_f",
            "l_sing_adam_dammit",
            "l_sing_adam_dammit_f",
            "l_sing_adam_nsjoker",
            "l_sing_adam_nsjoker_f",
            "l_sing_adam_walk",
            "l_sing_patrick_bulls",
            "l_sing_adam_walk_f",
            "l_sing_patrick_bulls_f",
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
            return Math.Abs(n1.Time - n2.Time) < tickThreshold;
        }

    }
}
