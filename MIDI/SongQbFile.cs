using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Diagnostics;
using System.Numerics;
using static GH_Toolkit_Core.MIDI.MidiDefs;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.QB.QBStruct;
using static GH_Toolkit_Core.Checksum.CRC;
using static GH_Toolkit_Core.Methods.ReadWrite;
using static GH_Toolkit_Core.Methods.Exceptions;

using MidiTheory = Melanchall.DryWetMidi.MusicTheory;
using MidiData = Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using static GH_Toolkit_Core.MIDI.SongQbFile;
using static GH_Toolkit_Core.MIDI.AnimStruct;
using static GH_Toolkit_Core.MIDI.SongClip;
using static GH_Toolkit_Core.MIDI.GH5Note;
using GH_Toolkit_Core.Debug;
using System.Collections.Specialized;
using System.Text;
using GH_Toolkit_Core.Checksum;
using System.Security.Policy;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static GH_Toolkit_Core.MIDI.SongQbFile.CameraGenerator;
using GH_Toolkit_Core.Methods;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using GH_Toolkit_Core.QB;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using static GH_Toolkit_Core.PAK.PAK;
using Microsoft.VisualBasic.FileIO;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;
using static System.Formats.Asn1.AsnWriter;


/*
 * This file contains all the logic for creating a QB file from a MIDI file
 *
 *
 *
 */

namespace GH_Toolkit_Core.MIDI
{
    public class SongQbFile
    {
        private const string EMPTYSTRING = "";
        private const string SECTION_OLD = "section ";
        private const string SECTION_NEW = "prc_";
        private const string CROWD_CHECK = "crowd";
        private const string SECTION_EVENT = "section";
        private const string CROWD_EVENT = "crowd_";

        private const string SCR = "scr";
        private const string TIME = "time";

        private const string EASY = "easy";
        private const string MEDIUM = "medium";
        private const string HARD = "hard";
        private const string EXPERT = "expert";

        private const string STAR = "Star";
        private const string STAR_BM = "StarBattleMode";
        private const string FACEOFFSTAR = "FaceOffStar";
        private const string TAP = "Tapping";
        private const string WHAMMYCONTROLLER = "WhammyController";

        private const string LYRIC = "lyric";

        private const int AccentVelocity = 127;
        private const int GhostVelocity = 1;
        private const int AllAccents = 0b11111;
        private const int DefaultTPB = 480;
        private const int DefaultTickThreshold = 15;


        public List<int> Fretbars = new List<int>();
        public List<TimeSig> TimeSigs = new List<TimeSig>();
        public List<(int Measure, int Beat)> Measures = new List<(int, int)>();
        public Instrument Guitar { get; set; } = new Instrument(GUITAR_NAME);
        public Instrument Rhythm { get; set; } = new Instrument(RHYTHM_NAME);
        public Instrument Drums { get; set; } = new Instrument(DRUMS_NAME);
        public Instrument Aux { get; set; } = new Instrument(AUX_NAME);
        public Instrument GuitarCoop { get; set; } = new Instrument(GUITARCOOP_NAME);
        public Instrument RhythmCoop { get; set; } = new Instrument(RHYTHMCOOP_NAME);
        public VocalsInstrument Vocals { get; set; } = new VocalsInstrument();
        public List<AnimNote>? ScriptNotes { get; set; }
        public List<AnimNote>? AnimNotes { get; set; }
        public List<AnimNote>? TriggersNotes { get; set; }
        public List<AnimNote>? CamerasNotes { get; set; }
        public List<AnimNote>? LightshowNotes { get; set; }
        public List<AnimNote>? CrowdNotes { get; set; }
        public List<AnimNote>? DrumsNotes { get; set; }
        public List<AnimNote>? PerformanceNotes { get; set; }
        public QBArrayNode? ScriptScripts { get; set; }
        public QBArrayNode? AnimScripts { get; set; }
        public QBArrayNode? TriggersScripts { get; set; }
        public QBArrayNode? CamerasScripts { get; set; }
        public List<(int, QBStructData)> CameraTimedScripts { get; set; } = new List<(int, QBStructData)>();
        public QBArrayNode? LightshowScripts { get; set; }
        public QBArrayNode? CrowdScripts { get; set; }
        public List<(int, QBStructData)> CrowdTimedScripts { get; set; } = new List<(int, QBStructData)>();
        public QBArrayNode? DrumsScripts { get; set; }
        public QBArrayNode? PerformanceScripts { get; set; }
        public QBArrayNode? FacialScripts { get; set; }
        public List<(int, QBStructData)> FacialTimedScripts { get; set; } = new List<(int, QBStructData)>();
        public QBArrayNode? LocalizedStrings { get; set; }
        public QBArrayNode? ScriptEvents { get; set; }
        public Dictionary<string, SongClip> SongClips { get; set; } = new Dictionary<string, SongClip>();
        public Dictionary<string, AnimStruct> AnimStructs { get; set; } = new Dictionary<string, AnimStruct>();
        public List<QBItem> SongScripts { get; set; } = new List<QBItem>(); // Actual scripts found in songs. Very rare.
        private Dictionary<string, QBItem>? SongSections { get; set; }
        public List<(int, QBStructData)> ScriptTimedEvents { get; set; } = new List<(int, QBStructData)>();
        public Dictionary<string, Dictionary<string, List<string>>> Gh6Loops { get; set; } = new Dictionary<string, Dictionary<string, List<string>>>()
        {
            {"male",  new Dictionary<string, List<string>>()
                {
                    {"guitarist", new List<string>()},
                    {"bassist", new List<string>()},
                    {"vocalist", new List<string>()}
                }
            },
            {"female",  new Dictionary<string, List<string>>()
                {
                    {"guitarist", new List<string>()},
                    {"bassist", new List<string>()},
                    {"vocalist", new List<string>()}
                }
            }

        };
        public QBItem PerfScriptEvents { get; set; }
        public List<Marker>? Markers { get; set; }
        public List<int> BandMoments { get; set; } = new List<int>();
        internal MidiFile SongMidiFile { get; set; }
        internal TempoMap SongTempoMap { get; set; }
        private int TPB { get; set; }
        private int HopoThreshold { get; set; }
        private long LastEventTick { get; set; }
        private bool HasCameras { get; set; } = false;
        private bool HasLights { get; set; } = false;
        private bool HasDrumAnims { get; set; } = false;
        private bool NoAux { get; set; } = true;
        public bool DoubleKick { get; set; } = false;
        public bool EasyOpens { get; set; } = false;
        public string SkaPath { get; set; } = "";
        public string Endian { get; set; } = "big";
        public static string? PerfOverride { get; set; }
        public static string? SongScriptOverride { get; set; }
        public static string? VenueSource { get; set; }
        public static bool RhythmTrack { get; set; }
        public static string? Game { get; set; }
        public List<string> GtrSkaAnims { get; set; } = new List<string>();
        public List<string> BassSkaAnims { get; set; } = new List<string>();
        public List<string> VoxSkaAnims { get; set; } = new List<string>();
        public List<string> DrumSkaAnims { get; set; } = new List<string>();
        private string? QfileGame { get; set; } = GAME_GHWT;
        private static string? _songName;
        public string? SongName
        {
            get
            {
                return _songName;
            }
            set
            {
                _songName = value.ToLower();
            }
        }
        public static string? GamePlatform { get; set; }
        public static bool OverrideBeat { get; set; }
        public static HopoType HopoMethod { get; set; }
        public Dictionary<string, string> QsList { get; set; } = new Dictionary<string, string>();
        internal Dictionary<string, string> SkaQbKeys { get; set; } = new Dictionary<string, string>();
        private List<string> ErrorList { get; set; } = new List<string>();
        private List<string> WarningList { get; set; } = new List<string>();
        public static ReadWrite _readWriteGh5 = new ReadWrite("big");
        private bool FromChart = false;
        private bool Gh3Plus = false;
        private bool MidiParsed = false;
        private bool WtExpertPlusPak = false;
        public SongQbFile(string midiPath, string songName, string game = GAME_GH3, string console = CONSOLE_XBOX, int hopoThreshold = 170, string perfOverride = "", string songScriptOverride = "", string venueSource = "", bool rhythmTrack = false, bool overrideBeat = false, HopoType hopoType = 0, bool easyOpens = false, string skaPath = "", bool fromChart = false, bool gh3Plus = false)
        {
            Game = game;
            SongName = songName;
            GamePlatform = console;
            HopoThreshold = hopoThreshold;
            PerfOverride = perfOverride;
            SongScriptOverride = songScriptOverride;
            VenueSource = venueSource == "" ? Game : venueSource;
            RhythmTrack = rhythmTrack;
            OverrideBeat = overrideBeat;
            EasyOpens = easyOpens;
            SkaPath = skaPath;
            SetSkaQbKeys();
            HopoMethod = hopoType;
            FromChart = fromChart;
            Gh3Plus = gh3Plus;

            if (Gh3Plus)
            {
                Console.WriteLine("Using GH3+ parsing method");
            }

            SetMidiInfo(midiPath);
        }
        public SongQbFile(string songName,
            byte[]? midQb,
            Dictionary<uint, string>? midQs,
            byte[]? songScripts,
            byte[]? notes,
            byte[]? perf,
            byte[]? perfXml,
            string endian = "big")
        {
            if (songName.ToLower().Contains("_perf2"))
            {
                songName = songName.Substring(0, songName.ToLower().IndexOf("_perf2"));
            }
            SongName = songName;
            Endian = endian;
            ParseQbToData(midQb, midQs, songScripts, notes, perf, perfXml);
        }

        public SongQbFile(string songName,
            string qPath,
            string? songScripts,
            string game = GAME_GH3,
            string console = CONSOLE_XBOX)
        {
            SongName = songName;
            Game = game;
            GamePlatform = console;
            GetSongSections();
            var isGH5 = false;
            var midName = Path.GetFileNameWithoutExtension(qPath);
            var midName2 = Path.GetFileNameWithoutExtension(midName);
            var (qbList, _) = ParseQFile(qPath);
            if (!string.IsNullOrEmpty(midName2) && midName2 != SongName)
            {
                foreach (QBItem qbItem in qbList)
                {
                    qbItem.Name = qbItem.Name.Replace(midName2, SongName);
                }
            }
            var qbDict = QbEntryDict(qbList);
            DetermineQType(qbDict, out isGH5);
            ParseTimeSigsAndFretbarsFromQb(qbDict);
            ParseInstrumentsFromQb(qbDict, null);
            ParseMarkersFromQ(qbDict);
            ParseNonInstrumentFromQ(qbDict);
        }
        private void GetSongSections()
        {
            var exeLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var songSections = Path.Combine(exeLocation, "Resources", "Sections", "all_sections.q");
            if (File.Exists(songSections))
            {
                var (tempSections, _) = ParseQFile(songSections); 
                SongSections = QbEntryDict(tempSections);
            }
        }
        private void DetermineQType(Dictionary<string, QBItem> qbDict, out bool isGH5)
        {
            isGH5 = false;
            if (!qbDict.ContainsKey($"{SongName}_song_easy"))
            {
                QfileGame = GAME_GH5;
                isGH5 = true;
            }
            else if (!qbDict.ContainsKey($"{SongName}_song_drum_easy"))
            {
                if (qbDict.ContainsKey($"{SongName}_song_aux"))
                {
                    QfileGame = GAME_GHA;
                }
                else
                {
                    QfileGame = GAME_GH3;
                }
            }
            else
            {
                QfileGame = GAME_GHWT;
            }
        }

        public bool HasGh6Loops()
        {
            var male = Gh6Loops["male"]["vocalist"].Count > 0 || Gh6Loops["male"]["guitarist"].Count > 0 || Gh6Loops["male"]["bassist"].Count > 0;
            var female = Gh6Loops["female"]["vocalist"].Count > 0 || Gh6Loops["female"]["guitarist"].Count > 0 || Gh6Loops["female"]["bassist"].Count > 0;
            return male && female;
        }
        public string GetGame()
        {
            return Game!;
        }
        public string GetConsole()
        {
            return GamePlatform!;
        }
        public void SetConsole(string console)
        {
            GamePlatform = console;
        }
        public void AddToErrorList(string error)
        {
            ErrorList.Add(error);
        }
        public void AddToWarningList(string warning)
        {
            WarningList.Add(warning);
        }
        public string GetErrorListAsString()
        {
            return string.Join("\r\n", ErrorList);
        }
        public string GetWarningListAsString()
        {
            return string.Join("\r\n", WarningList);
        }
        private string GetMeasureBeatTick(double errorMs)
        {
            var fretbarTime = GetClosestIntFromList((int)Math.Round(errorMs), Fretbars);
            var fretbarIndex = Fretbars.IndexOf(fretbarTime);
            if (fretbarTime > errorMs)
            {
                fretbarIndex--;
                fretbarTime = Fretbars[fretbarIndex]; // Always approach from below the error time.
            }
            var nextFretbarTime = fretbarIndex + 1 < Fretbars.Count ? Fretbars[fretbarIndex + 1] : fretbarTime + 1000; // If no next fretbar, assume 1 second after current.
            var measureInSong = Measures[fretbarIndex];
            var timeFromBeatInTicks = ((errorMs - fretbarTime) / (nextFretbarTime - fretbarTime)) * 100;
            return $"{measureInSong.Measure}.{measureInSong.Beat}.{(int)Math.Round(timeFromBeatInTicks)}";
        }
        private string GetMeasureBeatTick(int errorMs)
        {
            var fretbarTime = GetClosestIntFromList(errorMs, Fretbars);
            var fretbarIndex = Fretbars.IndexOf(fretbarTime);
            if (fretbarTime > errorMs)
            {
                fretbarIndex--;
                fretbarTime = Fretbars[fretbarIndex]; // Always approach from below the error time.
            }
            var nextFretbarTime = fretbarIndex + 1 < Fretbars.Count ? Fretbars[fretbarIndex + 1] : fretbarTime + 1000; // If no next fretbar, assume 1 second after current.
            var measureInSong = Measures[fretbarIndex];
            var timeFromBeatInTicks = ((errorMs - fretbarTime) / (nextFretbarTime - fretbarTime)) * 100.0;
            return $"{measureInSong.Measure}.{measureInSong.Beat}.{(int)Math.Round(timeFromBeatInTicks)}";
        }
        public void AddTimedError(string error, string part, long ticks)
        {
            var errorMs = TicksToMilliseconds(ticks);
            var mbt = GetMeasureBeatTick(errorMs);
            AddToErrorList($"{part}: {error} found at MBT {mbt} ({Math.Round(errorMs / 1000f, 3)}s)");
        }
        public void AddTimedError(string error, string part, int eventTime)
        {
            var mbt = GetMeasureBeatTick(eventTime);
            AddToErrorList($"{part}: {error} found at MBT {mbt} ({Math.Round(eventTime / 1000f, 3)}s)");
        }
        public void AddTimedWarning(string warning, string part, long ticks)
        {
            var errorMs = TicksToMilliseconds(ticks);
            var mbt = GetMeasureBeatTick(errorMs);
            AddToWarningList($"{part}: {warning} found at MBT {mbt} ({Math.Round(errorMs / 1000f, 3) / 1000f}s)");
        }
        public void AddTimedWarning(string warning, string part, int eventTime)
        {
            var mbt = GetMeasureBeatTick(eventTime);
            AddToWarningList($"{part}: {warning} found at MBT {mbt} ({Math.Round(eventTime / 1000f, 3) / 1000f}s)");
        }
        private void WriteUsedTrack(string trackName)
        {
            Console.WriteLine($"Processing track: {trackName}");
        }
        private void SkipTrack(string trackName)
        {
            Console.WriteLine($"Skipping track: {trackName}");
        }
        private void SetSkaQbKeys()
        {
            if (!Directory.Exists(SkaPath))
            {
                return;
            }
            foreach (string ska in Directory.GetFiles(SkaPath))
            {
                string file = Path.GetFileName(ska);// .Substring(0, ska.IndexOf('.'))
                file = file.Substring(0, file.IndexOf('.'));
                var qbkey = QBKey(file);
                try
                {
                    SkaQbKeys.Add(qbkey, file);
                }
                catch (Exception ex)
                {
                    AddToErrorList($"Same ska file found under two names: {qbkey} & {DebugReader.DebugCheck(qbkey)}");
                }
            }
        }
        public void ParseMidi()
        {
            bool hasBass = false;
            bool hasRhythm = false;
            // Getting the tempo map to convert ticks to time
            SongTempoMap = SongMidiFile.GetTempoMap();
            var trackChunks = SongMidiFile.GetTrackChunks();
            GetTimeSigs(trackChunks.First());
            CalculateFretbars();
            ParseClipsAndAnims();
            List<AnimNote>? drumAnimOverride = null;
            List<AnimNote>? crowdOverride = null;

            foreach (var trackChunk in trackChunks.Skip(1))
            {
                string trackName = GetTrackName(trackChunk);
                switch (trackName)
                {
                    case PARTDRUMS:
                        WriteUsedTrack(trackName);
                        Drums.MakeInstrument(trackChunk, this, drums: true);
                        if (Drums.AnimNotes.Count > 0)
                        {
                            DrumsNotes = Drums.AnimNotes;
                            HasDrumAnims = true;
                        }
                        break;
                    case PARTBASS:
                        WriteUsedTrack(trackName);
                        Rhythm.MakeInstrument(trackChunk, this);
                        hasBass = true;
                        break;
                    case PARTGUITAR:
                        WriteUsedTrack(trackName);
                        Guitar.MakeInstrument(trackChunk, this);
                        break;
                    case PARTGUITARCOOP:
                        WriteUsedTrack(trackName);
                        GuitarCoop.MakeInstrument(trackChunk, this);
                        break;
                    case PARTRHYTHM:
                        WriteUsedTrack(trackName);
                        RhythmCoop.MakeInstrument(trackChunk, this);
                        hasRhythm = true;
                        break;
                    case PARTAUX:
                        WriteUsedTrack(trackName);
                        NoAux = false;
                        Aux.MakeInstrument(trackChunk, this);
                        break;
                    case PARTVOCALS:
                        WriteUsedTrack(trackName);
                        Vocals.MakeInstrument(trackChunk, this, LastEventTick);
                        break;
                    case CAMERAS:
                        WriteUsedTrack(trackName);
                        ProcessCameras(trackChunk);
                        HasCameras = true;
                        break;
                    case LIGHTSHOW:
                        WriteUsedTrack(trackName);
                        ProcessLights(trackChunk);
                        HasLights = true;
                        break;
                    case EVENTS:
                        WriteUsedTrack(trackName);
                        ProcessEvents(trackChunk);
                        break;
                    case DRUMS:
                        WriteUsedTrack(trackName);
                        drumAnimOverride = makeOverride(trackChunk);
                        break;
                    case CROWD:
                        WriteUsedTrack(trackName);
                        crowdOverride = makeOverride(trackChunk);
                        break;
                    case ANIMS:
                        WriteUsedTrack(trackName);
                        AnimNotes = makeOverride(trackChunk);
                        break;
                    case BEAT:
                        if (OverrideBeat)
                        {
                            Console.WriteLine("Overriding fretbars with BEAT track");
                            ProcessBeat(trackChunk);
                        }
                        else
                        {
                            SkipTrack(trackName);
                        }
                        break;
                    default:
                        SkipTrack(trackName);
                        break;
                }
            }
            if (!hasBass && hasRhythm)
            {
                
            }
            if (drumAnimOverride != null)
            {
                DrumsNotes = drumAnimOverride;
                HasDrumAnims = true;
            }
            if (crowdOverride != null)
            {
                CrowdNotes = crowdOverride;
            }
            if (CamerasNotes == null || CamerasNotes.Count == 0)
            {
                Console.WriteLine("No camera track found, generating cameras");
                var cameraGen = new CameraGenerator();
                // create a slice of Fretbars grabbing every 8th entry
                var cameraFretbars = Fretbars.Where((x, i) => i % 8 == 0).ToList();
                CamerasNotes = cameraGen.AutoGenCamera(cameraFretbars, Game, VenueSource);
            }
            if (LightshowNotes == null || LightshowNotes.Count == 0)
            {
                Console.WriteLine("No lightshow track found, generating lightshow");
                var lightGen = new LightShowGenerator();
                LightshowNotes = lightGen.AutoGenLightshow(Fretbars, Markers, (Game == GAME_GH3 || Game == GAME_GHA));
            }
            if (!HasDrumAnims && (Game != GAME_GH3 || Game != GAME_GHA))
            {
                Console.WriteLine("No drum animations found, generating drum animations");
                var drumGen = new DrumAnimGenerator();
                DrumsNotes = drumGen.AutoGenDrumAnims(Drums.Expert);
            }
            if ((Game == GAME_GH5 || Game == GAME_GHWOR) && BandMoments.Count == 0)
            {
                Console.WriteLine("No band moments found, generating band moments");
                BandMoments = GenerateBandMoments();
            }
            MidiParsed = true;
        }
        private List<int> GenerateBandMoments()
        {

            List<int> bandMoments = new List<int>();
            var chorusMoments = new List<int>();
            var allMoments = new List<int>();

            float lastMarkerTime;

            try
            {
                lastMarkerTime = Markers.Last().Time / 1000f;
            }
            catch (Exception)
            {
                lastMarkerTime = 0;
            }

            int bandMomentsCount;

            switch (lastMarkerTime)
            {
                case float n when (n < 60):
                    bandMomentsCount = 1;
                    break;
                case float n when (n < 180):
                    bandMomentsCount = 2;
                    break;
                case float n when (n < 300):
                    bandMomentsCount = 3;
                    break;
                default:
                    bandMomentsCount = 4;
                    break;
            }

            if (Markers.Count <= bandMomentsCount)
            {
                Console.WriteLine("Not enough section markers to generate band moments");
                return bandMoments;
            }

            string chorusRegex = @"^chorus.*[0-9]+[a-z]?$";
            string endSongRegex = @"endofsong";

            for (int i = 0; i < Markers.Count; i++)
            {
                var marker = Markers[i];

                if (Regex.IsMatch(marker.Text, endSongRegex, RegexOptions.IgnoreCase))
                {
                    break;
                }
                if (Regex.IsMatch(marker.Text, chorusRegex, RegexOptions.IgnoreCase))
                {
                    chorusMoments.Add(marker.Time);
                }
                allMoments.Add(marker.Time);
            }

            if (chorusMoments.Count < bandMomentsCount)
            {
                Console.WriteLine("Not enough chorus markers found, generating band moments from all markers");
                chorusMoments = allMoments;
            }

            GetFurthestIntegers(chorusMoments, bandMomentsCount);

            var closestTime = 4000;

            foreach (var moment in chorusMoments)
            {
                var bandMomentStart = GetClosestIntFromList(moment - closestTime, Fretbars);
                var bandMomentEnd = moment + 15; //Just enough time to get the first note of the section
                var length = bandMomentEnd - bandMomentStart;
                bandMoments.Add(bandMomentStart);
                bandMoments.Add(length);
            }

            return bandMoments;
        }
        public string CalculateBaseScore()
        {
            var guitar = Guitar.GetBaseScore(Fretbars);
            //var rhythm = Rhythm.GetBaseScore(Fretbars);

            return "";
        }
        public static void GetFurthestIntegers(List<int> numbers, int numMoments)
        {
            if (numbers == null || numbers.Count == 0 || numMoments <= 0)
            {
                throw new ArgumentException("Invalid input");
            }

            // Sort the list to make it easier to find furthest points
            numbers.Sort();

            while (numbers.Count > numMoments)
            {
                int shortLoc = 0;
                int shortestDist = numbers[0];
                for (int i = 1; i < numbers.Count; i++)
                {
                    int dist = numbers[i] - numbers[i - 1];
                    if (dist < shortestDist)
                    {
                        shortestDist = dist;
                        shortLoc = i;
                    }
                }

                numbers.RemoveAt(shortLoc);
            }
        }
        private List<AnimNote> makeOverride(TrackChunk track)
        {
            List<AnimNote> notes = new List<AnimNote>();
            var timedNotes = track.GetTimedEvents().ToList();
            var allNotes = track.GetNotes().ToList();
            foreach (var note in allNotes)
            {
                var timeMs = TicksToMilliseconds(note.Time);
                var lengthMs = TicksToMilliseconds(note.EndTime) - timeMs;
                var time = (int)Math.Round(timeMs);
                var length = (int)Math.Round(lengthMs);
                var noteNum = note.NoteNumber;
                var velocity = note.Velocity;
                notes.Add(new AnimNote(time, noteNum, length, velocity));

            }
            return notes;
        }
        private List<QBItem> MakeMidQb()
        {
            List<QBItem> gameQb = new List<QBItem>();

            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                if (Guitar == null)
                {
                    throw new NotSupportedException("GH3 customs require a guitar track!");
                }

                gameQb.AddRange(Guitar.ProcessQbEntriesGH3(SongName, false));
                /*if (Game == GAME_GHA)
                { 
                    GhaRhythmAux(noAux);
                }*/
                gameQb.AddRange(Rhythm.ProcessQbEntriesGH3(SongName));
                if (Game == GAME_GHA)
                {
                    if (NoAux)
                    {
                        FakeAux();
                    }
                    gameQb.AddRange(Aux.ProcessQbEntriesGH3(SongName));
                }
                gameQb.AddRange(GuitarCoop.ProcessQbEntriesGH3(SongName));
                gameQb.AddRange(RhythmCoop.ProcessQbEntriesGH3(SongName));
                gameQb.AddRange(Guitar.MakeFaceOffQb(SongName));
                gameQb.AddRange(MakeBossBattleQb());
                gameQb.AddRange(MakeFretbarsAndTimeSig());
                gameQb.AddRange(ProcessMarkers());
                gameQb.Add(AnimNodeQbItem($"{SongName}_scripts_notes", ScriptNotes));
                gameQb.Add(MakeGtrAnimNotes());
                gameQb.Add(AnimNodeQbItem($"{SongName}_triggers_notes", TriggersNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_cameras_notes", CamerasNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_lightshow_notes", LightshowNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_crowd_notes", CrowdNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_drums_notes", DrumsNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_performance_notes", PerformanceNotes));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_scripts", ScriptScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_anim", AnimScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_triggers", TriggersScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_cameras", CamerasScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_lightshow", LightshowScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_crowd", CrowdScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_drums", DrumsScripts));
                gameQb.Add(MakePerformanceScriptsQb($"{SongName}_performance"));
            }
            else if (Game == GAME_GHWT)
            {
                gameQb.AddRange(MakeFretbarsAndTimeSig());
                gameQb.AddRange(Guitar.ProcessQbEntriesGHWT(SongName, GamePlatform));
                gameQb.AddRange(Rhythm.ProcessQbEntriesGHWT(SongName, GamePlatform));
                gameQb.AddRange(Drums.ProcessQbEntriesGHWT(SongName, GamePlatform));
                gameQb.AddRange(Aux.ProcessQbEntriesGHWT(SongName, GamePlatform));
                gameQb.AddRange(GuitarCoop.ProcessQbEntriesGHWT(SongName, GamePlatform));
                gameQb.AddRange(RhythmCoop.ProcessQbEntriesGHWT(SongName, GamePlatform));
                gameQb.AddRange(MakeBossBattleQb());
                gameQb.AddRange(ProcessMarkers());
                gameQb.AddRange(Drums.MakeDrumFillQb(SongName));
                gameQb.Add(AnimNodeQbItem($"{SongName}_scripts_notes", ScriptNotes));
                gameQb.Add(MakeGtrAnimNotes());
                gameQb.Add(AnimNodeQbItem($"{SongName}_triggers_notes", TriggersNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_cameras_notes", CamerasNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_lightshow_notes", LightshowNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_crowd_notes", CrowdNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_drums_notes", DrumsNotes));
                gameQb.Add(AnimNodeQbItem($"{SongName}_performance_notes", PerformanceNotes));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_scripts", ScriptScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_anim", AnimScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_triggers", TriggersScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_cameras", CamerasScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_lightshow", LightshowScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_crowd", CrowdScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_drums", DrumsScripts));
                gameQb.Add(MakePerformanceScriptsQb($"{SongName}_performance"));
                var (voxQb, QsDict) = Vocals.AddVoxToQb(SongName);
                // Merge QsDict with QsList
                if (!WtExpertPlusPak)
                {
                    QsList = QsList.Concat(QsDict).ToDictionary(x => x.Key, x => x.Value);
                }
                gameQb.AddRange(voxQb);
            }
            else if (Game == GAME_GHWOR || Game == GAME_GH5)
            {
                MakeGh5Markers();
                SplitPerformanceScripts();
                gameQb.Add(MakeGtrAnimNotes());
                gameQb.Add(ScriptArrayQbItem($"{SongName}_anim", AnimScripts));
                gameQb.Add(AnimNodeQbItem($"{SongName}_drums_notes", DrumsNotes));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_scripts", ScriptScripts));
                gameQb.Add(AnimNodeQbItem($"{SongName}_cameras_notes", null));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_cameras", CamerasScripts));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_performance", ScriptScripts));
                gameQb.Add(AnimNodeQbItem($"{SongName}_crowd_notes", CrowdNotes));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_crowd", CrowdScripts));
                gameQb.Add(AnimNodeQbItem($"{SongName}_lightshow_notes", LightshowNotes));
                gameQb.Add(ScriptArrayQbItem($"{SongName}_lightshow", LightshowScripts));

                gameQb.Add(TimedScriptArrayQbItem($"{SongName}_facial", FacialTimedScripts));
                gameQb.Add(LocalizedStringsQbItem($"{SongName}_localized_strings"));
                PerfScriptEvents = TimedScriptArrayQbItem($"{SongName}_scriptevents", ScriptTimedEvents);
            }
            return gameQb;
        }
        public byte[] MakeGh5Notes()
        {
            int entries = 0;
            List<byte> noteFile = [
                .. Gh5TempoMap(ref entries),
                .. Gh5BandMoments(ref entries),
                .. Gh5Markers(ref entries),
                .. Guitar.ProcessNewNotes(ref entries),
                .. Rhythm.ProcessNewNotes(ref entries),
                .. Drums.ProcessNewNotes(ref entries),
                .. Drums.ProcessNewDrumFills(ref entries),
                .. Vocals.MakeGh5Vocals(ref entries),
            ];
            CheckForDoubleKick();
            using (var stream = new MemoryStream())
            {
                _readWriteGh5.WriteInt32(stream, NOTE_ID);
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(SongName));
                _readWriteGh5.WriteInt32(stream, entries);
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(NOTE));
                _readWriteGh5.PadStreamTo(stream, 28);
                stream.Write(noteFile.ToArray(), 0, noteFile.Count);
                return stream.ToArray();
            }
        }
        private void CheckForDoubleKick()
        {
            if (Drums.Expert.PlayNotes.Any(note => (note.Note & 0x40) != 0) || Drums.Expert.PlayNotes.Any(note => note.Ghosts != 0))
            {
                DoubleKick = true;
            }
        }
        public void SetEmptyTracksToDiffZero(Dictionary<string, int> diffs)
        {
            if (Guitar.Expert.PlayNotes.Count == 0 && diffs["guitar"] != 0)
            {
                diffs["guitar"] = 0;
                WriteTierOverride("Guitar", 0);
            }
            if (Rhythm.Expert.PlayNotes.Count == 0 && diffs["bass"] != 0)
            {
                diffs["bass"] = 0;
                WriteTierOverride("Bass", 0);
            }
            if (Drums.Expert.PlayNotes.Count == 0 && diffs["drums"] != 0)
            {
                diffs["drums"] = 0;
                WriteTierOverride("Drums", 0);
            }
            if (Vocals.Notes.Count == 0 && diffs["vocals"] != 0)
            {
                diffs["vocals"] = 0;
                WriteTierOverride("Vocals", 0);
            }
        }
        private void WriteTierOverride(string part, int tier)
        {
            string noPlay = tier == 0 ? " due to no playable notes" : "";
            Console.WriteLine($"Overriding {part} tier to difficulty {tier}{noPlay}.");
        }
        public byte[] MakeGh5Perf()
        {
            int entries = 6;
            byte[] animStructs;
            if (HasGh6Loops())
            {
                animStructs = Gh6AnimStructs();
            }
            else
            {
                animStructs = Gh5AnimStructs();
            }
            List<byte> perfFile = [
                .. Gh5Cameras(),
                .. animStructs,
            ];
            using (var stream = new MemoryStream())
            {
                _readWriteGh5.WriteInt32(stream, PERF_ID);
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(SongName));
                _readWriteGh5.WriteInt32(stream, entries);
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(PERF));
                _readWriteGh5.PadStreamTo(stream, 28);
                stream.Write(perfFile.ToArray(), 0, perfFile.Count);
                return stream.ToArray();
            }
        }
        private byte[] Gh5AnimStructs()
        {
            using (var stream = new MemoryStream())
            {
                foreach (var animName in new string[] { "male", "female" })
                {
                    string altString = $"{animName}_alt";
                    if (!AnimStructs.ContainsKey(animName))
                    {
                        var newStruct = new AnimStruct(animName, false);
                        AnimStructs.Add(animName, newStruct);
                    }
                    if (!AnimStructs.ContainsKey(altString))
                    {
                        var newStruct = new AnimStruct(animName, true);
                        AnimStructs.Add(altString, newStruct);
                    }
                }
                foreach (var animStruct in AnimStructs.Values)
                {
                    var structName = $"{animStruct.GetName()}_{SongName}";
                    MakeGh5PerfHeader(stream, structName, 1, "gh5_actor_loops");
                    animStruct.SetDefaultAnimStruct();
                    var animBytes = Gh5AnimBytes(animStruct);
                    stream.Write(animBytes, 0, animBytes.Length);
                }
                return stream.ToArray();
            }
        }
        private byte[] Gh6AnimStructs()
        {
            var maleBytes = new List<byte>();
            var femaleBytes = new List<byte>();
            foreach (var anim in new string[] { "guitarist", "bassist", "vocalist" })
            {
                using (MemoryStream maleStream = new MemoryStream())
                using (MemoryStream femaleStream = new MemoryStream())
                {
                    foreach (string ska in Gh6Loops["male"][anim])
                    {
                        AddLoopsToStream(maleStream, ska);
                    }
                    foreach (string ska in Gh6Loops["female"][anim])
                    {
                        AddLoopsToStream(femaleStream, ska);
                    }
                    if (maleStream.Length > 200)
                    {
                        AddToErrorList($"Too many anim loops + cameras for male characters. Max 50, found {maleStream.Length / 4}");
                    }
                    if (femaleStream.Length > 200)
                    {
                        AddToErrorList($"Too many anim loops + cameras for female characters. Max 50, found {femaleStream.Length / 4}");
                    }
                    _readWriteGh5.PadStreamTo(maleStream, 200);
                    _readWriteGh5.PadStreamTo(femaleStream, 200);
                    maleBytes.AddRange(maleStream.ToArray());
                    femaleBytes.AddRange(femaleStream.ToArray());
                }
            }
            maleBytes.AddRange(new byte[400]);
            femaleBytes.AddRange(new byte[400]);
            using (MemoryStream animStruct = new MemoryStream())
            {
                foreach (var alt in new string[] { "anim_struct", "alt_anim_struct" })
                {
                    string structName = $"car_male_{alt}_{SongName}";
                    MakeGh5PerfHeader(animStruct, structName, 1, "gh6_actor_loops");
                    animStruct.Write(maleBytes.ToArray(), 0, maleBytes.Count);

                    structName = $"car_female_{alt}_{SongName}";
                    MakeGh5PerfHeader(animStruct, structName, 1, "gh6_actor_loops");
                    animStruct.Write(femaleBytes.ToArray(), 0, maleBytes.Count);
                }
                return animStruct.ToArray();
            }
        }
        private void AddLoopsToStream(MemoryStream stream, string ska)
        {
            if (ska.StartsWith("0x"))
            {
                uint skaInt = QBKeyUInt(ska);
                if (NewKeys.TryGetValue(skaInt, out string? newKey))
                {
                    ska = newKey;
                }
            }
            if (AnimLoopsCache.AnimLoops.Contains(ska.ToLower()))
            {
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(ska));
            }
            else
            {
                AddToErrorList($"Loop '{ska}' does not exist in Warriors of Rock.");
            }
            var cameras = AnimLoopsCache.AnimLoopsCams.Where(loop => loop.Contains($"{ska.ToLower()}_c")).ToList();
            if (cameras.Count > 0)
            {
                foreach (string camera in cameras)
                {
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(camera));
                }
            }
            else
            {
                AddToErrorList($"No cameras found for '{ska}'.");
            }
        }
        private byte[] Gh5AnimBytes(AnimStruct animStruct)
        {
            var animBytes = new List<byte>();
            using (var stream = new MemoryStream())
            {
                foreach (InstrumentAnim anim in new InstrumentAnim[] { animStruct.Guitar, animStruct.Bass, animStruct.Vocals, animStruct.Drum })
                {
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(anim.Pak));
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(anim.AnimSet));
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(anim.FingerAnims));
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(anim.FretAnims));
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(anim.StrumAnims));
                    _readWriteGh5.WriteUInt32(stream, QBKeyUInt(anim.FacialAnims));
                }

                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(DRUMLOOPS_ANIMS));
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(DRUMLOOPS_ANIMS_SET));
                _readWriteGh5.WriteUInt32(stream, QBKeyUInt(animStruct.Drum.FacialAnims));

                return stream.ToArray();
            }
        }
        private byte[] Gh5Cameras()
        {
            var autoCameras = new List<AnimNote>();
            var momentCameras = new List<AnimNote>();
            foreach (var camera in CamerasNotes)
            {
                if (momentCams.TryGetValue(camera.Note, out int autocut))
                {
                    momentCameras.Add(camera);
                    autoCameras.Add(new AnimNote(camera.Time, autocut, camera.Length, camera.Velocity));
                }
                else
                {
                    autoCameras.Add(camera);
                }
            }
            using (MemoryStream stream = new MemoryStream())
            {
                MakeGh5PerfHeader(stream, "AutocutCameras", autoCameras.Count, "gh5_camera_note");
                Gh5CamerasToBytes(stream, autoCameras);
                MakeGh5PerfHeader(stream, "MomentCameras", momentCameras.Count, "gh5_camera_note");
                Gh5CamerasToBytes(stream, momentCameras);
                return stream.ToArray();
            }
        }
        private void Gh5CamerasToBytes(MemoryStream stream, List<AnimNote> cameraNotes)
        {
            foreach (AnimNote camera in cameraNotes)
            {
                _readWriteGh5.WriteInt32(stream, camera.Time);
                _readWriteGh5.WriteInt16(stream, (short)camera.Length);
                _readWriteGh5.WriteInt8(stream, (byte)camera.Note);
            }
        }
        private byte[] Gh5TempoMap(ref int entries)
        {
            entries += 2;
            using (MemoryStream stream = new MemoryStream())
            {
                MakeGh5NoteHeader(stream, "fretbar", Fretbars.Count, "gh5_fretbar_note", 4);
                foreach (int fretbar in Fretbars)
                {
                    _readWriteGh5.WriteInt32(stream, fretbar);
                }

                MakeGh5NoteHeader(stream, "timesig", TimeSigs.Count, "gh5_timesig_note", 6);
                foreach (TimeSig ts in TimeSigs)
                {
                    _readWriteGh5.WriteInt32(stream, ts.Time);
                    _readWriteGh5.WriteInt8(stream, (byte)ts.Numerator);
                    _readWriteGh5.WriteInt8(stream, (byte)ts.Denominator);
                }

                return stream.ToArray();
            }
        }
        private void MakeGh5Markers()
        {
            foreach (Marker marker in Markers)
            {
                var origMarker = marker.Text;
                marker.Text = GetQsMarkerName(marker.Text);
                try
                {
                    QsList.Add(marker.QsKeyString, marker.Text);
                }
                catch (ArgumentException e)
                {
                    AddTimedWarning($"Duplicate marker {origMarker}", "EVENTS", marker.Time);
                }
                catch
                {
                    throw new Exception("Error adding marker to QsList");
                }
            }
        }
        private string GetQsMarkerName(string qs)
        {
            if (qs == "_ENDOFSONG")
            {
                return $"\\L{qs}";
            }
            else
            {
                return $"\\u[m]{qs}";
            }
        }
        private byte[] Gh5Markers(ref int entries)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                MakeGh5NoteHeader(stream, "guitarmarkers", Markers.Count, "gh5_marker_note", 8);
                foreach (Marker marker in Markers)
                {
                    _readWriteGh5.WriteInt32(stream, marker.Time);
                    _readWriteGh5.WriteUInt32(stream, marker.QsKey);
                }
                entries++;
                return stream.ToArray();
            }
        }
        private byte[] Gh5BandMoments(ref int entries)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                MakeGh5NoteHeader(stream, "bandmoment", BandMoments.Count / 2, "gh5_band_moment_note", 8);
                foreach (int moment in BandMoments)
                {
                    _readWriteGh5.WriteInt32(stream, moment);
                }
                entries++;
                return stream.ToArray();
            }
        }
        private static void MakeNewStarPower(Stream stream, string spName, List<StarPower> starEntries, ref int entries)
        {
            uint spSize = 6;
            MakeGh5NoteHeader(stream, spName, starEntries.Count, "gh5_star_note", spSize);
            foreach (StarPower star in starEntries)
            {
                _readWriteGh5.WriteInt32(stream, star.Time);
                _readWriteGh5.WriteInt16(stream, (short)star.Length);
            }
            entries++;
            return;
        }
        private static void MakeGh5NoteHeader(Stream stream, string secName, int entryCount, string entryType, uint elementSize)
        {
            uint secQb = QBKeyUInt(secName);
            _readWriteGh5.WriteUInt32(stream, secQb);
            _readWriteGh5.WriteInt32(stream, entryCount);
            _readWriteGh5.WriteUInt32(stream, QBKeyUInt(entryType));
            _readWriteGh5.WriteUInt32(stream, elementSize);
        }
        private static void MakeGh5PerfHeader(Stream stream, string secName, int entryCount, string entryType)
        {
            uint secQb = QBKeyUInt(secName);
            _readWriteGh5.WriteUInt32(stream, secQb);
            _readWriteGh5.WriteInt32(stream, entryCount);
            _readWriteGh5.WriteUInt32(stream, QBKeyUInt(entryType));
        }
        private void GhaRhythmAux(bool noAux)
        {
            // If rhythm_track == 1, Aux is bass, else it's the rhythm guitar
            if (RhythmTrack)
            {
                if (RhythmCoop.Expert.PlayNotes.Count > 0 && Rhythm.Expert.PlayNotes.Count > 0)
                {

                }
            }
        }
        private void FakeAux()
        {
            Instrument newAux;
            List<AnimNote> newAuxAnim;
            int animMod;
            byte comp1 = 0;
            byte comp2 = 0;

            if (RhythmCoop.Expert.PlayNotes.Count != 0)
            {
                newAux = Rhythm.Expert.PlayNotes.Count != 0 ? Rhythm : RhythmCoop;
                animMod = 1;
                comp1 = 101;
                comp2 = 110;
            }
            else
            {
                newAux = Guitar;
                animMod = 2;
                comp1 = 118;
                comp2 = 127;
            }
            List<AnimNote> newAuxNotes = new List<AnimNote>();
            if (newAux.AnimNotes.Count == 0 && AnimNotes.Count != 0) // Implying this is a q file convert
            {
                foreach (AnimNote note in AnimNotes)
                {
                    if (note.Note >= comp1 && note.Note <= comp2)
                    {
                        newAuxNotes.Add(new AnimNote(note.Time, note.Note - (animMod * 17), note.Length, note.Velocity));
                    }
                }
                AnimNotes.AddRange(newAuxNotes);
            }
            else
            {
                foreach (AnimNote note in newAux.AnimNotes)
                {
                    newAuxNotes.Add(new AnimNote(note.Time, note.Note - (animMod * 17), note.Length, note.Velocity));
                }
                Aux.AnimNotes = newAuxNotes;
            }
  
            Aux.Expert = newAux.Expert;

        }
        public void ParseQbToData(byte[]? midQb,
            Dictionary<uint, string>? midQs,
            byte[]? songScripts,
            byte[]? notes,
            byte[]? perf,
            byte[]? perfXml)
        {
            if (midQb != null)
            {
                ParseMidQb(midQb, midQs);
            }
            if (notes != null)
            {
                ParseNoteFileData(notes, midQs);
            }
        }
        private void ParseMidQb(byte[] midQb, Dictionary<uint, string>? midQs)
        {
            var qbDict = QbEntryDictFromBytes(midQb, Endian, SongName!);
            DetermineQType(qbDict, out _);
            ParseTimeSigsAndFretbarsFromQb(qbDict);
            ParseInstrumentsFromQb(qbDict, midQs);
        }
        private string GetDebugString(uint key)
        {
            return DebugReader.DbgString(key);
        }
        private Dictionary<string, Dictionary<string, Dictionary<string, List<int>>>> MakeBlankGh5Dictionary()
        {
            var instruments = new Dictionary<string, Dictionary<string, Dictionary<string, List<int>>>>
            {
                { GH5DRUMS, new Dictionary<string, Dictionary<string, List<int>>> ()
                    {
                        {EASY, new Dictionary<string, List<int>>()},
                        {MEDIUM, new Dictionary<string, List<int>>()},
                        {HARD, new Dictionary<string, List<int>>()},
                        {EXPERT, new Dictionary<string, List<int>>()}
                    }
                },
                { GH5GUITAR, new Dictionary<string, Dictionary<string, List<int>>> ()
                    {
                        {EASY, new Dictionary<string, List<int>>()},
                        {MEDIUM, new Dictionary<string, List<int>>()},
                        {HARD, new Dictionary<string, List<int>>()},
                        {EXPERT, new Dictionary<string, List<int>>()}
                    }
                },
                { GH5BASS, new Dictionary<string, Dictionary<string, List<int>>> ()
                    {
                        {EASY, new Dictionary<string, List<int>>()},
                        {MEDIUM, new Dictionary<string, List<int>>()},
                        {HARD, new Dictionary<string, List<int>>()},
                        {EXPERT, new Dictionary<string, List<int>>()}
                    }
                },
            };
            return instruments;
        }
        private void ParseNoteFileData(byte[] notes, Dictionary<uint, string>? midQs)
        {
            var instrumentData = MakeBlankGh5Dictionary();
            var voxPhrases = new List<int>();
            var voxMarkers = new Dictionary<int, string>();
            int sectionNum = 1;
            bool expertPlus = false;
            using (MemoryStream stream = new MemoryStream(notes))
            {
                stream.Position = 8;
                uint numEntries = _readWriteGh5.ReadUInt32(stream);
                uint fileType = _readWriteGh5.ReadUInt32(stream);
                if (GetDebugString(fileType) != NOTE)
                {
                    throw new Exception("Invalid note file type");
                }
                stream.Position = 0x1C;
                for (int i = 0; i < numEntries; i++)
                {
                    bool skip = false;
                    uint noteType = _readWriteGh5.ReadUInt32(stream);
                    string noteTypeString = GetDebugString(noteType);
                    (string inst, string diff, string modifier) = GetNoteType(noteTypeString);
                    uint entries = _readWriteGh5.ReadUInt32(stream);
                    var entryType = GetDebugString(_readWriteGh5.ReadUInt32(stream));
                    uint elementSize = _readWriteGh5.ReadUInt32(stream);
                    if (noteTypeString.StartsWith("0x"))
                    {
                        Console.WriteLine($"Skipping unknown note type {noteTypeString}");
                        skip = true;
                    }
                    switch (entryType)
                    {
                        case "gh5_fretbar_note":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                Fretbars.Add(time);
                            }
                            break;
                        case "gh5_timesig_note":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                byte num = _readWriteGh5.ReadUInt8(stream);
                                byte den = _readWriteGh5.ReadUInt8(stream);
                                TimeSigs.Add(new TimeSig(time, num, den));
                            }
                            break;
                        case "gh5_band_moment_note":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                int length = _readWriteGh5.ReadInt32(stream);
                                BandMoments.AddRange([time, length]);
                            }
                            break;
                        case "gh5_star_note":
                            List<int> spEntries = new List<int>();
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                ushort length = _readWriteGh5.ReadUInt16(stream);
                                spEntries.Add(time);
                                spEntries.Add(length);
                            }
                            if (skip)
                            {
                                break;
                            }
                            if (inst.Contains(GH5VOCALS))
                            {
                                Vocals.ParseGh5Sp(spEntries);
                            }
                            else
                            {
                                instrumentData[inst][diff][modifier] = spEntries;
                            }
                            break;
                        case "gh5_vocal_lyric":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                uint toRead = elementSize - 4;
                                bool unicode = toRead == 64;
                                byte[] lyricBytes = new byte[toRead];
                                stream.Read(lyricBytes, 0, lyricBytes.Length);
                                string lyric = unicode ? Encoding.BigEndianUnicode.GetString(lyricBytes) : Encoding.ASCII.GetString(lyricBytes);
                                if (!skip)
                                {
                                    Vocals.Lyrics.Add(new VocalLyrics(time, lyric.Replace("\0", "")));
                                }
                            }
                            break;
                        case "gh5_vocal_note":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                ushort length = _readWriteGh5.ReadUInt16(stream);
                                byte note = _readWriteGh5.ReadUInt8(stream);
                                if (!skip)
                                {
                                    Vocals.Notes.Add(new PlayNote(time, length, note, "Vocals"));
                                }
                            }
                            break;
                        case "gh5_vocal_freeform_note":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                int length = _readWriteGh5.ReadInt32(stream);
                                ushort unk = _readWriteGh5.ReadUInt16(stream);
                                /*if (unk != 0)
                                {
                                    throw new Exception("Unknown Freeform Note Found!");
                                }*/
                                if (skip)
                                {
                                    break;
                                }
                                Vocals.FreeformPhrases.Add(new Freeform(time, length, unk));
                            }
                            break;
                        case "gh5_vocal_marker_note":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                uint toRead = elementSize - 4;
                                bool unicode = toRead == 256;
                                byte[] markerBytes = new byte[toRead];
                                stream.Read(markerBytes, 0, markerBytes.Length);
                                string marker = unicode ? Encoding.BigEndianUnicode.GetString(markerBytes) : Encoding.ASCII.GetString(markerBytes);
                                if (!skip)
                                {
                                    voxMarkers[time] = marker;
                                }
                            }
                            break;
                        case "gh5_vocal_phrase":
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                if (!skip)
                                {
                                    voxPhrases.Add(time);
                                }
                            }
                            break;
                        case "gh6_expert_drum_note":
                            expertPlus = true;
                            goto case "gh5_instrument_note";
                        case "gh5_instrument_note":
                            bool expertGh6Drums = inst == GH5DRUMS && diff == EXPERT && expertPlus;
                            var instrumentNotes = new List<int>();
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                ushort length = _readWriteGh5.ReadUInt16(stream);
                                byte note = _readWriteGh5.ReadUInt8(stream);
                                byte accents = _readWriteGh5.ReadUInt8(stream);
                                instrumentNotes.AddRange([time, length, note, accents]);
                                if (expertGh6Drums)
                                {
                                    byte ghosts = _readWriteGh5.ReadUInt8(stream);
                                    instrumentNotes.Add(ghosts);
                                }
                            }
                            instrumentData[inst][diff][modifier] = instrumentNotes;
                            break;
                        case "gh5_tapping_note":
                            var tappingNotes = new List<int>();
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                int length = _readWriteGh5.ReadInt32(stream);
                                tappingNotes.AddRange([time, length]);
                            }
                            instrumentData[inst][diff][modifier] = tappingNotes;
                            break;
                        case "gh5_drumfill_note":
                            diff = noteTypeString.Substring(0, noteTypeString.IndexOf("drumfill"));
                            var drumFillNotes = new List<int>();
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                int timeEnd = _readWriteGh5.ReadInt32(stream);
                                drumFillNotes.AddRange([time, timeEnd]);
                            }
                            instrumentData[GH5DRUMS][diff]["drumfill"] = drumFillNotes;
                            break;
                        case "gh5_marker_note":
                            Markers = new List<Marker>();
                            for (int j = 0; j < entries; j++)
                            {
                                int time = _readWriteGh5.ReadInt32(stream);
                                var markerQb = _readWriteGh5.ReadUInt32(stream);
                                string? marker = "";
                                if (!midQs.TryGetValue(markerQb, out marker))
                                {
                                    marker = $"Section {sectionNum}";
                                    sectionNum++;
                                }
                                marker = Regex.Replace(marker, @"\\u\[(.*?)\]", "");
                                marker = Regex.Replace(marker, @"\\L", "");
                                if (marker.StartsWith("\"") && marker.EndsWith("\""))
                                {
                                    marker = marker.Substring(1, marker.Length - 2);
                                }
                                Markers.Add(new Marker(time, marker));
                            }
                            break;
                        case "gh6_phoneme_note":
                            uint toSkip = entries * elementSize;
                            stream.Position += toSkip;
                            break;
                        default:
                            throw new Exception("Invalid entry type");
                    }
                }
            }
            Guitar = new Instrument(GUITAR_NAME, instrumentData[GH5GUITAR], this);
            Rhythm = new Instrument(RHYTHM_NAME, instrumentData[GH5BASS], this);
            Drums = new Instrument(DRUMS_NAME, instrumentData[GH5DRUMS], this, expertPlus);
        }
        private void ParseTimeSigsAndFretbarsFromQb(Dictionary<string, QBItem> qbList)
        {
            if (qbList.TryGetValue($"{SongName}_fretbars", out QBItem fretbars))
            {
                if (fretbars.Info.Type != ARRAY)
                {
                    throw new Exception("Fretbars entry is not an array");
                }
                var fretbarArray = fretbars.Data as QBArrayNode;
                if (fretbarArray.FirstItem.Type != INTEGER)
                {
                    throw new Exception("Fretbars entry is not an array of integers");
                }
                IterateFretbars(fretbarArray.Items);
            }
            if (qbList.TryGetValue($"{SongName}_timesig", out QBItem timesigs))
            {
                if (timesigs.Info.Type != ARRAY)
                {
                    throw new Exception("Timesig entry is not an array");
                }
                var timeSigArray = timesigs.Data as QBArrayNode;
                TimeSigs = new List<TimeSig>();
                foreach (var ts in timeSigArray!.Items)
                {
                    var tsArray = ts as QBArrayNode;
                    if (tsArray.FirstItem.Type != INTEGER)
                    {
                        throw new Exception("Timesig entry is not an array");
                    }
                    if (tsArray.Items.Count != 3)
                    {
                        throw new Exception("Timesig entry is not an array of length 3");
                    }
                    var tsTime = (int)tsArray.Items[0];
                    if (!Fretbars.Contains(tsTime))
                    {
                        // I don't know why this is necessary, but it is
                        // How does a TS change differ from a fretbar?
                        var closestFb = GetClosestIntFromList(tsTime, Fretbars);
                        tsTime = closestFb;
                    }
                    TimeSig newTs = new TimeSig(tsTime, (int)tsArray.Items[1], (int)tsArray.Items[2]);
                    TimeSigs.Add(newTs);
                }

            }

        }
        private void IterateFretbars(List<object> fretbars)
        {
            Fretbars = new List<int>();
            foreach (var fb in fretbars)
            {
                Fretbars.Add((int)fb);
            }
        }
        private void ParseInstrumentsFromQb(Dictionary<string, QBItem> qbList, Dictionary<uint, string>? midQs)
        {
            // Dictionary to hold references to the class variables using Action<Instrument>
            Dictionary<string, Action<Instrument>> instrumentLookup = new Dictionary<string, Action<Instrument>>()
            {
                {DRUMS_NAME, (val) => Drums = val },
                {GUITAR_NAME, (val) => Guitar = val },
                {RHYTHM_NAME, (val) => Rhythm = val },
                {AUX_NAME, (val) => Aux = val },
                {GUITARCOOP_NAME, (val) => GuitarCoop = val },
                {RHYTHMCOOP_NAME, (val) => RhythmCoop = val },
            };
            var instruments = instrumentLookup.Keys;
            if (midQs == null)
            {
                midQs = new Dictionary<uint, string>();
            }

            foreach (string instrument in instruments)
            {
                if (instrument == VOCALS_NAME)
                {

                }
                else
                {
                    var newIns = new Instrument(instrument, SongName, qbList, this, QfileGame);
                    // Update the class variable directly by calling the Action<Instrument>
                    instrumentLookup[instrument](newIns);
                }
            }
        }
        private void ParseMarkersFromQ(Dictionary<string, QBItem> qbDict)
        {
            Markers = new List<Marker>();
            if (qbDict.TryGetValue($"{SongName}_markers", out QBItem markers))
            {
                if (markers.Info.Type != ARRAY)
                {
                    throw new Exception("Markers is not an array");
                }
                var markerArray = markers.Data as QBArrayNode;
                if (markerArray.FirstItem.Type != STRUCT)
                {
                    throw new Exception("Markers entry is not an array of structs");
                }
                foreach (QBStructData marker in markerArray!.Items)
                {
                    int time;
                    if (marker.StructDict.ContainsKey("time"))
                    {
                        time = (int)marker.StructDict["time"];
                    }
                    else
                    {
                        throw new Exception("Marker struct does not contain time");
                    }
                    string text;
                    if (marker.StructDict.ContainsKey("marker"))
                    {
                        text = (string)marker.StructDict["marker"];
                    }
                    else
                    {
                        throw new Exception("Marker struct does not contain text");
                    }
                    Markers.Add(new Marker(time, text));
                }
            }
        }
        private void ParseNonInstrumentFromQ(Dictionary<string, QBItem> qbDict)
        {
            var toParse = new string[] { "anim", "scripts", "triggers", "cameras", "lightshow", "crowd", "drums", "performance"};
            var scriptsLookup = new Dictionary<string, Action<QBArrayNode>>
            {
                { "anim", (val) => AnimScripts = val},
                { "scripts", (val) => ScriptScripts = val },
                { "triggers", (val) => TriggersScripts = val },
                { "cameras", (val) => CamerasScripts = val },
                { "lightshow",(val) => LightshowScripts = val  },
                { "crowd", (val) => CrowdScripts = val },
                { "drums", (val) => DrumsScripts = val },
                { "performance",(val) => PerformanceScripts = val}
            };
            var notesLookup = new Dictionary<string, Action<List<AnimNote>?>>
            {
                { "anim", (val) => AnimNotes = val},
                { "scripts", (val) => ScriptNotes = val },
                { "triggers", (val) => TriggersNotes = val },
                { "cameras", (val) => CamerasNotes = val },
                { "lightshow", (val) => LightshowNotes = val },
                { "crowd", (val) => CrowdNotes = val },
                { "drums", (val) => DrumsNotes = val },
                { "performance", (val) => PerformanceNotes = val }
            };

            foreach (var item in toParse)
            {
                var dictItem = $"{SongName}_{item}";
                var notesVariant = $"{SongName}_{item}_notes";
                if (qbDict.TryGetValue(dictItem, out QBItem scripts))
                {
                    scriptsLookup[item]((QBArrayNode)scripts.Data);
                }
                if (qbDict.TryGetValue(notesVariant, out QBItem notes))
                {
                    if (notes.Info.Type != ARRAY)
                    {
                        throw new Exception($"{SongName}_{item}_notes entry is not an array");
                    }
                    var notesData = (QBArrayNode)notes.Data;
                    if (notesData.FirstItem.Type == ARRAY) // GH3/GHA file
                    {
                        List<AnimNote> animNotes = new List<AnimNote>();
                        foreach (QBArrayNode noteArray in notesData.Items)
                        {
                            int time = (int)noteArray.Items[0];
                            int note = (int)noteArray.Items[1];
                            int length = (int)noteArray.Items[2];
                            int velocity = 100;
                            if (noteArray.Items.Count == 4)
                            {
                                velocity = (int)noteArray.Items[3];
                            }
                            animNotes.Add(new AnimNote(time, note, length, velocity));
                        }
                        if (animNotes.Count != 0)
                        {
                            notesLookup[item](animNotes);
                        }
                    }
                    else if (notesData.FirstItem.Type == STRUCTFLAG)
                    {
                        // Do Nothing
                    }
                    else
                    {
                        throw new NotImplementedException("GHWT style not yet supported");
                    }
                }
            }
            if (PerformanceScripts != null)
            {
                var newPerf = new List<object>();
                foreach (QBStructData script in PerformanceScripts.Items)
                {
                    try
                    {
                        var scrType = ((string)script["scr"]).ToLower();
                        if (scrType == BAND_PLAYFACIALANIM)
                        {
                            var scrParams = (QBStructData)script["params"];
                            switch (((string)scrParams["name"]).ToLower())
                            {
                                case "vocalist":
                                    VoxSkaAnims.Add((string)scrParams["anim"]);
                                    break;
                                case "guitarist":
                                    GtrSkaAnims.Add((string)scrParams["anim"]);
                                    break;
                                case "bassist":
                                    BassSkaAnims.Add((string)scrParams["anim"]);
                                    break;
                                case "drummer":
                                    DrumSkaAnims.Add((string)scrParams["anim"]);
                                    break;
                            }
                        }
                    }
                    catch
                    {

                    }
                    newPerf.Add(script);
                }
                PerformanceScripts.Items = newPerf;
            }
        }
        public byte[] ParseMidiToQb(bool expertPlus = false)
        {
            WtExpertPlusPak = expertPlus && Game == GAME_GHWT;
            string origSongName = SongName;
            var origExpertChart = Drums.Expert.PlayNotes;
            
            if (WtExpertPlusPak)
            {
                SongName = origSongName + "_expertplus"; // GHWT Expert Plus PAKs have a different name
                Drums.Expert.PlayNotes = Drums.Expert.ExPlusNotes; // Use the Expert Plus notes for GHWT;
            }
            if (!MidiParsed)
            {
                ParseMidi();
            }
            var gameQb = MakeConsoleQb();
            if (WtExpertPlusPak)
            {
                SongName = origSongName; // Reset song name
                Drums.Expert.PlayNotes = origExpertChart; // Reset the Expert notes
            }
            WtExpertPlusPak = false; // Reset the flag after making the console QB
            return gameQb;
        }
        public byte[] MakeConsoleQb()
        {
            var gameQb = MakeMidQb();
            string songMid;
            if (GamePlatform == CONSOLE_PS2)
            {
                songMid = $"data\\songs\\{SongName}.mid.qb";
            }
            else
            {
                songMid = $"songs\\{SongName}.mid.qb";
            }
            byte[] bytes = CompileQbFile(gameQb, songMid, Game, GamePlatform);
            return bytes;
        }
        public List<QBItem> MakeBossBattleQb()
        {
            List<QBItem> qBItems = new List<QBItem>();
            string qbName = $"{SongName}_BossBattle";
            for (int i = 1; i < 3; i++)
            {
                QBItem qBItem = new QBItem();
                qBItem.MakeEmpty($"{qbName}P{i}");
                qBItems.Add(qBItem);
            }

            return qBItems;
        }
        public List<QBItem> MakeFretbarsAndTimeSig()
        {
            List<QBItem> qbItems = new List<QBItem>();
            string tsName = $"{SongName}_timesig";
            QBArrayNode timeSigArray = new QBArrayNode();
            foreach (TimeSig ts in TimeSigs)
            {
                QBArrayNode tsEntry = new QBArrayNode();
                tsEntry.AddIntToArray(ts.Time);
                tsEntry.AddIntToArray(ts.Numerator);
                tsEntry.AddIntToArray(ts.Denominator);
                timeSigArray.AddArrayToArray(tsEntry);
            }
            qbItems.Add(new QBItem(tsName, timeSigArray));

            string fretName = $"{SongName}_fretbars";
            QBArrayNode fretbarArray = new QBArrayNode();
            fretbarArray.AddListToArray(Fretbars);
            qbItems.Add(new QBItem(fretName, fretbarArray));

            return qbItems;
        }
        public QBItem MakeGtrAnimNotes()
        {
            if (AnimNotes == null)
            {
                AnimNotes = Guitar.AnimNotes;
                if (Game == GAME_GH3)
                {
                    if (RhythmCoop.AnimNotes.Count != 0)
                    {
                        AnimNotes.AddRange(RhythmCoop.AnimNotes);
                    }
                    else
                    {
                        AnimNotes.AddRange(Rhythm.AnimNotes);
                    }
                }

                else if (Game == GAME_GHA)
                {
                    AnimNotes.AddRange(Rhythm.AnimNotes);
                    AnimNotes.AddRange(Aux.AnimNotes);
                }

                else
                {
                    AnimNotes.AddRange(Rhythm.AnimNotes);
                }
            }

            // Sort animNotes by the Time variable in each AnimNote
            AnimNotes.Sort((x, y) => x.Time.CompareTo(y.Time));

            return AnimNodeQbItem($"{SongName}_anim_notes", AnimNotes);
        }
        public QBArrayNode ProcessAnimNoteList(List<AnimNote> animNotes)
        {
            QBArrayNode animArray = new QBArrayNode();
            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                foreach (AnimNote animNote in animNotes)
                {
                    QBArrayNode animEntry = animNote.ToGH3Anim();
                    animArray.AddArrayToArray(animEntry);
                }
            }
            else
            {
                foreach (AnimNote animNote in animNotes)
                {
                    animNote.ToWtAnim(animArray);

                }
                // throw new NotImplementedException("Coming soon");
            }
            return animArray;
        }
        public QBItem AnimNodeQbItem(string name, List<AnimNote>? animNotes)
        {
            if (animNotes == null)
            {
                QBItem qBItem = new QBItem();
                qBItem.MakeEmpty(name);
                return qBItem;
            }
            else
            {
                QBArrayNode animArray = ProcessAnimNoteList(animNotes);
                return new QBItem(name, animArray);
            }
        }
        public QBItem ScriptArrayQbItem(string name, QBArrayNode? scriptArray)
        {
            if (scriptArray == null)
            {
                QBItem qBItem = new QBItem();
                qBItem.MakeEmpty(name);
                return qBItem;
            }
            else
            {
                return new QBItem(name, scriptArray);
            }
        }
        public QBItem TimedScriptArrayQbItem(string name, List<(int, QBStructData)> scriptArray)
        {
            QBArrayNode timedScriptArray = new QBArrayNode();
            foreach ((int time, QBStructData script) in scriptArray)
            {
                timedScriptArray.AddStructToArray(script);
            }
            return new QBItem(name, timedScriptArray);
        }
        public QBItem LocalizedStringsQbItem(string name)
        {

            QBArrayNode localizedStrings = new QBArrayNode();
            var sortedValues = QsList.Values.ToList();
            sortedValues.Sort();
            foreach (string qs in sortedValues)
            {
                localizedStrings.AddQskeyToArray(QBKeyQs(qs));
            }
            return new QBItem(name, localizedStrings);
        }
        public List<QBItem> ProcessMarkers()
        {
            if (Markers == null)
            {
                Markers = [new Marker(0, "start")];
            }
            QBArrayNode markerArray = new QBArrayNode();
            foreach (Marker marker in Markers)
            {
                QBStructData markerEntry = null;
                if (marker.Text == "_ENDOFSONG" && Game == GAME_GHWT)
                {
                    marker.Text = $"\\L{marker.Text}";
                    markerEntry = marker.ToStructQs();
                    QsList.Add(marker.QsKeyString, $"\"{marker.Text}\"");
                }
                else
                {
                    if (marker.Text.ToLower().StartsWith("0x"))
                    {
                        if (SongSections.TryGetValue(marker.Text, out var newMarker))
                        {
                            marker.Text = (string)newMarker.Data;
                        }
                    }
                    markerEntry = marker.ToStruct(GamePlatform);
                }

                markerArray.AddStructToArray(markerEntry);
            }
            string markerName;
            var qbList = new List<QBItem>();
            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                markerName = $"{SongName}_markers";
            }
            else
            {
                markerName = $"{SongName}_guitar_markers";

                QBArrayNode fakeArray = new QBArrayNode();
                fakeArray.MakeEmpty();
                QBItem rhythmMarker = new QBItem($"{SongName}_rhythm_markers", fakeArray);
                qbList.Add(rhythmMarker);
                QBItem drumsMarker = new QBItem($"{SongName}_drum_markers", fakeArray);
                qbList.Add(drumsMarker);
            }
            QBItem gtrMarker = new QBItem(markerName, markerArray);
            qbList.Insert(0, gtrMarker);

            return qbList;
        }
        private List<MidiData.Note> ConvertCameras(List<MidiData.Note> cameraNotes, Dictionary<int, int> cameraDict)
        {
            for (int i = 0; i < cameraNotes.Count; i++)
            {
                MidiData.Note note = cameraNotes[i];
                int noteVal = note.NoteNumber;
                if (cameraDict.TryGetValue(noteVal, out int newNoteVal))
                {
                    cameraNotes[i] = new MidiData.Note((SevenBitNumber)newNoteVal, note.Length, note.Time);
                    cameraNotes[i].Velocity = note.Velocity;
                }
            }
            return cameraNotes;
        }
        public void ProcessCameras(TrackChunk trackChunk)
        {
            int MinCameraGh3 = 79;
            int MaxCameraGh3 = 117;
            int MinCameraGha = 3;
            int MaxCameraGha = 91;
            int MinCameraWt = 3;
            int MaxCameraWt = 127;            
            int MinCamera5 = 3;
            int MaxCamera5 = 99;
            int MinCameraWor = 3;
            int MaxCameraWor = 99;

            var gh5NoteFilter = new Dictionary<int, int> 
            {
                {40, 74},
                {41, 74},
                {59, 31},
                {62, 14},
                {63, 9},
                {64, 24},
                {65, 18},
                {66, 61},
                {67, 60},
                {68, 26},
                {69, 8}
            };

            int minCamera;
            int maxCamera;
            switch (VenueSource)
            {
                case GAME_GH3:
                    minCamera = MinCameraGh3;
                    maxCamera = MaxCameraGh3;
                    break;
                case GAME_GHA:
                    minCamera = MinCameraGha;
                    maxCamera = MaxCameraGha;
                    break;
                case GAME_GHWT:
                    minCamera = MinCameraWt;
                    maxCamera = MaxCameraWt;
                    break;
                case GAME_GH5:
                    minCamera = MinCamera5;
                    maxCamera = MaxCamera5;
                    break;
                case GAME_GHWOR:
                    minCamera = MinCameraWor;
                    maxCamera = MaxCameraWor;
                    break;
                default:
                    throw new NotImplementedException("Unknown game found");
            }
            var timedEvents = trackChunk.GetTimedEvents().ToList();
            var textEvents = timedEvents.Where(e => e.Event is TextEvent || e.Event is LyricEvent).ToList();

            var cameraNotes = trackChunk.GetNotes().Where(x => x.NoteNumber >= minCamera && x.NoteNumber <= maxCamera).ToList();

            var (game, venue) = NormalizeGameAndVenueSource(Game, VenueSource);

            bool skipConvertCams = game == GAME_GHA && venue == GAME_GH3 && (GamePlatform != CONSOLE_PC && GamePlatform != CONSOLE_PS2);

            if (skipConvertCams && venue == GAME_GH3)
            {
                minCamera = MinCameraGh3 - 4;
            }

            if (game != venue && !skipConvertCams)
            {
                Dictionary<int, int> cameraMap;
                try
                {
                    switch (Game)
                    {
                        case GAME_GH3:
                            cameraMap = cameraToGh3[venue];
                            break;
                        case GAME_GHA:
                            cameraMap = cameraToGha[venue];
                            break;
                        case GAME_GHWT:
                        case GAME_GH5:
                        case GAME_GHWOR:
                            cameraMap = cameraToGhwt[venue];
                            break;
                        default:
                            throw new NotImplementedException("Unknown game found");
                    }
                }
                catch
                {
                    throw;
                }
                cameraNotes = ConvertCameras(cameraNotes, cameraMap);
            }

            List<AnimNote> cameraAnimNotes = new List<AnimNote>();
            for (int i = 0; i < cameraNotes.Count; i++)
            {
                MidiData.Note note = cameraNotes[i];
                int startTime = RoundTimeMills(note.Time);
                int length;
                if (i == cameraNotes.Count - 1)
                {
                    length = 20000;
                }
                else
                {
                    length = RoundCamLen(RoundTimeMills(cameraNotes[i + 1].Time) - startTime);
                }
                int noteVal = note.NoteNumber;
                if (Game == GAME_GH5 && gh5NoteFilter.ContainsKey(noteVal))
                {
                    noteVal = gh5NoteFilter[noteVal];
                }
                int velocity = note.Velocity;
                cameraAnimNotes.Add(new AnimNote(startTime, noteVal, length, velocity));
            }
            CamerasNotes = cameraAnimNotes;

            if (Game != GAME_GH3 && Game != GAME_GHA)
            {
                var cameraScripts = new List<(int, QBStructData)>();
                foreach (var timedEvent in textEvents)
                {
                    string eventText;
                    if (timedEvent.Event is TextEvent textEvent)
                    {
                        eventText = textEvent.Text;
                        var (eventType, eventData) = GetEventData(eventText);
                        int eventTime = GameMilliseconds(timedEvent.Time);
                        switch (eventType)
                        {
                            case ZOOM_IN_QUICK_SMALL:
                            case ZOOM_IN_QUICK_LARGE:
                            case ZOOM_OUT_QUICK_SMALL:
                            case ZOOM_OUT_QUICK_LARGE:
                            case ZOOM_IN_SLOW_SMALL:
                            case ZOOM_IN_SLOW_LARGE:
                            case ZOOM_OUT_SLOW_SMALL:
                            case ZOOM_OUT_SLOW_LARGE:
                            case PULSE1:
                            case PULSE2:
                            case PULSE3:
                            case PULSE4:
                            case PULSE5:
                                cameraScripts.Add((eventTime, MakeNewCameraFovScript(eventTime, eventType, eventData)));
                                break;
                            case FADEOUTANDIN:
                            case FADEINANDOUT:
                                cameraScripts.Add((eventTime, MakeNewFadeOutInScript(eventTime, eventData)));
                                break;
                        }
                    }
                }
                CameraTimedScripts = cameraScripts;
            }
        }
        private static (string, string) NormalizeGameAndVenueSource(string game, string venue)
        {
            // The following checks are to normalize the game and venue source names
            // Since GHWT and GHWoR are the same in terms of cameras
            if (venue == GAME_GH5 || venue == GAME_GHWOR)
            {
                venue = GAME_GHWT;
            }
            if (game == GAME_GH5 || game == GAME_GHWOR)
            {
                game = GAME_GHWT;
            }

            return (game, venue);
        }
        private List<(int, QBStructData)> CombinePerformanceScripts()
        {
            var perfScripts = new List<(int, QBStructData)>();
            //QBItem perfScripts = new QBItem(songName, Guitar.PerformanceScript);
            perfScripts.AddRange(Guitar.PerformanceScript);
            perfScripts.AddRange(Rhythm.PerformanceScript);
            if (Game == GAME_GHA)
            {
                perfScripts.AddRange(Aux.PerformanceScript);
            }
            else if (Game != GAME_GH3)
            {
                perfScripts.AddRange(Drums.PerformanceScript);
            }
            perfScripts.AddRange(Vocals.PerformanceScript);
            // Add Perf override function here
            var perfOverride = PerformanceOverrideParsing();
            if (perfOverride != null)
            {
                perfScripts.AddRange(perfOverride);
            }
            perfScripts.AddRange(CameraTimedScripts);
            perfScripts.AddRange(CrowdTimedScripts);
            perfScripts.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            
            return perfScripts;
        }
        public QBItem MakePerformanceScriptsQb(string songName)
        {
            var perfScripts = CombinePerformanceScripts();
            if (!(perfScripts.Count == 0 && PerformanceScripts != null && PerformanceScripts.Items.Count != 0))
            {
                PerformanceScripts = new QBArrayNode();
                foreach (var script in perfScripts)
                {
                    PerformanceScripts.AddStructToArray(script.Item2);
                }
            }
            QBItem PerfScriptQb = new QBItem(songName, PerformanceScripts);
            return PerfScriptQb;
        }
        public List<(int, QBStructData)>? PerformanceOverrideParsing()
        {
            if (!File.Exists(PerfOverride))
            {
                return null;
            }
            string perfText = File.ReadAllText(PerfOverride).Trim();
            if (perfText == "")
            {
                return null;
            }
            perfText = CleanPerfOverride(perfText);
            var (qb, _) = ParseQFile(perfText);
            var perfScripts = new List<(int, QBStructData)>();
            List<string> SkaErrors = new List<string>();

            // This nested method looks icky, but I don't feel like refactoring right now.
            // It works... so it's fine for now.
            foreach (var qbItem in qb)
            {
                if (qbItem is QBItem qbItem2)
                {
                    if (qbItem2.Name.Contains("_performance"))
                    {
                        var qbStructData = qbItem2.Data as QBArrayNode;
                        if (qbStructData != null)
                        {
                            foreach (var perfBlock in qbStructData.Items)
                            {
                                var perfEntry = perfBlock as QBStructData;
                                if (perfEntry != null)
                                {
                                    int timeOrig = (int)perfEntry["time"];
                                    int time = RoundTime(timeOrig);
                                    perfEntry["time"] = time; //Update the time to be on the nearest frame.
                                    var scriptType = perfEntry["scr"].ToString().ToLower();
                                    if (scriptType == "band_playclip")
                                    {
                                        var scrParams = (QBStructData)perfEntry["params"];
                                        var clip = scrParams["clip"].ToString();
                                        var clipQb = QBKey(scrParams["clip"].ToString());
                                        if (!SongClips.ContainsKey(clipQb))
                                        {
                                            throw new ClipNotFoundException($"{clip} at time {timeOrig} referenced in Performance Override, but not defined in Song Scripts!");
                                        }
                                        var currClip = SongClips[clipQb];
                                        currClip.UpdateFromParams(scrParams);
                                        if (!scrParams.StructDict.ContainsKey("timefactor"))
                                        {
                                            if (currClip.TimeFactor == 1.0f)
                                            {
                                                scrParams.AddIntToStruct("timefactor", 1);
                                            }
                                            else
                                            {
                                                scrParams.AddFloatToStruct("timefactor", currClip.TimeFactor);
                                            }
                                        }
                                        float timeFactor = Convert.ToSingle(scrParams["timefactor"]);
                                        if (Game == GAME_GHWT && timeFactor != 1.0f)
                                        {
                                            scrParams.AddIntToStruct("tempomatching", 0);
                                        }
                                        int closeStart = GetClosestCamera(time);
                                        int timeEnd = RoundCamLen(time + currClip.Length);
                                        int closeEnd = GetClosestCamera(timeEnd);
                                        int startChange = time - closeStart;
                                        int endChange = timeEnd - closeEnd;

                                        if (Math.Abs(startChange) > 100)
                                        {
                                            startChange = 0;
                                        }
                                        else
                                        {
                                            time = closeStart;
                                            perfEntry["time"] = time;
                                        }
                                        if (Math.Abs(endChange) > 100)
                                        {
                                            endChange = 0;
                                        }
                                        var startNoRound = startChange * 30f / 1000 * timeFactor;
                                        var endNoRound = endChange * 30f / 1000 * timeFactor;
                                        startChange = (int)Math.Round(startNoRound);
                                        endChange = (int)Math.Round(endNoRound);
                                        int endCameraChange = 0;
                                        try
                                        {
                                            currClip.UpdateFromSkaFile(SkaPath, startChange, endChange, closeStart, closeEnd, out endCameraChange, SkaQbKeys);
                                        }
                                        catch (Exception e)
                                        {
                                            var errors = e.Message.Split(',');
                                            SkaErrors.AddRange(errors);
                                            continue;
                                        }
                                        int newTimeEnd = RoundCamLen(time + currClip.Length);
                                        closeEnd = GetClosestCamera(newTimeEnd);
                                        int moveEnd = RoundCamLen(closeEnd - endCameraChange);
                                        int cameraCheck = RoundCamLen(moveEnd - newTimeEnd);
                                        if (Math.Abs(cameraCheck) <= 100)
                                        {
                                            ChangeCamera(closeEnd, newTimeEnd);
                                        }
                                        // update start and end frame in the perf entry
                                        var clipParams = (QBStructData)perfEntry["params"];
                                        if (clipParams.StructDict.ContainsKey("startframe"))
                                        {
                                            clipParams["startframe"] = currClip.StartFrame;
                                        }
                                        else
                                        {
                                            clipParams.AddIntToStruct("startframe", currClip.StartFrame);
                                        }
                                        if (clipParams.StructDict.ContainsKey("endframe"))
                                        {
                                            clipParams["endframe"] = currClip.EndFrame;
                                        }
                                        else
                                        {
                                            clipParams.AddIntToStruct("endframe", currClip.EndFrame);
                                        }
                                    }
                                    if (Game == GAME_GH5 || Game == GAME_GHWOR)
                                    {
                                        switch (scriptType)
                                        {
                                            case "band_playfacialanim":
                                                FacialTimedScripts.Add((time, perfEntry));
                                                break;
                                            case "band_playloop":
                                                if (Game == GAME_GHWOR)
                                                {
                                                    var parameters = (QBStructData)perfEntry["params"];
                                                    var avatar = (string)parameters["name"];

                                                    string[] genders = { "male", "female" };

                                                    foreach (var gender in genders)
                                                    {
                                                        try
                                                        {
                                                            string loop = parameters[gender] as string;
                                                            if (loop != null && !Gh6Loops[gender][avatar].Contains(loop))
                                                            {
                                                                Gh6Loops[gender][avatar].Add(loop);
                                                            }
                                                        }
                                                        catch
                                                        {
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var newParams = new QBStructData();
                                                    var parameters = (QBStructData)perfEntry["params"];
                                                    foreach (var item in parameters.StructDict)
                                                    {
                                                        if (item.Key == "name")
                                                        {
                                                            newParams.AddQbKeyToStruct(item.Key, (string)item.Value);
                                                        }
                                                    }
                                                    perfEntry["params"] = newParams;
                                                }
                                                goto default;
                                            default:
                                                ScriptTimedEvents.Add((time, perfEntry));
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        perfScripts.Add((time, perfEntry));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (SkaErrors.Count > 0)
            {
                foreach (var error in SkaErrors)
                {
                    var errorList = error.Split(':');
                    AddToErrorList($"Animation file {errorList[1]} referenced in clip {errorList[0]}, but not found in SKA folder");
                }
            }
            return perfScripts;
        }
        private int GetClosestCamera(int time)
        {
            int closest = 0;
            int minDiff = int.MaxValue;
            foreach (var camera in CamerasNotes)
            {
                int diff = Math.Abs(camera.Time - time);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = camera.Time;
                }
            }
            return closest;
        }
        private int GetClosestIntFromList(int integer, List<int> list)
        {
            int closest = 0;
            int minDiff = int.MaxValue;
            foreach (int item in list)
            {
                int diff = Math.Abs(item - integer);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = item;
                }
            }
            return closest;
        }
        private void ChangeCamera(int origTime, int toTime)
        {
            for (int i = 0; i < CamerasNotes.Count; i++)
            {
                if (CamerasNotes[i].Time == origTime)
                {
                    var prevCamera = CamerasNotes[i - 1];
                    var currCamera = CamerasNotes[i];

                    currCamera.Time = toTime;
                    prevCamera.Length = RoundCamLen(currCamera.Time - prevCamera.Time);
                    if (i < CamerasNotes.Count - 1)
                    {
                        var nextCamera = CamerasNotes[i + 1];
                        currCamera.Length = RoundCamLen(nextCamera.Time - currCamera.Time);
                    }
                    break;
                }
            }
        }
        public void SplitPerformanceScripts()
        {
            PerformanceOverrideParsing();
            FacialTimedScripts.AddRange(Guitar.PerformanceScript);
            FacialTimedScripts.AddRange(Rhythm.PerformanceScript);
            FacialTimedScripts.AddRange(Drums.PerformanceScript);
            FacialTimedScripts.AddRange(Vocals.PerformanceScript);

            ScriptTimedEvents.AddRange(CameraTimedScripts);
            ScriptTimedEvents.AddRange(CrowdTimedScripts);

            FacialTimedScripts.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            ScriptTimedEvents.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }
        public string CleanPerfOverride(string perfText)
        {
            string regString = @"^[a-zA-Z]+_performance\s*=\s*\[";
            if (perfText.StartsWith(LEFTBRACE))
            {
                perfText = $"{SongName}_performance = [\n{perfText}\n]";
            }
            else if (Regex.IsMatch(perfText, regString))
            {

            }
            else if (perfText.StartsWith("0x"))
            {
                var firstSpace = perfText.IndexOf(' ');
                var textTillFirstSpace = perfText.Substring(0, firstSpace);
                perfText = perfText.Replace(textTillFirstSpace, $"{SongName}_performance");
            }
            else
            {
                throw new FormatException("No proper performance array found");
            }
            return perfText;
        }
        // Method to generate the scripts for the instrument
        public List<(int, QBStructData)> InstrumentScripts(List<TimedEvent> events, string actor)
        {
            bool isOldGame = Game == GAME_GH3 || Game == GAME_GHA;
            bool isGtrOrSinger = actor == GUITARIST || actor == VOCALIST;
            bool isGtrAndPs2 = actor == GUITARIST && GamePlatform == CONSOLE_PS2;
            var scriptArray = new List<(int, QBStructData)>();
            foreach (var timedEvent in events)
            {
                string eventText;
                if (timedEvent.Event is TextEvent textEvent)
                {
                    eventText = textEvent.Text;
                    var (eventType, eventData) = GetEventData(eventText);
                    int eventTime = GameMilliseconds(timedEvent.Time);
                    switch (eventType)
                    {
                        case STANCE_A:
                        case STANCE_B:
                        case STANCE_C:
                        case STANCE_D:
                            if (!isOldGame)
                            {
                                continue;
                            }
                            scriptArray.Add((eventTime, MakeNewStanceScript(eventTime, actor, eventType, eventData)));
                            break;
                        case BAND_PLAYFACIALANIM:
                            if (isOldGame && !isGtrOrSinger)
                            {
                                continue;
                            }
                            else if (isGtrAndPs2)
                            {
                                continue;
                            }
                            scriptArray.Add((eventTime, MakeNewLipsyncScript(eventTime, actor, eventData)));
                            break;
                        case JUMP:
                        case SPECIAL:
                        case KICK:
                            if (!isOldGame)
                            {
                                continue;
                            }
                            else if (actor == DRUMMER)
                            {
                                continue;
                            }
                            scriptArray.Add((eventTime, MakeNewAnimScript(eventTime, actor, eventType, eventData)));
                            break;
                        // Singer exclusive events
                        case RELEASE:
                        case LONG_NOTE:
                            if (!isOldGame || actor != VOCALIST)
                            {
                                continue;
                            }
                            scriptArray.Add((eventTime, MakeNewAnimScript(eventTime, actor, eventType, eventData)));
                            break;
                        // Guitarist exclusive events
                        case SOLO:
                        case HANDSOFF:
                        case ENDSTRUM:
                            if (!isOldGame || (actor != GUITARIST && actor != BASSIST))
                            {
                                continue;
                            }
                            scriptArray.Add((eventTime, MakeNewAnimScript(eventTime, actor, eventType, eventData)));
                            break;
                        case GUITAR_START:
                        case GUITAR_WALK01:
                            if (!isOldGame || actor != GUITARIST)
                            {
                                continue;
                            }
                            scriptArray.Add((eventTime, MakeNewWalkScript(eventTime, actor, eventType, eventData)));
                            break;
                        default:
                            break;
                    }
                }
            }
            return scriptArray;
        }
        public void ProcessLights(TrackChunk trackChunk)
        {
            // The following checks are to normalize the game and venue source names
            // Since GHWT and GHWoR are the same in terms of lights
            // As are GH3 and GHA
            var venue = VenueSource;
            if (venue == GAME_GH5 || venue == GAME_GHWOR)
            {
                venue = GAME_GHWT;
            }
            else if (venue == GAME_GHA)
            {
                venue = GAME_GH3;
            }

            var game = Game;
            if (game == GAME_GH5 || game == GAME_GHWOR)
            {
                game = GAME_GHWT;
            }
            else if (game == GAME_GHA)
            {
                game = GAME_GH3;
            }

            var lightNotes = trackChunk.GetNotes().ToList();

            if (game != venue)
            {
                lightNotes = SwapLights(lightNotes, game);
            }

            var textEvents = trackChunk.GetTimedEvents().ToList();
            bool hasNote107 = lightNotes.Any(x => x.NoteNumber == 107);
            List<AnimNote> lightAnimNotes = new List<AnimNote>();
            List<ScriptStruct> lightScripts = new List<ScriptStruct>();
            QBArrayNode lightArray;
            if ((Game == GAME_GH3 || Game == GAME_GHA) && hasNote107)
            {
                lightNotes = lightNotes.Where(x => x.NoteNumber != 107).ToList();
                lightArray = Gh3LightsNote107(lightNotes);
            }
            else
            {
                lightArray = new QBArrayNode();
                foreach (var timedEvent in textEvents)
                {
                    string eventText;
                    if (timedEvent.Event is TextEvent textEvent)
                    {
                        eventText = textEvent.Text;
                        var (eventType, eventData) = GetEventData(eventText);
                        int eventTime = GameMilliseconds(timedEvent.Time);
                        if (eventType == LIGHTSHOW_SETTIME)
                        {
                            lightArray.AddStructToArray(MakeNewLightScript(eventTime, eventData));
                        }
                    }
                }
            }

            foreach (MidiData.Note note in lightNotes)
            {
                int noteVal = note.NoteNumber;
                int startTime = GameMilliseconds(note.Time);
                int length = GameMilliseconds(note.EndTime) - startTime;
                int velocity = note.Velocity;
                lightAnimNotes.Add(new AnimNote(startTime, noteVal, length, velocity));
            }
            LightshowNotes = lightAnimNotes;

            if (lightArray.Items.Count > 0)
            {
                LightshowScripts = lightArray;
            }

        }
        private List<MidiData.Note> SwapLights(List<MidiData.Note> notes, string game)
        {
            var toLights = lightSwapDict[game];
            var newNotes = new List<MidiData.Note>();

            bool isGh3 = game == GAME_GH3;
            bool isPyro = false;
            long prevTime = 0;

            foreach (var note in notes)
            {
                if (toLights.TryGetValue(note.NoteNumber, out int newNote))
                {
                    if (isGh3 && isPyro && note.Time == prevTime)
                    {
                        continue;
                    }
                    isPyro = false;
                    newNotes.Add(new MidiData.Note((SevenBitNumber)newNote, note.Length, note.Time));
                    if (newNote == 56)
                    {
                        isPyro = true;
                    }
                    prevTime = note.Time;
                }
            }
            return newNotes;
        }
        private QBArrayNode Gh3LightsNote107(List<MidiData.Note> notes)
        {
            QBArrayNode listScripts = new QBArrayNode();
            int MOOD_MIN = 70;
            int MOOD_MAX = 77;
            int KEY_MIN = 57;
            int KEY_MAX = 58;
            int PYRO_NOTE = 56;
            bool strobeMode = false;
            int prevLen = -1;
            long defaultTick = DefaultTickThreshold * TPB / DefaultTPB;
            long blendReduction = defaultTick; // Always place blends slightly before the note being blended.
            List<MidiData.Chord> chords = GroupNotes(notes, blendReduction);
            foreach (MidiData.Chord lightGroup in chords)
            {
                long blendNoteTime = lightGroup.Time - blendReduction;
                if (blendNoteTime < 0)
                {
                    blendNoteTime = 0;
                }
                long maxLen = 0;
                bool isBlended = false;
                foreach (MidiData.Note note in lightGroup.Notes)
                {
                    if (note.NoteNumber == PYRO_NOTE)
                    {
                        continue;
                    }
                    // Check if note.NoteNumber is in between MOOD_MIN and MOOD_MAX or KEY_MIN and KEY_MAX and set isBlended to true if it is
                    if (note.NoteNumber >= MOOD_MIN && note.NoteNumber <= MOOD_MAX)
                    {
                        isBlended = true;
                    }
                    else if (note.NoteNumber >= KEY_MIN && note.NoteNumber <= KEY_MAX)
                    {
                        isBlended = true;
                    }
                    if (note.NoteNumber == MOOD_MIN) // aka Strobe
                    {
                        strobeMode = true;
                    }
                    else if (note.NoteNumber > MOOD_MIN && note.NoteNumber <= MOOD_MAX) // If not a strobe
                    {
                        strobeMode = false;
                    }
                    maxLen = Math.Max(maxLen, note.Length);
                }
                if (!isBlended)
                {
                    continue;
                }
                if (strobeMode)
                {
                    notes.Add(new MidiData.Note((SevenBitNumber)blendLookup[0], defaultTick, blendNoteTime));
                    prevLen = 0;
                }
                else
                {
                    int startTime = GameMilliseconds(lightGroup.Time);
                    int scriptTime = GameMilliseconds(blendNoteTime);
                    int endTime = GameMilliseconds(lightGroup.Time + maxLen);
                    int length = endTime - startTime;
                    if (length > 1050) // This will make a text event blend time
                    {
                        float lengthFloat = (float)length / 1000;
                        string lengthString = Math.Round(lengthFloat, 3).ToString();
                        if (prevLen != length)
                        {
                            listScripts.AddStructToArray(MakeNewLightScript(scriptTime, lengthString));
                            prevLen = length;
                        }
                    }
                    else
                    {
                        int blendNote = FindBlendNote(blendLookup, length);
                        if (prevLen != blendNote)
                        {
                            notes.Add(new MidiData.Note((SevenBitNumber)blendNote, defaultTick, blendNoteTime));
                            prevLen = blendNote;
                        }
                    }
                }
            }
            notes.Sort((x, y) => x.Time.CompareTo(y.Time));
            return listScripts;
        }
        private int FindBlendNote(Dictionary<int, int> blendLookup, int number)
        {
            int closestKey = int.MinValue;
            foreach (var key in blendLookup.Keys)
            {
                // Check if the key is less than or equal to the given number and closer to the number than the current closest key
                if (key <= number && key > closestKey)
                {
                    closestKey = key;
                }
            }

            // Return the value associated with the closest key, or a default value if no such key was found
            return closestKey != int.MinValue ? blendLookup[closestKey] : default(int);
        }
        private QBStructData MakeNewCameraFovScript(int eventTime, string eventData, string fovTime)
        {
            QBStructData cameraParams = new QBStructData();
            cameraParams.MakeCameraFovParams(eventData, fovTime);
            ScriptStruct cameraScript = new ScriptStruct(eventTime, FOVPULSE, cameraParams);
            return cameraScript.ToStruct();
        }
        private QBStructData MakeNewFadeOutInScript(int eventTime, string eventParams)
        {
            // Default values for fade parameters
            // In order: time, delay, zPriority, alpha, initial delay
            var defaultDict = new Dictionary<string, string>
            {
                { "time", "1.0" },
                { "delay", "0.0" },
                { "zPriority", "0" },
                { "alpha", "1.0" },
                { "initialDelay", "0.0" }
            };
            var defaultDictMap = new Dictionary<int, string>
            {
                {0, "time" },
                {1, "delay" },
                {2, "zPriority" },
                {3, "alpha" },
                {4, "initialDelay" }
            };

            bool isSpecified = false;
            bool isInOrder = false;

            string[] dataSplit = eventParams.Split(' ');
            for (int i = 0; i < dataSplit.Length; i++)
            {
                var data = dataSplit[i];
                if (string.IsNullOrEmpty(data))
                {
                    continue;
                }
                char last_char = data[data.Length - 1];
                // check if last_char is a letter
                if (char.IsLetter(last_char))
                {
                    data = data.Substring(0, data.Length - 1);
                    switch (last_char)
                    {
                        case 't':
                            defaultDict["time"] = data;
                            isSpecified = true;
                            break;
                        case 'd':
                            defaultDict["delay"] = data;
                            isSpecified = true;
                            break;
                        case 'z':
                            defaultDict["zPriority"] = data;
                            isSpecified = true;
                            break;
                        case 'a':
                            defaultDict["alpha"] = data;
                            isSpecified = true;
                            break;
                        case 'i':
                            defaultDict["initialDelay"] = data;
                            isSpecified = true;
                            break;
                        default:
                            throw new FormatException($"Invalid fadeoutandin parameter {last_char} found");
                    }
                }
                else
                {
                    isInOrder = true;
                    defaultDict[defaultDictMap[i]] = data;
                }
            }

            if (isSpecified && isInOrder)
            {
                AddTimedError("Fadeoutandin parameters are in both order and specified format,", "CAMERAS", eventTime);
            }

            // Ensure that dataSplit has 5 elements, filling in from defaultVals where necessary
            string[] finalParams = new string[5];
            for (int i = 0; i < 5; i++)
            {
                finalParams[i] = defaultDict[defaultDictMap[i]];
            }

            QBStructData fadeParams = new QBStructData();
            fadeParams.MakeFadeParams(finalParams);
            ScriptStruct fadeScript = new ScriptStruct(eventTime, FADEOUTANDIN, fadeParams);
            return fadeScript.ToStruct();
        }
        private QBStructData MakeNewLightScript(int eventTime, string eventData)
        {
            QBStructData lightParams = new QBStructData();
            lightParams.MakeLightBlendParams(eventData);
            ScriptStruct lightScript = new ScriptStruct(eventTime, LIGHTSHOW_SETTIME, lightParams);
            return lightScript.ToStruct();
        }
        private QBStructData MakeNewStanceScript(int eventTime, string actor, string eventData, string flagParams)
        {
            QBStructData? animParams = new QBStructData();
            animParams.MakeTwoParams(actor, eventData, STANCE);
            if (flagParams != EMPTYSTRING)
            {
                animParams.AddFlags(flagParams);
            }
            ScriptStruct animScript = new ScriptStruct(eventTime, BAND_CHANGESTANCE, animParams);
            return animScript.ToStruct();
        }
        private QBStructData MakeNewLipsyncScript(int eventTime, string actor, string eventData)
        {
            QBStructData? animParams = new QBStructData();
            animParams.MakeTwoParams(actor, eventData, ANIM);
            ScriptStruct animScript = new ScriptStruct(eventTime, BAND_PLAYFACIALANIM, animParams);
            return animScript.ToStruct();
        }
        private QBStructData MakeNewAnimScript(int eventTime, string actor, string eventType, string flagParams)
        {
            QBStructData? animParams = new QBStructData();
            animParams.MakeTwoParams(actor, eventType, ANIM);
            if (eventType == HANDSOFF)
            {
                flagParams = $"disable_auto_arms {flagParams}".TrimEnd();
            }
            if (flagParams != EMPTYSTRING)
            {
                animParams.AddFlags(flagParams);
            }
            ScriptStruct animScript = new ScriptStruct(eventTime, BAND_PLAYANIM, animParams);
            return animScript.ToStruct();
        }
        private QBStructData MakeNewWalkScript(int eventTime, string actor, string nodeType, string flagParams)
        {
            QBStructData? animParams = new QBStructData();
            animParams.MakeWalkToNode(actor, nodeType, GamePlatform!);
            if (flagParams != EMPTYSTRING)
            {
                animParams.AddFlags(flagParams);
            }
            ScriptStruct animScript = new ScriptStruct(eventTime, BAND_WALKTONODE, animParams);
            return animScript.ToStruct();
        }
        public void ProcessEvents(TrackChunk trackChunk)
        {
            // Create a variable that grabs all text events
            var allEvents = trackChunk.GetTimedEvents().ToList();
            var bandMoments = trackChunk.GetNotes().Where(x => x.NoteNumber == 104).ToList();
            CrowdNotes = new List<AnimNote>();
            Dictionary<string, int> crowdDict;
            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                crowdDict = crowdMapGh3;
            }
            else
            {
                crowdDict = crowdMapWt;
            }
            Markers = new List<Marker>();

            var crowdScripts = new List<(int, QBStructData)>();

            foreach (var timedEvent in allEvents)
            {
                string eventText;
                if (timedEvent.Event is TextEvent textEvent)
                {
                    eventText = textEvent.Text;
                    var (eventType, eventData) = GetEventData(eventText);
                    int eventTime = GameMilliseconds(timedEvent.Time);
                    switch (eventType)
                    {
                        case SECTION_EVENT:
                            bool inDict = SectionNames.SectionNamesDict.TryGetValue(eventData, out string? sectionName);
                            string markerName = inDict ? sectionName! : MakeMarkerNameFromVariable(eventData);
                            Markers.Add(new Marker(eventTime, markerName));
                            break;
                        case CROWD_EVENT:
                            if (crowdDict.TryGetValue(eventData, out int crowdVal))
                            {
                                CrowdNotes.Add(new AnimNote(eventTime, crowdVal, 100, 100));
                                if (eventData == INTENSE) // Add a second note for intense crowd
                                {
                                    CrowdNotes.Add(new AnimNote(eventTime, crowdDict[SURGE_FAST], 100, 100));
                                }
                            }
                            else
                            {
                                switch (eventData)
                                {
                                    case THE_END:
                                        if (Game != GAME_GH3)
                                        {
                                            Markers.Add(new Marker(eventTime, "_ENDOFSONG"));
                                        }
                                        break;
                                    case STARTLIGHTERS:
                                    case STOPLIGHTERS:
                                        if (Game == GAME_GH3 || Game == GAME_GHA)
                                        {
                                            var scriptStruct = new ScriptStruct(eventTime, $"crowd_{eventData}");
                                            crowdScripts.Add((eventTime, scriptStruct.ToStruct()));
                                        }
                                        break;
                                    case STAGEDIVER_JUMP:
                                        if (Game == GAME_GH3)
                                        {
                                            var scriptStruct = new ScriptStruct(eventTime, $"crowd_{eventData}");
                                            crowdScripts.Add((eventTime, scriptStruct.ToStruct()));
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            CrowdTimedScripts = crowdScripts;
            if (Markers.Count == 0)
            {
                Markers = [new Marker(0, "start")];
            }
            foreach (var bandMoment in bandMoments)
            {
                int startTime = GameMilliseconds(bandMoment.Time);
                int endTime = GameMilliseconds(bandMoment.EndTime);
                int length = endTime - startTime;
                BandMoments.AddRange(new List<int> { startTime, length });
            }
        }
        public void ProcessBeat(TrackChunk trackChunk)
        {
            var beatNotes = trackChunk.GetNotes().Where(x => x.NoteNumber == DownbeatNote || x.NoteNumber <= UpbeatNote).ToList();

            int prevDownbeat = 0;
            int currTsNumerator = 0;
            int numeratorCount = 0;

            var newFretbars = new List<int>();
            var newTimeSigs = new List<TimeSig>();

            foreach (MidiData.Note note in beatNotes)
            {
                int noteVal = note.NoteNumber;
                int startTime = GameMilliseconds(note.Time);
                if (noteVal == DownbeatNote)
                {
                    // Add a new time signature if the previous number of beats was not the same as the current number of beats
                    if ((prevDownbeat != 0 && numeratorCount != currTsNumerator) || (prevDownbeat == 0 && numeratorCount != 0))
                    {
                        newTimeSigs.Add(new TimeSig(prevDownbeat, numeratorCount, 4));
                        currTsNumerator = numeratorCount;
                    }
                    prevDownbeat = startTime;
                    numeratorCount = 0;
                }
                newFretbars.Add(startTime);
                numeratorCount++;
            }

            Fretbars = newFretbars;
            TimeSigs = newTimeSigs;
        }
        public static string MakeMarkerNameFromVariable(string inputString)
        {
            string formattedString = inputString.Replace("_", " ");
            formattedString = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedString.ToLower());
            return formattedString;
        }

        private (string eventType, string eventData) GetEventData(string eventText)
        {
            string eventType;
            eventText = eventText.ToLower();
            if (!eventText.StartsWith("[") && !eventText.EndsWith("]"))
            {
                eventType = LYRIC;
                return (eventType, eventText);
            }
            else
            {
                eventText = eventText.Substring(1, eventText.Length - 2);
            }

            string[] flagArray = eventText.Split(' ');

            if (eventText.StartsWith(SECTION_OLD))
            {
                eventType = SECTION_EVENT;
                eventText = eventText.Replace(SECTION_OLD, EMPTYSTRING);
            }
            else if (eventText.StartsWith(SECTION_NEW))
            {
                eventType = SECTION_EVENT;
            }
            else if (eventText.StartsWith(CROWD_CHECK))
            {
                eventType = CROWD_EVENT;
                eventText = eventText.Replace(CROWD_EVENT, EMPTYSTRING);
            }
            else if (eventText.ToLower().StartsWith(SETBLENDTIME) || eventText.ToLower().StartsWith(LIGHTSHOW_SETTIME))
            {
                eventType = LIGHTSHOW_SETTIME;
                var splitText = eventText.Split(' ');
                eventText = splitText[1];
            }
            else if (eventText == MUSIC_START)
            {
                eventType = CROWD_EVENT;
                eventText = MUSIC_START;
            }
            else if (eventText == MUSIC_END)
            {
                eventType = CROWD_EVENT;
                eventText = MUSIC_END;
            }
            else if (eventText == THE_END)
            {
                eventType = CROWD_EVENT;
                eventText = THE_END;
            }
            else if (eventText == CODA)
            {
                eventType = CROWD_EVENT;
                eventText = CODA;
            }
            else if (eventText.StartsWith(STANCE))
            {
                if (Game == GAME_GH3 || Game == GAME_GHA)
                {
                    switch (flagArray[0])
                    {
                        case STANCE_A:
                        case STANCE_B:
                        case STANCE_C:
                            eventType = flagArray[0];
                            break;
                        case STANCE_D:
                            if (Game != GAME_GHA)
                            {
                                eventType = STANCE_A; // Fallback since Stance D is not in GH3
                            }
                            else
                            {
                                eventType = flagArray[0];
                            }
                            break;
                        default:
                            eventType = EMPTYSTRING;
                            break;
                    }
                    eventText = eventText.Replace(flagArray[0], EMPTYSTRING).TrimStart();
                }
                else
                {
                    eventType = EMPTYSTRING;
                }
            }
            else if (eventText.StartsWith(BAND_PLAYFACIALANIM))
            {
                eventType = BAND_PLAYFACIALANIM;
                eventText = eventText.Replace(BAND_PLAYFACIALANIM, EMPTYSTRING).TrimStart();
            }
            else if (eventText.StartsWith(FADEOUTANDIN))
            {
                eventType = FADEOUTANDIN;
                eventText = eventText.Replace(FADEOUTANDIN, EMPTYSTRING).TrimStart();
            }
            else if (Regex.IsMatch(eventText, CAMERA_FX_REGEX))
            {
                eventText = "10";
                if (flagArray.Length > 1 && int.TryParse(flagArray[1], out int _))
                {
                    eventText = flagArray[1];
                }
                eventType = flagArray[0];
            }
            else
            {
                switch (flagArray[0])
                {
                    case JUMP:
                    case SPECIAL:
                    case KICK:
                    // Singer events
                    case RELEASE:
                    case LONG_NOTE:
                    // Guitarist events
                    case SOLO:
                    case HANDSOFF:
                    case ENDSTRUM:
                    case GUITAR_START:
                    case GUITAR_WALK01:
                        eventType = flagArray[0];
                        eventText = eventText.Replace(flagArray[0], EMPTYSTRING).TrimStart();
                        break;
                    default:
                        eventType = EMPTYSTRING;
                        break;
                }

            }
            return (eventType, eventText);
        }

        public class CameraGenerator
        {
            private List<int> camCycleGh3 = new List<int> { 84, 97, 89, 88, 84, 85, 110, 100, 103, 110 };
            private List<int> camCycleGha = new List<int> { 34, 14, 21, 27, 82, 33, 15, 22, 26, 53 };
            private List<int> camCycleWoR = new List<int> { 10, 18, 23, 15, 43, 11, 19, 24, 16, 30 };
            private List<AnimNote> cameras = new List<AnimNote>();

            public List<AnimNote> AutoGenCamera(List<int> fretbars, string game, string venueSource)
            {
                var (newGame, _) = NormalizeGameAndVenueSource(game, venueSource);
                List<int> cameraMap;
                switch (newGame)
                {
                    case GAME_GH3:
                        cameraMap = camCycleGh3;
                        break;
                    case GAME_GHA:
                        cameraMap = camCycleGha;
                        break;
                    case GAME_GHWT:
                        cameraMap = camCycleWoR;
                        break;
                    default:
                        throw new NotImplementedException("Unknown game found");
                }

                for (int i = 0; i < fretbars.Count; i++)
                {
                    int noteStart = RoundTime(fretbars[i]);
                    int noteLen;
                    if (i < fretbars.Count - 1)
                    {
                        noteLen = RoundCamLen(RoundTime(fretbars[i + 1]) - noteStart);
                    }
                    else
                    {
                        noteLen = 20000; // Default length when there's no next note
                    }

                    int camIndex = cameraMap[i % cameraMap.Count];
                    int velocity = 100;

                    cameras.Add(new AnimNote(noteStart, camIndex, noteLen, velocity));
                }

                return cameras;
            }
            public class LightShowGenerator
            {
                public List<AnimNote> AutoGenLightshow(List<int> fretbars, List<Marker> markers, bool gh3 = false)
                {
                    List<AnimNote> lightshow = new List<AnimNote>();
                    if (gh3)
                    {
                        lightshow.AddRange(new List<AnimNote> { new AnimNote(0, 39, 25, 100), new AnimNote(0, 76, 25, 100) });
                    }
                    else
                    {
                        lightshow.AddRange(new List<AnimNote> { new AnimNote(0, 39, 25, 100), new AnimNote(0, 84, 25, 100) });
                    }

                    Dictionary<int, int> gh3Lights = new Dictionary<int, int>
                    {
                        { 74, 74 },
                        { 75, 74 },
                        { 76, 75 },
                        { 77, 74 },
                        { 78, 75 },
                        { 79, 78 }
                    };

                    for (int i = 0; i < markers.Count; i++)
                    {
                        int nextTime = (i < markers.Count - 1) ? markers[i + 1].Time : 0;
                        int light;
                        int lightSteps = DetermineLightStepsAndLight(markers[i].Text, out light);

                        if (gh3 && gh3Lights.ContainsKey(light))
                        {
                            light = gh3Lights[light];
                        }

                        lightshow.Add(new AnimNote(markers[i].Time, light, 25, 100));

                        if (lightSteps != 0)
                        {
                            int currentSteps = 1;
                            foreach (var fret in fretbars.Where(x => x > markers[i].Time && x < nextTime))
                            {
                                if (currentSteps % lightSteps == 0)
                                {
                                    int lightTime = fret;
                                    if (gh3)
                                    {
                                        lightshow.Add(new AnimNote(lightTime, 58, 25, 100));
                                    }
                                    else
                                    {
                                        lightshow.Add(new AnimNote(lightTime, 58, 25, 100));
                                    }
                                }
                                currentSteps++;
                            }
                        }
                    }

                    return lightshow;
                }

                private int DetermineLightStepsAndLight(string markerText, out int light)
                {
                    Regex regex = new Regex(
                    "(intro( [0-9]?[a-z]?)?)|" +
                    "(verse( [0-9]?[a-z]?)?)|" +
                    "(pre-?chorus( [0-9]?[a-z]?)?)|" +
                    "(chorus( [0-9]?[a-z]?)?)|" +
                    "(bridge( [0-9]?[a-z]?)?)|" +
                    "(main riff( [0-9]?[a-z]?)?)" +
                    "(solo)",
                    RegexOptions.IgnoreCase);

                    Match match = regex.Match(markerText);

                    if (match.Success)
                    {
                        var matched = match.Value.ToLower().Split(' ')[0];
                        switch (matched)
                        {
                            case "intro":
                                light = 79;  // Prelude
                                return 8;
                            case "verse":
                                light = 78;  // Exposition
                                return 4;
                            case "pre-chorus":
                                light = 74;  // Falling Action
                                return 2;
                            case "chorus":
                                light = 75;  // Climax
                                return 1;
                            case "bridge":
                                light = 77;  // Resolution
                                return 2;
                            case "main riff":
                                light = 76;  // Tension
                                return 2;
                            case "solo":
                                light = 75;  // Climax
                                return 2;
                            default:
                                light = 78;  // Default Exposition
                                return 4;
                        }
                    }
                    else
                    {
                        light = 78;  // Default if no matches
                        return 4;
                    }
                }
            }
            public class DrumAnimGenerator
            {
                public List<AnimNote> AutoGenDrumAnims(Difficulty drums)
                {
                    List<AnimNote> drumAnims = new List<AnimNote>();
                    var zeroToFive = new List<int> { 1, 2, 4, 8, 16, 32 };
                    var animLookup = new Dictionary<int, int>()
                    {
                        {1, 74},
                        {2, 77},
                        {4, 78},
                        {8, 75},
                        {16, 80},
                        {32, 73}
                    };
                    if (drums.PlayNotes == null)
                    {
                        return drumAnims;
                    }
                    foreach (var drumNote in drums.PlayNotes)
                    {
                        foreach (var note in zeroToFive)
                        {
                            if ((drumNote.Note & note) == note)
                            {
                                int animValue;
                                if (animLookup.TryGetValue(note, out animValue))
                                {
                                    drumAnims.Add(new AnimNote(drumNote.Time, animValue, drumNote.Length, 100));
                                    drumAnims.Add(new AnimNote(drumNote.Time, animValue - 13, drumNote.Length, 100));
                                }
                            }
                        }
                    }
                    return drumAnims;
                }
            }
        }
        public static int RoundTime(int entry)
        {
            string entryStr = entry.ToString("D6");
            int timeTrunc = int.Parse(entryStr.Substring(4, 2));

            switch (timeTrunc)
            {
                case 0:
                case 33:
                case 67:
                    return entry;
                case 99:
                    return entry + 1;
                case 1:
                    return int.Parse(entryStr.Substring(0, 4) + "00");
                default:
                    if (timeTrunc <= 34)
                        return int.Parse(entryStr.Substring(0, 4) + "33");
                    else if (timeTrunc <= 68)
                        return int.Parse(entryStr.Substring(0, 4) + "67");
                    else
                        return int.Parse(entryStr.Substring(0, 4) + "99") + 1;
            }
        }
        public int RoundTimeMills(long entry)
        {
            return RoundTime(GameMilliseconds(entry));
        }
        public static int RoundCamLen(int lengthVal)
        {
            string lengthStr = lengthVal.ToString();
            char lastChar = lengthStr[lengthStr.Length - 1];

            if (lastChar == '4')
                return lengthVal - 1;
            else if (lastChar == '6')
                return lengthVal + 1;
            else
                return lengthVal;
        }
        public class Instrument
        {
            public Difficulty Easy { get; set; } = new Difficulty(EASY);
            public Difficulty Medium { get; set; } = new Difficulty(MEDIUM);
            public Difficulty Hard { get; set; } = new Difficulty(HARD);
            public Difficulty Expert { get; set; } = new Difficulty(EXPERT);
            public List<StarPower>? FaceOffStar { get; set; } = null; // In-game. Based off of the easy chart.
            public QBArrayNode FaceOffP1 { get; set; } = new QBArrayNode();
            public QBArrayNode FaceOffP2 { get; set; } = new QBArrayNode();
            public QBArrayNode? DrumFill { get; set; }
            public QBArrayNode SoloMarker { get; set; }
            public List<AnimNote> AnimNotes { get; set; } = new List<AnimNote>();
            public List<(int, QBStructData)> PerformanceScript { get; set; } = new List<(int, QBStructData)>();
            private string Q_Game { get; set; } = GAME_GHWT;
            internal List<MidiData.Note> StarPowerPhrases { get; set; }
            internal List<MidiData.Note> BattleStarPhrases { get; set; }
            internal List<MidiData.Note> FaceOffStarPhrases { get; set; }
            internal string TrackName { get; set; }
            internal SongQbFile _songQb { get; set; }
            public bool HasExpertPlus { get
                {
                    return Expert.HasExpertPlus;
                }
            }
            public Instrument(string trackName) // Default empty instrument
            {
                TrackName = trackName;
            }
            public Instrument(string trackName, string songName, Dictionary<string, QBItem> songData, SongQbFile songQb, string qGame = GAME_GHWT)
            {
                Q_Game = qGame;
                TrackName = trackName;
                _songQb = songQb;
                bool failed = false;
                // Check faceoff data before the loop
                string faceOffStarLookup = $"{songName}{trackName}_faceoffstar";
                string faceOffP1Lookup = $"{songName}{trackName}_faceoffp1";
                string faceOffP2Lookup = $"{songName}{trackName}_faceoffp2";
                QBArrayNode? faceOffStar = CheckAndRetrieveNode(songData, faceOffStarLookup, _songQb.AddToWarningList, ref failed);
                QBArrayNode? faceOffP1 = CheckAndRetrieveNode(songData, faceOffP1Lookup, _songQb.AddToWarningList, ref failed);
                QBArrayNode? faceOffP2 = CheckAndRetrieveNode(songData, faceOffP2Lookup, _songQb.AddToWarningList, ref failed);

                if (faceOffP1 != null)
                {
                    FaceOffP1 = faceOffP1;
                }
                if (faceOffP2 != null)
                {
                    FaceOffP2 = faceOffP2;
                }

                if (TrackName == DRUMS_NAME)
                {
                    string drumFillLookup = $"{songName}_expert_drumfill";
                    DrumFill = CheckAndRetrieveNode(songData, drumFillLookup, _songQb.AddToWarningList, ref failed);
                }

                // Iterate over difficulties and process data
                string[] diffs = { EASY, MEDIUM, HARD, EXPERT };
                foreach (string diff in diffs)
                {

                    string instLookup = $"{songName}_song{trackName}_{diff}";
                    string starLookup = $"{songName}{trackName}_{diff}_star";
                    string starBmLookup = $"{songName}{trackName}_{diff}_starbattlemode";
                    string tappingLookup = $"{songName}{trackName}_{diff}_tapping";

                    QBArrayNode? instData = CheckAndRetrieveNode(songData, instLookup, _songQb.AddToErrorList, ref failed, true);
                    QBArrayNode? starData = CheckAndRetrieveNode(songData, starLookup, _songQb.AddToWarningList, ref failed);
                    QBArrayNode? starBmData = CheckAndRetrieveNode(songData, starBmLookup, _songQb.AddToWarningList, ref failed);
                    QBArrayNode? tapNotes = CheckAndRetrieveNode(songData, tappingLookup, _songQb.AddToWarningList, ref failed);

                    if (instData == null)
                    {
                        // Assuming this is a GH5+ song. Else the song is invalid and will fail later.
                        continue;
                    }

                    switch (diff)
                    {
                        case EASY:
                            Easy = new Difficulty(EASY, instData!, starData, starBmData, tapNotes, faceOffStar, Q_Game);
                            break;
                        case MEDIUM:
                            Medium = new Difficulty(MEDIUM, instData!, starData, starBmData, tapNotes, faceOffStar, Q_Game);
                            break;
                        case HARD:
                            Hard = new Difficulty(HARD, instData!, starData, starBmData, tapNotes, faceOffStar, Q_Game);
                            break;
                        case EXPERT:
                            Expert = new Difficulty(EXPERT, instData!, starData, starBmData, tapNotes, faceOffStar, Q_Game);
                            break;
                    }
                }
            }
            public Instrument(string trackName, Dictionary<string, Dictionary<string, List<int>>> instData, SongQbFile songQb, bool expertPlus = false)
            {
                TrackName = trackName;
                _songQb = songQb;
                foreach (var (diff, diffData) in instData)
                {
                    switch (diff)
                    {
                        case EASY:
                            Easy = new Difficulty(EASY, TrackName, diffData);
                            break;
                        case MEDIUM:
                            Medium = new Difficulty(MEDIUM, TrackName, diffData);
                            break;
                        case HARD:
                            Hard = new Difficulty(HARD, TrackName, diffData);
                            break;
                        case EXPERT:
                            Expert = new Difficulty(EXPERT, TrackName, diffData, expertPlus);
                            break;
                    }
                }
            }
            public void SetTrackName(string trackName)
            {
                TrackName = trackName;
            }
            private QBArrayNode? CheckAndRetrieveNode(Dictionary<string, QBItem> songData, string lookupKey, Action<string>? logAction, ref bool failed, bool failIfNull = false)
            {
                if (songData.ContainsKey(lookupKey))
                {
                    var data = songData[lookupKey].Data as QBArrayNode;
                    if (data == null)
                    {
                        if (logAction != null && failIfNull)
                        {
                            logAction($"Data for {lookupKey} is not an array");
                            failed = true;
                        }
                    }
                    return data;
                }
                return null;
            }
            public void MakeInstrument(TrackChunk trackChunk, SongQbFile songQb, bool drums = false)
            {

                if (trackChunk == null || songQb == null)
                {
                    throw new ArgumentNullException("trackChunk or songQb is null");
                }
                _songQb = songQb;

                int drumsMode = !drums ? 0 : 1;
                // Create performance scripts for the instrument
                var timedEvents = trackChunk.GetTimedEvents().ToList();
                var textEvents = timedEvents.Where(e => e.Event is TextEvent).ToList();

                Dictionary<MidiTheory.NoteName, int> noteDict;
                Dictionary<int, int> animDict = new Dictionary<int, int>();
                Dictionary<int, int> drumAnimDict = new Dictionary<int, int>();
                List<TimedEvent>? sysExEvents = null;

                int openNotes = Game == GAME_GH3 || Game == GAME_GHA ? 0 : 1;
                int easyOpens;
                if (openNotes == 0)
                {
                    easyOpens = 0;
                    if (Game == GAME_GH3 && songQb.Gh3Plus)
                    {
                        openNotes = 1; // GH3+ has open notes, so we set it to 1
                        sysExEvents = timedEvents.Where(e => e.Event is NormalSysExEvent).ToList();
                    }
                }
                else
                {
                    easyOpens = songQb.EasyOpens ? 1 : 0;
                }


                if (Game == GAME_GH3 || Game == GAME_GHA)
                {
                    noteDict = Gh3Notes;
                    try
                    {
                        animDict = leftHandMappingsGh3[TrackName];
                    }
                    catch (KeyNotFoundException)
                    {
                        animDict = leftHandMappingsGh3[RHYTHM_NAME];
                    }
                    drumAnimDict = drumKeyMapRB_gh3;
                }
                else if (drums)
                {
                    noteDict = Gh4Drums;
                    drumAnimDict = Game == GAME_GHWOR ? drumKeyMapRB_wor : drumKeyMapRB_wt;
                }
                else
                {
                    sysExEvents = timedEvents.Where(e => e.Event is NormalSysExEvent).ToList();
                    noteDict = Gh4Notes;
                    try
                    {
                        animDict = leftHandMappingsWt[TrackName];
                    }
                    catch (KeyNotFoundException)
                    {
                        animDict = leftHandMappingsWt[""];
                    }
                    if (easyOpens == 1)
                    {
                        try
                        {
                            animDict[58] = animDict[59];
                            animDict.Remove(59);
                        }
                        catch
                        {
                            // Nothing to do here
                        }
                    }

                }

                var sysexTaps = new List<StarPower>();
                var sysexOpens = new Dictionary<int, List<StarPower>>
                {
                    { 0, new List<StarPower>() },
                    { 1, new List<StarPower>() },
                    { 2, new List<StarPower>() },
                    { 3, new List<StarPower>() }
                };
                if (sysExEvents != null)
                {
                    (sysexTaps, sysexOpens) = SplitSysEx(sysExEvents);
                }

                // Extract all notes from the track once
                var allNotes = trackChunk.GetNotes().ToList();

                // Extract Face-Off Notes
                var faceOffP1Notes = allNotes.Where(x => x.NoteNumber == FaceOffP1Note).ToList();
                FaceOffP1 = ProcessOtherSections(faceOffP1Notes, songQb);
                var faceOffP2Notes = allNotes.Where(x => x.NoteNumber == FaceOffP2Note).ToList();
                FaceOffP2 = ProcessOtherSections(faceOffP2Notes, songQb);


                // TapNotes = ProcessOtherSections(tapNotes, songQb, isTapNote:true);



                // Create performance scripts for the instrument, excludes the drummer if GH3 or GHA
                if (((Game == GAME_GH3 || Game == GAME_GHA) && TrackName != DRUMS_NAME) || (Game != GAME_GH3 && Game != GAME_GHA))
                {
                    try
                    {
                        PerformanceScript = songQb.InstrumentScripts(textEvents, ActorNameFromTrack[TrackName]);
                    }
                    catch
                    {
                        // Nothing to do here
                    }
                }

                // Extract Star Power, BM Star, and FO Star
                StarPowerPhrases = allNotes.Where(x => x.NoteNumber == StarPowerNote).ToList();
                BattleStarPhrases = allNotes.Where(x => x.NoteNumber == BattleStarNote).ToList();
                if (BattleStarPhrases.Count == 0)
                {
                    BattleStarPhrases = StarPowerPhrases;
                }
                FaceOffStarPhrases = allNotes.Where(x => x.NoteNumber == FaceOffStarNote).ToList();
                if (FaceOffStarPhrases.Count == 0)
                {
                    FaceOffStarPhrases = StarPowerPhrases;
                }

                if (!drums)
                {
                    AnimNotes = InstrumentAnims(allNotes, GuitarAnimStart, GuitarAnimEnd, animDict, songQb);
                    // Process notes for each difficulty level
                    Easy.ProcessDifficultyGuitar(allNotes, EasyNoteMin, EasyNoteMax, noteDict, easyOpens, songQb, StarPowerPhrases, BattleStarPhrases, FaceOffStarPhrases, sysexTaps: sysexTaps, sysexOpens: sysexOpens[0], trackName: TrackName);
                    Medium.ProcessDifficultyGuitar(allNotes, MediumNoteMin, MediumNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases, sysexTaps: sysexTaps, sysexOpens: sysexOpens[1], trackName: TrackName);
                    Hard.ProcessDifficultyGuitar(allNotes, HardNoteMin, HardNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases, sysexTaps: sysexTaps, sysexOpens: sysexOpens[2], trackName: TrackName);
                    Expert.ProcessDifficultyGuitar(allNotes, ExpertNoteMin, ExpertNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases, sysexTaps: sysexTaps, sysexOpens: sysexOpens[3], trackName: TrackName);
                }
                else
                {
                    var drumFillNotes = allNotes.Where(x => x.NoteNumber == TapNote).ToList();
                    AnimNotes = InstrumentAnims(allNotes, DrumAnimStart, DrumAnimEnd, drumAnimDict, songQb, true);
                    DrumFill = ProcessStartEndArrays(drumFillNotes, songQb);
                    // Process notes for each difficulty level
                    Easy.ProcessDifficultyDrums(allNotes, EasyNoteMin, EasyNoteMax + 1, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases, FaceOffStarPhrases);
                    Medium.ProcessDifficultyDrums(allNotes, MediumNoteMin, MediumNoteMax + 1, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases);
                    Hard.ProcessDifficultyDrums(allNotes, HardNoteMin, HardNoteMax + 1, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases);
                    Expert.ProcessDifficultyDrums(allNotes, ExpertNoteMin, ExpertNoteMax + 1, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                }


                if (Hard.PlayNotes.Count == 0)
                {
                    Hard.PlayNotes = Expert.PlayNotes; // If Hard has no notes, use Expert's notes
                }
                if (Medium.PlayNotes.Count == 0)
                {
                    Medium.PlayNotes = Hard.PlayNotes; // If Medium has no notes, use Hard's notes
                }
                if (Easy.PlayNotes.Count == 0)
                {
                    Easy.PlayNotes = Medium.PlayNotes; // If Easy has no notes, use Medium's notes
                }

                if (Game == GAME_GHWT && GamePlatform == CONSOLE_PC)
                {
                    SoloMarker = ProcessStartEndArrays(allNotes.Where(x => x.NoteNumber == SoloNote).ToList(), songQb, true);
                }


                FaceOffStar = Easy.FaceOffStar;
            }
            private (List<StarPower>, Dictionary<int, List<StarPower>>) SplitSysEx(List<TimedEvent>? sysExEvents)
            {
                var sysexTaps = new List<StarPower>();

                var easyOpens = new List<StarPower>();
                var mediumOpens = new List<StarPower>();
                var hardOpens = new List<StarPower>();
                var expertOpens = new List<StarPower>();

                var opensDict = new Dictionary<int, List<StarPower>>()
                {
                    { 0, easyOpens },
                    { 1, mediumOpens },
                    { 2, hardOpens },
                    { 3, expertOpens }
                };

                var currOpenDict = new Dictionary<int, StarPower>()
                {
                    { 0, new StarPower()},
                    { 1, new StarPower()},
                    { 2, new StarPower()},
                    { 3, new StarPower()}
                };

                bool tapOn = false;

                bool easyOn = false;
                bool mediumOn = false;
                bool hardOn = false;
                bool expertOn = false;

                // Just for comparing
                var openOnDict = new Dictionary<int, bool>()
                {
                    { 0, easyOn },
                    { 1, mediumOn },
                    { 2, hardOn },
                    { 3, expertOn }
                };

                var currTap = new StarPower();
                foreach (var sysEx in sysExEvents)
                {
                    if (sysEx.Event is NormalSysExEvent normalSysEx)
                    {
                        var data = normalSysEx.Data;
                        // Extract the first three bytes
                        byte[] sysHead = new byte[3];
                        Array.Copy(data, 0, sysHead, 0, 3);
                        // Compare them to the expected values
                        if (!(sysHead[0] == 80 && sysHead[1] == 83 && sysHead[2] == 0))
                        {
                            continue; // Continue the enclosing loop
                        }
                        var sysexType = data[5];
                        var diff = data[4];
                        bool dataOn = data[6] == 1 ? true : false;
                        var time = (int)sysEx.Time;
                        // Tap Notes
                        if (diff > 2 && sysexType == 4)
                        {
                            if (dataOn && !tapOn)
                            {
                                currTap.SetTime(time);
                                tapOn = true;
                            }
                            else if (!dataOn && tapOn)
                            {
                                currTap.SetLength(time);
                                sysexTaps.Add(currTap);
                                currTap = new StarPower();
                                tapOn = false;
                            }
                            else
                            {
                                _songQb.AddTimedError("Multiple SysEx Tap on/off events found in a row", trackNames[TrackName], sysEx.Time);
                            }
                        }
                        // Open Notes
                        else if (sysexType == 1)
                        {
                            if (dataOn && !openOnDict[diff])
                            {
                                currOpenDict[diff].SetTime(time);
                                opensDict[diff].Add(currOpenDict[diff]);
                                openOnDict[diff] = true;
                            }
                            else if (!dataOn && openOnDict[diff])
                            {
                                currOpenDict[diff].SetLength(time);
                                openOnDict[diff] = false;
                                currOpenDict[diff] = new StarPower();
                            }
                            else
                            {
                                _songQb.AddTimedError("Multiple SysEx Open on/off events found in a row", trackNames[TrackName], sysEx.Time);
                            }
                        }
                    }
                }
                return (sysexTaps, opensDict);
            }
            private List<AnimNote> InstrumentAnims(List<MidiData.Note> allNotes, int minNote, int maxNote, Dictionary<int, int> animDict, SongQbFile songQb, bool allowMultiTime = false)
            {
                int AnimNoteMin = 22;
                List<AnimNote> animNotes = new List<AnimNote>();
                var notes = allNotes.Where(n => n.NoteNumber >= minNote && n.NoteNumber <= maxNote && n.NoteNumber != DrumHiHatOpen).ToList();
                var hihatNotes = allNotes.Where(n => n.NoteNumber == DrumHiHatOpen).ToList();
                var prevTime = 0;
                var prevNote = 0;
                foreach (MidiData.Note note in notes)
                {
                    if (!animDict.ContainsKey(note.NoteNumber))
                    {
                        continue;
                    }
                    int noteVal = animDict[note.NoteNumber];
                    int practiceNote = 0;
                    if (note.NoteNumber == DrumHiHatLeft || note.NoteNumber == DrumHiHatRight)
                    {
                        if (songQb.IsInTimeRange(note.Time, hihatNotes))
                        {
                            noteVal++;
                        }
                    }
                    int startTime = (int)Math.Round(songQb.TicksToMilliseconds(note.Time));
                    int endTime = (int)Math.Round(songQb.TicksToMilliseconds(note.EndTime));
                    int length = endTime - startTime;
                    if (length < 10)
                    {
                        length = 10;
                    }
                    else if (length > 65535) // More for WoR since length is stored in a 16-bit short
                    {
                        length = 65535;
                    }
                    if (startTime == prevTime)
                    {
                        if (allowMultiTime && note.NoteNumber != prevNote)
                        {
                            animNotes.Add(new AnimNote(startTime, noteVal, length, note.Velocity));
                        }
                    }
                    else
                    {
                        animNotes.Add(new AnimNote(startTime, noteVal, length, note.Velocity));
                    }
                    if (allowMultiTime && note.NoteNumber >= 22) // Basically all notes below 22 do not need to be processed for practice mode
                    {
                        (bool addPractice, practiceNote) = AddPracticeNote(animNotes, noteVal);
                        if (addPractice)
                        {
                            animNotes.Add(new AnimNote(startTime, practiceNote, length, note.Velocity));
                        }
                    }
                    // Test for WoR sometime
                    prevNote = note.NoteNumber;
                    prevTime = startTime;
                }
                return animNotes;
            }
            private (bool addPractice, int practiceNote) AddPracticeNote(List<AnimNote> list, int noteVal)
            {
                int LeftHandGh3 = 47;
                int RightHandGh3 = 59;
                int LeftHandWor = 22;
                int RightHandWor = 85;
                switch (Game)
                {
                    case GAME_GH3:
                    case GAME_GHA:
                        if (noteVal == 70) // Count-in note
                        {
                            return (false, 0);
                        }
                        if (noteVal < LeftHandGh3)
                        {
                            return (true, noteVal + 24);
                        }
                        else if (noteVal < RightHandGh3)
                        {
                            return (true, noteVal + 12);
                        }
                        else
                        {
                            throw new NotImplementedException("Unknown note value found");
                        }
                    case GAME_GHWT:
                    case GAME_GH5:
                        return (true, noteVal - 13);
                    case GAME_GHWOR:
                        if (noteVal < LeftHandWor)
                        {
                            return (true, noteVal + 86);
                        }
                        else if (noteVal < RightHandWor)
                        {
                            return (true, noteVal + 56);
                        }
                        else
                        {
                            return (false, 0);
                        }
                    default:
                        throw new NotImplementedException("Unknown game found");
                }
            }
            public void CheckForDuplicates()
            {
                List<string> duplicates = new List<string>();

                foreach (var diff in new Difficulty[] { Easy, Medium, Hard, Expert })
                {
                    Dictionary<int, List<PlayNote>> duplicateDiff = new Dictionary<int, List<PlayNote>>();
                    int prevTime = -500;
                    if (diff.PlayNotes == null)
                    {
                        continue;
                    }
                    foreach (var note in diff.PlayNotes)
                    {
                        if (!duplicateDiff.ContainsKey(note.Time))
                        {
                            duplicateDiff[note.Time] = [note];
                        }
                        else
                        {
                            duplicateDiff[note.Time].Add(note);
                        }
                        var prevDiff = Math.Abs(note.Time - prevTime);
                        if (prevDiff < 16 && prevDiff != 0)
                        {
                            // 16ms is the smallest time difference possible
                            duplicateDiff[prevTime].Add(note);
                        }
                        prevTime = note.Time;
                    }
                    foreach (var key in duplicateDiff.Keys)
                    {
                        if (duplicateDiff[key].Count > 1)
                        {
                            var sb = new StringBuilder();
                            var value = duplicateDiff[key];
                            sb.Append($"{diff.diffName}, ");
                            for (int i = 0; i < value.Count; i++)
                            {
                                var note = value[i];
                                var keyTime = TimeSpan.FromMilliseconds(note.Time).ToString(@"mm\:ss\.fff");
                                sb.Append($"{keyTime}, ");
                                sb.Append($"{note.NoteColor}, {note.Length}");
                                if (i < value.Count - 1)
                                {
                                    sb.Append(", ");
                                }
                            }
                            _songQb.AddToErrorList(sb.ToString());
                        }
                    }
                }

            }
            public void CheckForOverlaps()
            {
                List<string> overlaps = new List<string>();

                foreach (var diff in new Difficulty[] { Easy, Medium, Hard, Expert })
                {
                    if (diff.PlayNotes == null)
                    {
                        continue;
                    }
                    var notes = diff.PlayNotes;
                    for (int i = 0; i < notes.Count - 1; i++)
                    {
                        var note = notes[i];
                        var nextNote = notes[i + 1];
                        if (note.Time + note.Length > nextNote.Time && note.Note > nextNote.Note)
                        {
                            Console.WriteLine($"{diff.diffName}, {note.Time}, {note.Length}");
                        }
                    }
                }
            }

            public ((int baseScore, int simScore) easy, (int baseScore, int simScore) medium, (int baseScore, int simScore) hard, (int baseScore, int simScore) expert) GetBaseScore(List<int> fretbars)
            {
                var (x_base_score, x_sim_score) = Expert.GetSongScores(fretbars);
                var (h_base_score, h_sim_score) = Hard.GetSongScores(fretbars);
                var (m_base_score, m_sim_score) = Medium.GetSongScores(fretbars);
                var (e_base_score, e_sim_score) = Easy.GetSongScores(fretbars);
                return ((e_base_score, e_sim_score), (m_base_score, m_sim_score), (h_base_score, h_sim_score), (x_base_score, x_sim_score));
            }
            public List<QBItem> ProcessQbEntriesGH3(string name, bool blankBM = true)
            {
                var list = new List<QBItem>();
                string playName = $"{name}_song{TrackName}";
                string starName = $"{name}{TrackName}";

                bool gh3Plus = false;
                if (_songQb != null)
                {
                    gh3Plus = _songQb.Gh3Plus;
                }
                list.Add(Easy.CreateGH3Notes(playName, gh3Plus));
                list.Add(Medium.CreateGH3Notes(playName, gh3Plus));
                list.Add(Hard.CreateGH3Notes(playName, gh3Plus));
                list.Add(Expert.CreateGH3Notes(playName, gh3Plus));
                list.Add(Easy.CreateStarPowerPhrases(starName));
                list.Add(Medium.CreateStarPowerPhrases(starName));
                list.Add(Hard.CreateStarPowerPhrases(starName));
                list.Add(Expert.CreateStarPowerPhrases(starName));
                list.Add(Easy.CreateBattleStarPhrases(starName, blankBM));
                list.Add(Medium.CreateBattleStarPhrases(starName, blankBM));
                list.Add(Hard.CreateBattleStarPhrases(starName, blankBM));
                list.Add(Expert.CreateBattleStarPhrases(starName, blankBM));

                return list;
            }
            public List<QBItem> ProcessQbEntriesGHWT(string name, string console = CONSOLE_PC)
            {
                var list = new List<QBItem>();
                string playName = $"{name}_song{TrackName}";
                string starName = $"{name}{TrackName}";
                list.Add(Easy.CreateGHWTNotes(playName));
                list.Add(Medium.CreateGHWTNotes(playName));
                list.Add(Hard.CreateGHWTNotes(playName));
                list.Add(Expert.CreateGHWTNotes(playName));
                list.Add(Easy.CreateStarPowerPhrases(starName));
                list.Add(Medium.CreateStarPowerPhrases(starName));
                list.Add(Hard.CreateStarPowerPhrases(starName));
                list.Add(Expert.CreateStarPowerPhrases(starName));
                list.Add(Easy.CreateBattleStarPhrases(starName, false));
                list.Add(Medium.CreateBattleStarPhrases(starName, false));
                list.Add(Hard.CreateBattleStarPhrases(starName, false));
                list.Add(Expert.CreateBattleStarPhrases(starName, false));
                list.Add(Easy.CreateTapPhrases(starName));
                list.Add(Medium.CreateTapPhrases(starName));
                list.Add(Hard.CreateTapPhrases(starName));
                list.Add(Expert.CreateTapPhrases(starName));
                list.Add(Easy.CreateWhammyController(starName));
                list.Add(Medium.CreateWhammyController(starName));
                list.Add(Hard.CreateWhammyController(starName));
                list.Add(Expert.CreateWhammyController(starName));
                list.Add(Easy.CreateFaceOffStar(starName));
                list.AddRange(MakeFaceOffQb(name));
                if (console == CONSOLE_PC)
                {
                    list.AddRange(CreateSoloMarker(starName));
                }

                return list;
            }
            public byte[] ProcessNewNotes(ref int entries)
            {
                bool ghwor = Game == GAME_GHWOR;
                string name = TrackName.Replace("_", "");
                switch (name)
                {

                    case "":
                        name = "guitar";
                        break;
                    case "rhythm":
                        name = "bass";
                        break;
                    case "drum":
                        name = "drums";
                        break;
                }
                var list = new List<byte>();
                list.AddRange(Easy.CreateGh5Notes(name, ref entries, ghwor));
                list.AddRange(Medium.CreateGh5Notes(name, ref entries, ghwor));
                list.AddRange(Hard.CreateGh5Notes(name, ref entries, ghwor));
                list.AddRange(Expert.CreateGh5Notes(name, ref entries, ghwor));
                return list.ToArray();
            }
            public byte[] ProcessNewDrumFills(ref int entries)
            {
                var readWrite = new ReadWrite("big");
                var drumFillBytes = new List<byte>();

                var fills = new List<int>();
                var diffs = new List<string>() { "easy", "medium", "hard", "expert" };
                int numFills = 0;
                uint elementSize = 8;
                if (DrumFill != null)
                {
                    foreach (QBArrayNode fill in DrumFill.Items)
                    {
                        foreach (int time in fill.Items)
                        {
                            fills.Add(time);
                        }
                    }
                    numFills = fills.Count / 2;
                }

                using (var stream = new MemoryStream())
                {
                    foreach (string diff in diffs)
                    {
                        string diffFill = $"{diff}drumfill";
                        MakeGh5NoteHeader(stream, diffFill, numFills, "gh5_drumfill_note", elementSize);
                        foreach (int fill in fills)
                        {
                            _readWriteGh5.WriteInt32(stream, fill);
                        }
                        entries++;
                    }


                    return stream.ToArray();
                }


            }
            public List<QBItem> MakeFaceOffQb(string name)
            {
                List<QBItem> qBItems = new List<QBItem>();
                string qbName = $"{name}{TrackName}_FaceOff";
                qBItems.Add(new QBItem(qbName + "P1", FaceOffP1));
                qBItems.Add(new QBItem(qbName + "P2", FaceOffP2));
                return qBItems;
            }
            public List<QBItem> MakeDrumFillQb(string name)
            {
                string[] diffs = ["Easy", "Medium", "Hard", "Expert"];
                var list = new List<QBItem>();
                foreach (string diff in diffs)
                {
                    string qbName = $"{name}_{diff}_DrumFill";
                    list.Add(new QBItem(qbName, DrumFill));
                }
                foreach (string diff in diffs)
                {
                    string qbName = $"{name}_{diff}_DrumUnmute";
                    var qb = new QBItem();
                    qb.SetName(qbName);
                    qb.MakeEmpty();
                    list.Add(qb);
                }
                return list;
            }
            public List<QBItem> CreateSoloMarker(string name)
            {
                List<QBItem> qbItems = new List<QBItem>();
                string[] diffs = ["Easy", "Medium", "Hard", "Expert"];
                foreach (string diff in diffs)
                {
                    string qbName = $"{name}_{diff}_SoloMarkers";
                    qbItems.Add(new QBItem(qbName, SoloMarker));
                }
                return qbItems;
            }
            private QBArrayNode ProcessOtherSections(List<MidiData.Note> entryNotes, SongQbFile songQb, bool isTapNote = false)
            {
                QBArrayNode entries = new QBArrayNode();
                if (entryNotes.Count == 0)
                {
                    entries.MakeEmpty();
                }
                else
                {
                    foreach (MidiData.Note entryNote in entryNotes)
                    {
                        QBArrayNode entry = new QBArrayNode();
                        int startTime = (int)Math.Round(songQb.TicksToMilliseconds(entryNote.Time));
                        int endTime = (int)Math.Round(songQb.TicksToMilliseconds(entryNote.EndTime));
                        int length = endTime - startTime;
                        entry.AddIntToArray(startTime);
                        entry.AddIntToArray(length);
                        if (isTapNote)
                        {
                            entry.AddIntToArray(1); // Always 1? Official songs have other values here sometimes
                        }
                        entries.AddArrayToArray(entry);
                    }
                }

                return entries;
            }
            // Use this for creating an array of arrays for the start and end times of notes
            // Set lessOne to true if the end time should be one millisecond less than the end time (e.g. for solo markers)
            private QBArrayNode ProcessStartEndArrays(List<MidiData.Note> entryNotes, SongQbFile songQb, bool lessOne = false)
            {
                QBArrayNode entries = new QBArrayNode();
                if (entryNotes.Count == 0)
                {
                    entries.MakeEmpty();
                }
                else
                {
                    foreach (MidiData.Note entryNote in entryNotes)
                    {
                        QBArrayNode entry = new QBArrayNode();
                        int startTime = (int)Math.Round(songQb.TicksToMilliseconds(entryNote.Time));
                        int endTime = (int)Math.Round(songQb.TicksToMilliseconds(entryNote.EndTime));
                        entry.AddIntToArray(startTime);
                        entry.AddIntToArray(endTime);

                        entries.AddArrayToArray(entry);
                    }
                }

                return entries;
            }

        }
        public class VocalsInstrument
        {
            public List<(int, QBStructData)> PerformanceScript { get; set; } = new List<(int, QBStructData)>();
            public List<PlayNote> Notes { get; set; } = new List<PlayNote>();
            public List<Freeform> FreeformPhrases { get; set; } = new List<Freeform>();
            public List<VocalPhrase>? VocalPhrases { get; set; } = new List<VocalPhrase>();
            public (int, int)? NoteRange { get; set; } = (60, 60);
            public List<VocalLyrics> Lyrics { get; set; } = new List<VocalLyrics>();
            public List<VocalPhrase>? Markers { get; set; } = new List<VocalPhrase>();
            internal List<StarPower>? StarPowerPhrases { get; set; } = new List<StarPower>();
            internal SongQbFile _songQb { get; set; }
            private string Name { get; set; }
            // GHWT stuff to come later
            public VocalsInstrument()
            {

            }
            public byte[] MakeGh5Vocals(ref int entries)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    MakeGh5VoxNotes(stream, ref entries);
                    Gh5VoxLyrics(stream, ref entries);
                    Gh5VoxPhrases(stream, ref entries);
                    Gh5Freeforms(stream, ref entries);
                    MakeNewStarPower(stream, "vocalstarpower", StarPowerPhrases, ref entries);
                    return stream.ToArray();
                }

            }
            private void MakeGh5VoxNotes(Stream stream, ref int entries)
            {
                MakeGh5NoteHeader(stream, "vocals", Notes.Count, "gh5_vocal_note", 7);
                for (int i = 0; i < Notes.Count; i++)
                {
                    PlayNote note = Notes[i];
                    CalculateNoteLengths(note, i);
                    _readWriteGh5.WriteInt32(stream, note.Time);
                    _readWriteGh5.WriteInt16(stream, (short)note.Length);
                    _readWriteGh5.WriteInt8(stream, (byte)note.Note);
                }
                entries++;
            }
            private void Gh5VoxLyrics(Stream stream, ref int entries, bool isWii = false)
            {
                uint elementSize = (uint)(isWii ? 36 : 68);
                int maxString = (int)(elementSize - 4) / 2;
                MakeGh5NoteHeader(stream, "vocallyrics", Lyrics.Count, "gh5_vocal_lyric", elementSize);
                for (int i = 0; i < Lyrics.Count; i++)
                {
                    VocalLyrics lyric = Lyrics[i];

                    lyric.Text = lyric.Text.Replace("\\L", "");
                    if (lyric.Text.Length > maxString)
                    {
                        Console.WriteLine($"{lyric.Time} - Truncating too long lyric text: {lyric.Text}");
                        lyric.Text = lyric.Text.Substring(0, maxString);
                    }
                    lyric.Text = lyric.Text.PadRight(maxString, '\0');
                    _readWriteGh5.WriteInt32(stream, lyric.Time);
                    WriteWideString(stream, lyric.Text);
                }
                entries++;
            }
            private void Gh5VoxPhrases(Stream stream, ref int entries, bool isWii = false)
            {
                int markerLength = isWii ? 132 : 260;
                FilterMarkers(markerLength);
                using (MemoryStream phraseStream = new MemoryStream())
                using (MemoryStream markerStream = new MemoryStream())
                {
                    entries += 2;
                    MakeGh5NoteHeader(phraseStream, "vocalphrase", VocalPhrases.Count, "gh5_vocal_phrase", 4);
                    MakeGh5NoteHeader(markerStream, "vocalsmarkers", Markers.Count, "gh5_vocal_marker_note", 260);

                    foreach (VocalPhrase phrase in VocalPhrases)
                    {
                        _readWriteGh5.WriteInt32(phraseStream, phrase.TimeMills);
                    }
                    foreach (VocalPhrase marker in Markers)
                    {
                        _readWriteGh5.WriteInt32(markerStream, marker.TimeMills);
                        WriteWideString(markerStream, marker.Text);
                    }

                    phraseStream.WriteTo(stream);
                    markerStream.WriteTo(stream);

                }
            }
            private void Gh5Freeforms(Stream stream, ref int entries)
            {
                MakeGh5NoteHeader(stream, "vocalfreeform", FreeformPhrases.Count, "gh5_vocal_freeform_note", 10);
                for (int i = 0; i < FreeformPhrases.Count; i++)
                {
                    Freeform phrase = FreeformPhrases[i];
                    _readWriteGh5.WriteInt32(stream, phrase.Time);
                    _readWriteGh5.WriteInt32(stream, phrase.Length);
                    _readWriteGh5.WriteInt16(stream, 0); // unk value. Always 0 in official songs
                }
                entries++;
            }
            public void ParseGh5Sp(List<int> starPower)
            {
                for (int i = 0; i < starPower.Count; i += 2)
                {
                    StarPower phrase = new StarPower();
                    phrase.SetTime(starPower[i]);
                    phrase.SetLength(starPower[i + 1]);
                    StarPowerPhrases.Add(phrase);
                }
            }
            private void FilterMarkers(int markerLength)
            {
                markerLength = (markerLength - 4) / 2;
                foreach (VocalPhrase phrase in VocalPhrases)
                {
                    if (phrase.Text == null)
                    {
                        continue;
                    }
                    else if (phrase.Type == VocalPhraseType.Lyrics)
                    {
                        phrase.Text = phrase.Text.PadRight(markerLength, '\0');
                        Markers.Add(phrase);
                    }
                    else if (phrase.Type == VocalPhraseType.Freeform)
                    {
                        phrase.Text = string.Empty.PadRight(markerLength, '\0');
                        Markers.Add(phrase);
                    }
                }
            }
            public (List<QBItem>, Dictionary<string, string>) AddVoxToQb(string name)
            {
                Name = name;
                var voxQb = new List<QBItem>
                {
                    MakeVocalNotes(),
                    MakeVocalFreeform()
                };
                var (phraseQb, markerQb, markerDict) = MakeVocalPhrases();
                voxQb.Add(phraseQb);
                voxQb.Add(MakeNoteRange());
                var (lyricQb, lyricDict) = MakeLyrics();
                voxQb.Add(lyricQb);
                voxQb.Add(markerQb);

                foreach (var (key, value) in markerDict)
                {
                    if (!lyricDict.ContainsKey(key))
                    {
                        lyricDict[key] = value;
                    }
                }


                return (voxQb, lyricDict);
            }
            private void CalculateNoteLengths(PlayNote note, int i)
            {
                if (note.Note == 2)
                {
                    PlayNote prevNote = Notes[i - 1];
                    PlayNote nextNote = Notes[i + 1];
                    note.Time = prevNote.Time + prevNote.Length;
                    note.Length = nextNote.Time - note.Time;
                }
            }
            private QBItem MakeVocalNotes()
            {
                string qbName = $"{Name}_song_vocals";
                var qb = new QBItem();
                qb.SetName(qbName);
                qb.SetInfo(ARRAY);
                QBArrayNode entry = new QBArrayNode();

                if (Notes.Count == 0)
                {
                    qb.MakeEmpty();
                    return qb;
                }
                else
                {
                    for (int i = 0; i < Notes.Count; i++)
                    {
                        PlayNote note = Notes[i];
                        CalculateNoteLengths(note, i);
                        entry.AddIntToArray(note.Time);
                        entry.AddIntToArray(note.Length);
                        entry.AddIntToArray(note.Note);
                    }
                }
                qb.SetData(entry);
                return qb;
            }
            private QBItem MakeVocalFreeform()
            {
                string qbName = $"{Name}_vocals_freeform";
                var qb = new QBItem();
                qb.SetName(qbName);
                qb.SetInfo(ARRAY);
                QBArrayNode entry = new QBArrayNode();

                if (FreeformPhrases.Count == 0)
                {
                    qb.MakeEmpty();
                    return qb;
                }
                else
                {
                    foreach (Freeform phrase in FreeformPhrases)
                    {
                        QBArrayNode phraseEntry = new QBArrayNode();
                        phraseEntry.AddIntToArray(phrase.Time);
                        phraseEntry.AddIntToArray(phrase.Length);
                        phraseEntry.AddIntToArray(phrase.Points);
                        entry.AddArrayToArray(phraseEntry);
                    }
                }
                qb.SetData(entry);
                return qb;
            }
            private (QBItem, QBItem, Dictionary<string, string>) MakeVocalPhrases()
            {
                string qbNamePhrase = $"{Name}_vocals_phrases";
                string qbNameMarker = $"{Name}_vocals_markers";

                var qbPhrase = new QBItem();
                qbPhrase.SetName(qbNamePhrase);
                qbPhrase.SetInfo(ARRAY);
                QBArrayNode entryPhrase = new QBArrayNode();

                var qbMarker = new QBItem();
                qbMarker.SetName(qbNameMarker);
                qbMarker.SetInfo(ARRAY);
                QBArrayNode entryMarker = new QBArrayNode();

                var markerDict = new Dictionary<string, string>();

                if (VocalPhrases.Count == 0)
                {
                    qbPhrase.MakeEmpty();
                    qbMarker.MakeEmpty();
                    return (qbPhrase, qbMarker, markerDict);
                }
                else
                {
                    foreach (VocalPhrase phrase in VocalPhrases)
                    {
                        entryPhrase.AddIntToArray(phrase.TimeMills);
                        entryPhrase.AddIntToArray(phrase.Player);
                        if (phrase.Player != 0)
                        {
                            QBStructData markerData = new QBStructData();
                            markerData.AddIntToStruct("time", phrase.TimeMills);
                            string markerType = phrase.Type == VocalPhraseType.Freeform ? POINTER : QSKEY;
                            string markerText;
                            if (markerType == POINTER)
                            {
                                markerText = $"vocal_marker_{phrase.Text}";
                            }
                            else
                            {
                                markerText = $"\\L{phrase.Text}";
                                string qsKey = CRC.QBKeyQs(markerText);
                                if (!markerDict.ContainsKey(qsKey))
                                {
                                    markerDict[qsKey] = $"\"{markerText}\"";
                                }
                                markerText = qsKey;
                            }
                            markerData.AddVarToStruct("marker", markerText, markerType);
                            entryMarker.AddStructToArray(markerData);
                        }
                    }
                }
                qbPhrase.SetData(entryPhrase);
                qbMarker.SetData(entryMarker);
                return (qbPhrase, qbMarker, markerDict);
            }
            private QBItem MakeNoteRange()
            {
                string qbName = $"{Name}_vocals_note_range";
                var qb = new QBItem();
                qb.SetName(qbName);
                qb.SetInfo(ARRAY);
                QBArrayNode entry = new QBArrayNode();
                entry.AddIntToArray(NoteRange!.Value.Item1);
                entry.AddIntToArray(NoteRange!.Value.Item2);
                qb.SetData(entry);
                return qb;
            }
            private (QBItem, Dictionary<string, string>) MakeLyrics()
            {
                string qbName = $"{Name}_lyrics";
                var qbLyrics = new QBItem();
                qbLyrics.SetName(qbName);
                qbLyrics.SetInfo(ARRAY);
                QBArrayNode entryLyric = new QBArrayNode();

                var lyricDict = new Dictionary<string, string>();

                if (Lyrics.Count == 0)
                {
                    qbLyrics.MakeEmpty();
                    return (qbLyrics, lyricDict);
                }
                else
                {
                    foreach (VocalLyrics lyric in Lyrics)
                    {
                        QBStructData lyricData = new QBStructData();
                        lyricData.AddIntToStruct("time", lyric.Time);
                        string lyricText = lyric.Text;
                        string qsKey = CRC.QBKeyQs(lyricText);
                        if (!lyricDict.ContainsKey(qsKey))
                        {
                            lyricDict[qsKey] = $"\"{lyricText}\"";
                        }
                        lyricText = qsKey;
                        lyricData.AddVarToStruct("text", lyricText, QSKEY);
                        entryLyric.AddStructToArray(lyricData);
                    }
                }
                qbLyrics.SetData(entryLyric);
                return (qbLyrics, lyricDict);
            }
            public void MakeInstrument(TrackChunk trackChunk, SongQbFile songQb, long lastEvent)
            {
                if (trackChunk == null || songQb == null)
                {
                    throw new ArgumentNullException("trackChunk or songQb is null");
                }
                _songQb = songQb;
                var timedEvents = trackChunk.GetTimedEvents().ToList();
                var textEvents = timedEvents.Where(e => e.Event is TextEvent || e.Event is LyricEvent).ToList();
                PerformanceScript = _songQb.InstrumentScripts(textEvents, VOCALIST);
                if (Game != GAME_GH3 && Game != GAME_GHA)
                {
                    bool isGh5orWor = Game == GAME_GH5 || Game == GAME_GHWOR;
                    var allNotes = trackChunk.GetNotes().ToList();
                    var singNotes = new Dictionary<long, MidiData.Note>();
                    var phraseNotes = new Dictionary<long, VocalPhrase>();
                    var freeformNotes = new Dictionary<long, int>();
                    var lyrics = new Dictionary<long, string>();
                    foreach (var textData in textEvents)
                    {
                        string? eventText = textData.Event switch
                        {
                            TextEvent textEvent => textEvent.Text,
                            LyricEvent lyricEvent => lyricEvent.Text,
                            _ => null
                        };

                        if (eventText != null)
                        {
                            var (eventType, eventData) = songQb.GetEventData(eventText);
                            var eventTime = textData.Time;
                            if (eventType == LYRIC)
                            {
                                lyrics[eventTime] = eventText; // Use indexer for potential overwrite instead of Add to avoid duplicate key errors
                            }
                        }
                    }
                    var lyricTimeList = lyrics.Keys.ToList();
                    lyricTimeList.Sort();
                    var slideTimeList = new List<long>();
                    var noteRangeMin = 127;
                    var noteRangeMax = 0;
                    var allLyrics = new List<string>();
                    bool nextJoin = false;
                    foreach (MidiData.Note note in allNotes)
                    {
                        if ((note.NoteNumber >= VocalMin && note.NoteNumber <= VocalMax) || note.NoteNumber == VocalTalkie)
                        {
                            if (singNotes.ContainsKey(note.Time))
                            {
                                songQb.AddTimedError("Duplicate vocal note found", "PART VOCALS", note.Time);
                            }
                            else
                            {
                                if (lyrics.TryGetValue(note.Time, out string? lyric))
                                {
                                    if (nextJoin && lyric != SLIDE_LYRIC)
                                    {
                                        lyric = HYPHEN_LYRIC + lyric;
                                        nextJoin = false;
                                    }

                                    // Use the method to trim the lyric if it ends with specific suffixes
                                    lyric = TrimEndIfMatched(lyric, RANGE_SHIFT_LYRIC, UNKNOWN_LYRIC);

                                    // After trimming, check if the specific cases for setting the note number need to be handled
                                    if (lyric.EndsWith(TALKIE_LYRIC) || lyric.EndsWith(TALKIE_LYRIC2))
                                    {
                                        lyric = lyric.Substring(0, lyric.Length - 1);
                                        note.NoteNumber = (SevenBitNumber)26;
                                    }
                                    // Use the opportunity to set the note range since it must exclude the talkie notes
                                    else
                                    {
                                        if (note.NoteNumber < noteRangeMin)
                                        {
                                            noteRangeMin = note.NoteNumber;
                                        }
                                        if (note.NoteNumber > noteRangeMax)
                                        {
                                            noteRangeMax = note.NoteNumber;
                                        }
                                    }

                                    // Handle the replacement for LINKED_LYRIC
                                    if (lyric.Contains(LINKED_LYRIC))
                                    {
                                        lyric = lyric.Replace(LINKED_LYRIC, " ");
                                    }
                                    switch (lyric)
                                    {
                                        case SLIDE_LYRIC:
                                            try
                                            {
                                                var prevTime = lyricTimeList.IndexOf(note.Time) - 1;
                                                var prevNote = singNotes[lyricTimeList[prevTime]];
                                                var newNote = new MidiData.Note((SevenBitNumber)2, note.Time - prevNote.EndTime, prevNote.EndTime);
                                                singNotes.Add(newNote.Time, newNote);
                                                slideTimeList.Add(note.Time);
                                            }
                                            catch (KeyNotFoundException e)
                                            {
                                                _songQb.AddTimedError("Slide lyric found without (or misaligned) previous note", "PART VOCALS", note.Time);
                                            }
                                            /*
                                            catch (ArgumentException e) // Duplicate key exception, as well as slide without gap exception
                                            {
                                                _songQb.AddTimedError("Slide without gap found or lyric found with duplicate note", "PART VOCALS", note.Time);
                                            }*/
                                            catch (Exception e)
                                            {
                                                throw e;
                                            }
                                            break;
                                        case var o when o.EndsWith(HYPHEN_LYRIC):
                                            lyric = lyric.Substring(0, (int)(lyric.Length - 1)) + JOIN_LYRIC;
                                            nextJoin = true;
                                            goto default;
                                        case var o when o.EndsWith(JOIN_LYRIC):
                                            lyric = lyric.Substring(0, (int)(lyric.Length - 1));
                                            nextJoin = true;
                                            goto default;
                                        default:
                                            lyrics[note.Time] = lyric;
                                            allLyrics.Add((string)lyric);
                                            break;
                                    }
                                    try
                                    {
                                        singNotes.Add(note.Time, note);
                                    }
                                    catch
                                    {
                                        singNotes[note.Time] = note; // Not having this causes a bug in the error reporting system and I don't know why.
                                        songQb.AddTimedError("Duplicate vocal note or slide without gap found", "PART VOCALS", note.Time);
                                    }
                                }
                                else
                                {
                                    songQb.AddTimedError("Vocal note found without lyrics", "PART VOCALS", note.Time);
                                }
                            }
                        }
                        else if (note.NoteNumber == PhraseMin || note.NoteNumber == PhraseMax)
                        {
                            var player = note.NoteNumber - (PhraseMin - 1);
                            if (phraseNotes.ContainsKey(note.Time))
                            {
                                phraseNotes[note.Time].Player += player;
                            }
                            else
                            {
                                phraseNotes.Add(note.Time, new VocalPhrase(note.Time, note.EndTime, player));
                            }
                        }
                        else if (note.NoteNumber == FreeformNote)
                        {
                            if (!freeformNotes.ContainsKey(note.Time))
                            {
                                freeformNotes.Add(note.Time, note.Channel);
                                int noteTime = (int)songQb.TicksToMilliseconds(note.Time);
                                int noteLength = (int)(songQb.TicksToMilliseconds(note.EndTime) - noteTime);
                                int points = note.Channel == 1 ? 0 : noteLength / 6;
                                FreeformPhrases.Add(new Freeform(noteTime, noteLength, points));
                            }
                            else
                            {
                                songQb.AddTimedError("Duplicate freeform note found", "PART VOCALS", note.Time);
                            }
                        }
                        else if (note.NoteNumber == StarPowerNote)
                        {
                            StarPowerPhrases.Add(new StarPower((int)songQb.TicksToMilliseconds(note.Time), (int)(songQb.TicksToMilliseconds(note.EndTime) - songQb.TicksToMilliseconds(note.Time)), 1));
                        }
                    }
                    foreach (var time in slideTimeList)
                    {
                        lyrics.Remove(time);
                        lyricTimeList.Remove(time);
                    }
                    var allMarkers = new List<VocalPhrase>();
                    lyrics.OrderBy(n => n.Key);
                    foreach (var phrase in phraseNotes.Values)
                    {
                        var notesInPhrase = lyrics.Where(n => n.Key >= phrase.Time && n.Key < phrase.EndTime).ToDictionary(n => n.Key, n => n.Value);
                        phrase.SetText(MakePhrases(notesInPhrase));
                        if (phrase.Text == string.Empty)
                        {
                            if (freeformNotes.TryGetValue(phrase.Time, out int freeform))
                            {
                                phrase.SetType(VocalPhraseType.Freeform);
                                phrase.SetPlayer(3);
                                phrase.SetText(freeform == 1 ? "Hype" : "Freeform");
                                allMarkers.Add(phrase);
                            }
                        }
                        else
                        {
                            phrase.SetType(VocalPhraseType.Lyrics);
                            allMarkers.Add(phrase);
                        }
                    }
                    var singTimes = singNotes.Keys.ToList();
                    singTimes.Sort();
                    foreach (var time in singTimes)
                    {
                        var note = singNotes[time];

                        Notes.Add(new PlayNote((int)songQb.TicksToMilliseconds(note.Time), (int)note.NoteNumber, (int)(songQb.TicksToMilliseconds(note.EndTime) - songQb.TicksToMilliseconds(note.Time)), "Vocals"));
                    }
                    var lyricTimes = lyrics.Keys.ToList();
                    lyricTimes.Sort();
                    foreach (var time in lyricTimes)
                    {
                        var lyric = lyrics[time];
                        Lyrics.Add(new VocalLyrics((int)songQb.TicksToMilliseconds(time), $"\\L{lyric}"));
                    }
                    var sortedPhrases = MakeDummyPhrases(phraseNotes, songQb.TPB, lastEvent);
                    foreach (var phrase in sortedPhrases)
                    {
                        phrase.SetTimeMills((int)songQb.TicksToMilliseconds(phrase.Time));
                        VocalPhrases.Add(phrase);
                    }
                    if (noteRangeMax != 0)
                    {
                        NoteRange = (noteRangeMin, noteRangeMax);
                    }
                    allLyrics = allLyrics.Distinct().ToList();
                }
            }
            private string TrimEndIfMatched(string lyric, params string[] suffixes)
            {
                foreach (var suffix in suffixes)
                {
                    if (lyric.EndsWith(suffix))
                    {
                        return lyric.Substring(0, lyric.Length - 1);
                    }
                }
                return lyric;
            }
            private static string MakePhrases(Dictionary<long, string> dictionary)
            {
                StringBuilder result = new StringBuilder();

                // Sort the dictionary by keys
                var sortedDictionary = dictionary.OrderBy(pair => pair.Key);

                var addSpace = new List<bool>();

                foreach (var pair in sortedDictionary)
                {
                    if (pair.Value.StartsWith("="))
                    {
                        addSpace.Add(false);
                    }
                    else
                    {
                        addSpace.Add(true);
                    }
                }

                for (int i = 0; i < sortedDictionary.Count(); i++)
                {
                    var pair = sortedDictionary.ElementAt(i);
                    var value = pair.Value;
                    if (value.StartsWith("="))
                    {
                        result.Append(value.Substring(1));
                    }
                    else
                    {
                        result.Append(value);
                    }
                    if (i < sortedDictionary.Count() - 1)
                    {
                        if (addSpace[i + 1])
                        {
                            result.Append(" ");
                        }
                    }
                }

                return result.ToString();
            }
            private List<VocalPhrase> MakeDummyPhrases(Dictionary<long, VocalPhrase> keyValuePairs, long tpb = 480, long lastEventTime = 0)
            {
                long timeDivisor = tpb * 16; // About 4 measures.
                var phraseTimes = keyValuePairs.Keys.ToList();
                phraseTimes.Sort();
                long prevEndTime = 0;

                // Initial dummy phrase at tick 0 if necessary
                if (!keyValuePairs.ContainsKey(0))
                {
                    long firstPhraseTime = phraseTimes.Count > 0 ? phraseTimes[0] : Math.Min(timeDivisor, lastEventTime);
                    long gap = firstPhraseTime;
                    FillGapWithDummyPhrases(0, gap, keyValuePairs, timeDivisor);
                    prevEndTime = firstPhraseTime;
                }

                // Fill in gaps between existing phrases
                foreach (var time in phraseTimes)
                {
                    var phrase = keyValuePairs[time];
                    var gap = phrase.Time - prevEndTime;
                    if (gap > timeDivisor)
                    {
                        FillGapWithDummyPhrases(prevEndTime, gap, keyValuePairs, timeDivisor);
                    }
                    prevEndTime = phrase.EndTime;
                }

                // Fill in the gap after the final phrase until the last event time, if necessary
                if (prevEndTime < lastEventTime)
                {
                    long finalGap = lastEventTime - prevEndTime;
                    FillGapWithDummyPhrases(prevEndTime, finalGap, keyValuePairs, timeDivisor);
                }

                return keyValuePairs.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
            }
            private void FillGapWithDummyPhrases(long start, long gap, Dictionary<long, VocalPhrase> keyValuePairs, long timeDivisor)
            {
                if (gap <= 0) return; // No gap to fill

                var phraseCount = (int)Math.Ceiling((double)gap / timeDivisor);
                var gapForEachPhrase = gap / phraseCount;

                for (int i = 0; i < phraseCount; i++)
                {
                    var newPhraseTime = start + i * gapForEachPhrase;
                    var newPhraseEndTime = (i + 1) == phraseCount ? start + gap : start + (i + 1) * gapForEachPhrase;
                    var newPhrase = new VocalPhrase(newPhraseTime, newPhraseEndTime, 0);
                    if (!keyValuePairs.ContainsKey(newPhraseTime)) // Ensure not to overwrite existing phrases
                    {
                        keyValuePairs.Add(newPhraseTime, newPhrase);
                    }
                }
            }
        }
        public List<PlayNote> MakeGuitar(List<MidiData.Chord> chords, List<MidiData.Note> forceOn, List<MidiData.Note> forceOff, Dictionary<MidiTheory.NoteName, int> noteDict)
        {
            List<PlayNote> noteList = new List<PlayNote>();
            long prevTime = 0;
            int prevNote = 0;
            for (int i = 0; i < chords.Count; i++)
            {
                MidiData.Chord notes = chords[i];
                long currTime = notes.Time;
                int noteVal = 0;
                long endTimeTicks = notes.EndTime;
                foreach (MidiData.Note note in notes.Notes)
                {
                    noteVal += noteDict[note.NoteName];
                    if (note.EndTime < endTimeTicks)
                    {
                        endTimeTicks = note.EndTime;
                    }
                }
                int startTime = (int)Math.Round(TicksToMilliseconds(currTime));
                int endTime = (int)Math.Round(TicksToMilliseconds(endTimeTicks));
                int length = endTime - startTime;
                if (length < 10) // Mostly for Moonscraper charts with their 1 tick lengths...
                {
                    length = 10; // This is a 1/128th note at 200bpm. Definitely small enough to not overlap even the fastest of notes
                }
                PlayNote currNote = new PlayNote(startTime, noteVal, length);
                switch (HopoMethod)
                {
                    case HopoType.RB:
                        if (prevTime != 0 && BitOperations.IsPow2(noteVal) && (prevNote & noteVal) != noteVal) // If it's not the first note in the chart and a single note
                        {
                            currNote.IsHopo = (currTime - prevTime) < HopoThreshold;
                        }
                        currNote.ForcedOn = IsInTimeRange(currTime, forceOn);
                        currNote.ForcedOff = IsInTimeRange(currTime, forceOff);
                        break;
                    case HopoType.GH3:
                        if (prevTime != 0 && BitOperations.IsPow2(noteVal) && prevNote != noteVal) // If it's not the first note in the chart and a single note
                        {
                            currNote.IsHopo = (currTime - prevTime) < HopoThreshold;
                        }
                        currNote.ForcedOn = IsInTimeRange(currTime, forceOn);
                        if (currNote.ForcedOn && currNote.IsHopo)
                        {
                            currNote.ForcedOn = false;
                            currNote.ForcedOff = true;
                        }
                        break;
                    case HopoType.MoonScraper:
                        if (prevTime != 0 && BitOperations.IsPow2(noteVal) && prevNote != noteVal) // If it's not the first note in the chart and a single note
                        {
                            currNote.IsHopo = (currTime - prevTime) < HopoThreshold;
                        }
                        currNote.ForcedOn = IsInTimeRange(currTime, forceOn);
                        currNote.ForcedOff = IsInTimeRange(currTime, forceOff);
                        break;
                    case HopoType.GHWT:
                        currNote.ForcedOn = IsInTimeRange(currTime, forceOn);
                        currNote.IsHopo = currNote.ForcedOn;
                        break;
                    default:
                        throw new NotImplementedException("Unknown hopo method found");
                }

                prevTime = currTime;
                prevNote = noteVal;
                noteList.Add(currNote);
            }
            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                // Make method to make sure notes do not overlap
                FixOverlappingNotes(noteList);
            }
            else
            {
                // Make method to determine which notes overlap and change flags as appropriate
                MakeExtendedNotes(noteList);
            }
            return noteList;
        }
        private void FixOverlappingNotes(List<PlayNote> notes)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                // Get the current note
                var currNote = notes[i];
                // Get the notes that are contained within the current note
                var containedNotes = notes.Where(n => n.Time > currNote.Time && n.Time < (currNote.Time + currNote.Length)).ToList();


                if (containedNotes.Count > 0)
                {
                    // If there are notes contained within the current note, adjust the length of the current note
                    // to end at the start of the first contained note
                    currNote.Length = containedNotes[0].Time - currNote.Time;
                }
            }
        }
        private void MakeExtendedNotes(List<PlayNote> notes)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                // Get the current note
                var currNote = notes[i];

                // Ensure currNote.Accents has itself (currNote.Note) set
                currNote.Accents |= currNote.Note;

                // Get the notes that are contained within the current note
                var containedNotes = notes.Where(n => n.Time > currNote.Time && n.Time < (currNote.Time + currNote.Length)).ToList();
                foreach (var note in containedNotes)
                {
                    if (note.Note < currNote.Note)
                    {
                        currNote.Length = note.Time - currNote.Time;
                        break;
                    }
                    /*
                    // Only add bits from note.Note that aren't already set in currNote.Accents
                    // First, determine which bits are not yet set in currNote.Accents
                    int bitsNotSetExtend = ~currNote.Accents & note.Note;
                    // Then, add those bits to currNote.Accents
                    currNote.Accents |= bitsNotSetExtend;*/

                    // Unset bits from note.Note that are set in currNote.Accents
                    int bitsToUnset = ~note.Note & currNote.Accents;
                    // Then, update currNote.Accents by unsetting these bits
                    currNote.Accents &= bitsToUnset;
                    note.Accents &= ~currNote.Note;
                }
                /*
                // Apply updated currNote.Accents to all contained notes that weren't skipped
                foreach (var note in containedNotes)
                {
                    if (note.Time >= currNote.Time + currNote.Length)
                    {
                        // This note was beyond the updated range; it should not be updated.
                        break;
                    }

                    // Unset bits from note.Accents that are set in currNote.Note
                    

                    //note.Accents = currNote.Accents;
                }*/
            }
        }
        public List<PlayNote> MakeDrums(List<MidiData.Chord> chords, Dictionary<MidiTheory.NoteName, int> noteDict)
        {
            List<PlayNote> noteList = new List<PlayNote>();

            long prevTime = 0;
            for (int i = 0; i < chords.Count; i++)
            {
                MidiData.Chord notes = chords[i];
                KickNote? kickNote = null;
                // If the kick note and hand-notes are different lengths, this is made to make a potential second entry

                long currTime = notes.Time;
                int noteVal = 0;
                int accentVal = AllAccents;
                int ghostVal = 0;
                byte numAccents = 0;
                long endTimeTicks = notes.EndTime;
                foreach (MidiData.Note note in notes.Notes)
                {
                    int noteBit = noteDict[note.NoteName];
                    if (note.NoteName != MidiTheory.NoteName.C && note.NoteName != MidiTheory.NoteName.B)
                    {
                        noteVal += noteBit;
                        if (note.EndTime < endTimeTicks)
                        {
                            // This ensures that the end time is the shortest note in the chord
                            // Not counting kick notes which can be separate.
                            endTimeTicks = note.EndTime;
                        }
                        if (note.Velocity != AccentVelocity)
                        {
                            // If the note is not an accent, remove the accent bit
                            accentVal -= noteBit;
                        }
                        else
                        {
                            numAccents++;
                        }
                        if (note.Velocity == GhostVelocity)
                        {
                            ghostVal += noteBit;
                        }
                    }
                    else if (kickNote == null)
                    {
                        kickNote = new KickNote(note.EndTime);
                        kickNote.NoteBit += noteBit;
                    }
                    else
                    {
                        kickNote.NoteBit += noteBit;
                    }
                }
                if (numAccents == 0)
                {
                    accentVal = 0;
                }
                if (kickNote != null)
                {
                    var eightNoteCheck = TPB / 2;
                    var kickCheck = kickNote.EndTimeTicks;
                    if (kickCheck == endTimeTicks || (kickCheck <= eightNoteCheck && endTimeTicks <= eightNoteCheck))
                    {
                        // If the kick note is the same length as the chord or less than an eighth note
                        // It can be combined with the hand notes
                        noteVal += kickNote.NoteBit;
                        kickNote = null;
                    }
                }
                int startTime = (int)Math.Round(TicksToMilliseconds(currTime));
                int endTime = (int)Math.Round(TicksToMilliseconds(endTimeTicks));
                int length = endTime - startTime;
                if (length < 10) // Mostly for Moonscraper charts with their 1 tick lengths...
                {
                    length = 10; // This is a 1/128th note at 200bpm. Definitely small enough to not overlap even the fastest of notes
                }
                PlayNote currNote = new PlayNote(startTime, noteVal, length);
                currNote.Accents = accentVal;
                currNote.Ghosts = ghostVal;

                prevTime = currTime;
                noteList.Add(currNote);
                if (kickNote != null)
                {
                    // If there is still a kick note remaining, add it as a separate note
                    int endKickTime = (int)Math.Round(TicksToMilliseconds(kickNote.EndTimeTicks));
                    int kickLength = endKickTime - startTime;
                    if (kickLength < 10)
                    {
                        kickLength = 10;
                    }
                    PlayNote sepKick = new PlayNote(startTime, kickNote.NoteBit, kickLength);
                    noteList.Add(sepKick);
                }
            }

            return noteList;
        }
        public static byte[] MakePs2SkaScript(string gender = "Male", string songname = "")
        {
            string qbName = $"data\\songs\\{songname}_song_scripts.qb";
            string skaScript = $"""
                script {songname}_song_startup
                    animload_Singer_{gender}_{songname} <...>
                endscript
                """;
            var (skaScriptList, _) = ParseQFile(skaScript);
            var scriptCompiled = CompileQbFile(skaScriptList, qbName, game: GAME_GH3, console: CONSOLE_PS2);
            return scriptCompiled;
        }
        public void ParseClipsAndAnims()
        {
            if (SongScriptOverride == null || !File.Exists(SongScriptOverride))
            {
                return;
            }
            var songScripts = CleanOldScripts(SongScriptOverride);
            var (songScriptsQb, _) = ParseQFile(songScripts);
            foreach (var item in songScriptsQb)
            {
                if (item.Name.ToLower().StartsWith("car_"))
                {
                    ParseCarAnim(item);
                }
                else if (item.Info.Type == "Script")
                {
                    SongScripts.Add(item);
                }
                else
                {
                    ParseClips(item);
                }
            }
        }
        private void ParseCarAnim(QBItem item)
        {
            string genPattern = @"(?<!\p{L})(male|female)(?!\p{L})";
            var gender = Regex.Match(item.Name, genPattern);
            string altPattern = @"(?<!\p{L})alt(?!\p{L})";
            var alt = Regex.Match(item.Name, altPattern);
            bool isAlt = alt.Success;

            if (gender.Success)
            {
                string altString = isAlt ? "_alt" : "";
                AnimStructs[$"{gender.Value}{altString}"] = new AnimStruct(gender.Value, item.Data as QBStructData, isAlt);
            }
        }
        private void ParseClips(QBItem item)
        {
            if (item.Data is QBStructData)
            {
                try
                {
                    SongClips[QBKey(item.Name)] = new SongClip(item.Name, Game, item.Data as QBStructData);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error parsing clip {item.Name}: {e.Message}. Skipping");
                }
            }
        }
        public byte[]? MakeSongScripts()
        {
            // For non-ps2 games

            bool isNew = Game == GAME_GH5 || Game == GAME_GHWOR;
            var scriptDict = new Dictionary<string, QBItem>();

            if (!isNew)
            {
                foreach (var gender in AnimStructs.Values)
                {
                    var animStruct = gender.MakeAnimStruct(SongName);
                    scriptDict[animStruct.GetName()] = animStruct;
                }
            }
            foreach (var script in SongScripts)
            {
                scriptDict[script.GetName()] = script;
            }
            foreach (var clip in SongClips)
            {
                scriptDict[clip.Key] = clip.Value.MakeClip();
            }
            if (isNew)
            {
                var scriptarray = new QBArrayNode();
                foreach (var perfScript in ScriptTimedEvents)
                {
                    scriptarray.AddStructToArray(perfScript.Item2);
                }
                var scriptevents = new QBItem($"{SongName}_scriptevents", scriptarray);
                scriptDict[scriptevents.GetName()] = scriptevents;
            }
            string scriptName = isNew ? ".perf.xml" : "_song_scripts";
            var qbScriptFile = CompileQbFromDict(scriptDict, $"songs\\{SongName}{scriptName}.qb", Game, GamePlatform);
            return qbScriptFile;
        }
        private string CleanOldScripts(string script)
        {
            string pattern = @"^script\s\w+\s*=\s*""([0-9a-fA-F]{2}( [0-9a-fA-F]{2})*)""$";
            string newFile = "";
            int oldScripts = 0;
            foreach (var line in File.ReadLines(script))
            {
                if (Regex.IsMatch(line, pattern))
                {
                    oldScripts++;
                }
                else
                {
                    newFile += line + "\n";
                }
            }
            if (oldScripts > 0)
            {
                var plural = oldScripts > 1 ? "s" : "";
                Console.WriteLine($"Removed {oldScripts} old script{plural}. Update your Q file");
            }


            return newFile;
        }
        [DebuggerDisplay("{Time}: {Numerator}/{Denominator}")]
        public class TimeSig
        {
            public int Time { get; set; }
            public int Numerator { get; set; }
            public int Denominator { get; set; }
            public TimeSig(int time, int numerator, int denominator)
            {
                Time = time;
                Numerator = numerator;
                Denominator = denominator;
            }
        }
        [DebuggerDisplay("{PlayNotes.Count} Notes")]
        public class Difficulty
        {

            public string diffName { get; set; }
            public string PartName { get; set; }
            public List<PlayNote>? PlayNotes { get; set; } = new List<PlayNote>();
            public List<PlayNote>? ExPlusNotes { get; set; } = null; // Only used for WT to make 2 paks
            public List<StarPower>? StarEntries { get; set; } = new List<StarPower>();
            public List<StarPower>? BattleStarEntries { get; set; } = new List<StarPower>();
            public List<StarPower>? FaceOffStar { get; set; } = new List<StarPower>();
            public List<StarPower>? TapNotes { get; set; } = new List<StarPower>();
            private string Q_Game { get; set; } = GAME_GHWT;
            private bool ExpertPlus { get; set; }
            public string nameProper { get
                {
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(diffName);
                }
            }
            public bool HasExpertPlus {
                get
                {
                    return ExpertPlus;
                }
            }
            public Difficulty(string name)
            {
                diffName = name;
            }
            public Difficulty(string name, QBArrayNode notes, QBArrayNode? star, QBArrayNode? starBM, QBArrayNode? tapNotes, QBArrayNode? faceoffStar, string qGame = GAME_GHWT)
            {
                Q_Game = qGame;
                diffName = name;
                if (notes.Items.Count != 0)
                {
                    PlayNotes = QbArrayToNotes(notes);
                }
                if (star != null && star.Items.Count != 0)
                {
                    StarEntries = QbArrayToSp(star);
                }
                if (starBM != null && starBM.Items.Count != 0)
                {
                    BattleStarEntries = QbArrayToSp(starBM);
                }
                if (tapNotes != null && tapNotes.Items.Count != 0)
                {
                    TapNotes = QbArrayToSp(tapNotes);
                }
            }
            public Difficulty(string name, string partName, Dictionary<string, List<int>> diffData, bool expertPlus = false)
            {
                diffName = name;
                PartName = partName;
                foreach (var data in diffData)
                {
                    switch (data.Key)
                    {
                        case "instrument":
                            PlayNotes = new List<PlayNote>();
                            int toSkip = expertPlus ? 5 : 4;
                            for (int i = 0; i < data.Value.Count; i += toSkip)
                            {
                                bool isHopo = false;
                                int time = data.Value[i];
                                int length = data.Value[i + 1];
                                int note = data.Value[i + 2];
                                if (PartName != DRUMS_NAME)
                                {
                                    isHopo = (note & 0x40) != 0;
                                    note = note & 0x3F;
                                }
                                int accents = data.Value[i + 3];
                                var currNote = new PlayNote(time, note, length, accents, isHopo);
                                int ghosts;
                                if (expertPlus)
                                {
                                    ghosts = data.Value[i + 4];
                                    currNote.Note += ghosts;
                                    currNote.Ghosts = ghosts;
                                }
                                PlayNotes.Add(currNote);
                            }
                            break;
                        case "starpower":
                            StarEntries = ProcessGh5SpEntries(data.Value);
                            break;
                        case "tapping":
                            TapNotes = ProcessGh5SpEntries(data.Value);
                            break;
                    }
                }
            }
            private List<StarPower> ProcessGh5SpEntries(List<int> spEntries)
            {
                List<StarPower> starEntries = new List<StarPower>();
                for (int i = 0; i < spEntries.Count; i += 2)
                {
                    int time = spEntries[i];
                    int length = spEntries[i + 1];
                    starEntries.Add(new StarPower(time, length, 0));
                }
                return starEntries;
            }
            private List<PlayNote> QbArrayToNotes(QBArrayNode notes)
            {
                List<PlayNote> noteList = new List<PlayNote>();
                if (notes.FirstItem.Type != INTEGER)
                {
                    throw new ArgumentException("First item in array is not an integer");
                }
                var noteArray = notes.Items;
                if (Q_Game == GAME_GHWT)
                {
                    for (int i = 0; i < noteArray.Count; i += 2)
                    {
                        int time = (int)noteArray[i];
                        int note = (int)noteArray[i + 1];
                        var length = (ushort)note;
                        note = note >> 16;
                        var noteBits = (byte)note & 0x3F;
                        var isHopo = (note & 0x40) != 0;
                        note = note >> 7;
                        var accents = (byte)note & 0x1F;
                        var isDouble = (note & 0x40) != 0;
                        if (isDouble)
                        {
                            noteBits |= 0x40;
                        }
                        noteList.Add(new PlayNote(time, noteBits, length, accents, isHopo));
                    }
                }
                else if (Q_Game == GAME_GH3 || Q_Game == GAME_GHA)
                {
                    for (int i = 0; i < noteArray.Count; i += 3)
                    {
                        int time = (int)noteArray[i];
                        int length = (int)noteArray[i + 1];
                        int note = (int)noteArray[i + 2];
                        var isForced = (note & 0b00100000) != 0;
                        noteList.Add(new PlayNote(time, note, length, isForced));
                    }
                }
                return noteList;
            }
            private List<StarPower> QbArrayToSp(QBArrayNode spNotes)
            {

                List<StarPower> noteList = new List<StarPower>();
                if (spNotes.FirstItem.Type != ARRAY)
                {
                    throw new ArgumentException("First item in array is not an array");
                }
                foreach (QBArrayNode intArray in spNotes.Items)
                {
                    if (intArray.FirstItem.Type != INTEGER)
                    {
                        throw new ArgumentException("First item in array is not an integer");
                    }
                    if (intArray.Items.Count == 2)
                    {
                        intArray.Items.Add(1);
                    }
                    else if (intArray.Items.Count != 3)
                    {
                        throw new ArgumentException("Array does not have 2 or 3 items");
                    }
                    int time = (int)intArray.Items[0];
                    int length = (int)intArray.Items[1];
                    int numNotes = (int)intArray.Items[2];
                    noteList.Add(new StarPower(time, length, numNotes));
                }
                return noteList;
            }
            public void ProcessDifficultyGuitar(List<MidiData.Note> allNotes, int minNote, int maxNote, Dictionary<MidiTheory.NoteName, int> noteDict, int openNotes, SongQbFile songQb, List<MidiData.Note> StarPowerPhrases, List<MidiData.Note> BattleStarPhrases, List<MidiData.Note>? FaceOffStarPhrases = null, List<StarPower>? sysexTaps = null, List<StarPower>? sysexOpens = null, string trackName = "")
            {
                var notes = allNotes.Where(n => n.NoteNumber >= (minNote - openNotes) && n.NoteNumber <= maxNote).ToList();
                //var chords = notes.GetChords().ToList();
                var chords = GroupNotes(notes, DefaultTickThreshold * songQb.TPB / DefaultTPB).ToList();
                var onNotes = allNotes.Where(n => n.NoteNumber == maxNote + 1).ToList();
                var offNotes = allNotes.Where(n => n.NoteNumber == maxNote + 2).ToList();

                if (sysexOpens.Count != 0)
                {
                    foreach (var open in sysexOpens)
                    {
                        List<long> times = new List<long>();
                        var toMod = notes.Where(sysexOpens => sysexOpens.Time >= open.Time && sysexOpens.Time < open.Length).ToList();
                        /*if (toMod.Count != 1)
                        {
                            songQb.AddTimedError("Open note Sysex event found on chord with more than one note", trackNames[trackName], (long)open.Time);
                            continue;
                        }*/
                        foreach (var mod in toMod)
                        {
                            if (times.Contains(mod.Time))
                            {
                                songQb.AddTimedError("Open note Sysex event found on chord with more than one note", trackNames[trackName], (long)open.Time);
                                continue;
                            }
                            if (mod.NoteName == NoteName.C)
                            {
                                mod.NoteNumber -= (SevenBitNumber)1;
                            }
                            times.Add(mod.Time);
                        }
                    }
                }

                PlayNotes = songQb.MakeGuitar(chords, onNotes, offNotes, noteDict);
                // Process tap notes
                TapNotes = ProcessTapNotes(allNotes, chords, songQb);
                if (TapNotes.Count == 0 && sysexTaps != null)
                {
                    var wtdeTaps = songQb.GetConsole() == CONSOLE_PC && songQb.GetGame() == GAME_GHWT;
                    var gh3PlusTaps = songQb.GetGame() == GAME_GH3 && songQb.Gh3Plus;
                    bool filterChords = !(wtdeTaps || gh3PlusTaps);
                    var sysTapsTemp = new List<StarPower>();
                    foreach (var taps in sysexTaps)
                    {
                        var tapUnder = chords.Where(x => x.Time >= taps.Time && x.Time < taps.Length).ToList();
                        if (tapUnder.Count == 0)
                        {
                            // If there are no chords under the tap note, skip it
                            continue;
                        }
                        var noteList = new List<List<MidiData.Chord>>();
                        var tempList = new List<MidiData.Chord>();

                        foreach (var chord in tapUnder)
                        {
                            if (filterChords)
                            {
                                if (chord.Notes.Count > 1)
                                {
                                    if (tempList.Count == 0)
                                    {
                                        continue;
                                    }
                                    noteList.Add(tempList);
                                    tempList = new List<MidiData.Chord>();
                                    continue;
                                }
                            }
                            else
                            {
                                tempList.Add(chord);
                            }
                        }
                        if (tempList.Count == 0 && noteList.Count == 0)
                        {
                            continue;
                        }
                        if (tempList.Count != 0)
                        {
                            noteList.Add(tempList);
                        }
                        foreach (var tapList in noteList)
                        {
                            var lastNote = chords.IndexOf(tapList[tapList.Count - 1]);
                            var nextNote = chords.ElementAtOrDefault(lastNote + 1);
                            var startTime = songQb.GameMilliseconds(tapList[0].Time);
                            int endTime;
                            if (nextNote != null)
                            {
                                endTime = songQb.GameMilliseconds(nextNote.Time);
                            }
                            else
                            {
                                // If there are no more notes, set the end time to the last sysex event + 1/4 note
                                endTime = songQb.GameMilliseconds(tapList[tapList.Count - 1].EndTime + songQb.TPB);
                            }
                            var length = endTime - startTime;
                            TapNotes.Add(new StarPower(startTime, length, 1));
                        }
                    }
                }
                if (Game == GAME_GH3 && songQb.Gh3Plus)
                {
                    foreach (var tapMarker in TapNotes)
                    {
                        var toMod = PlayNotes.Where(n => n.Time >= tapMarker.Time && n.Time < tapMarker.Time + tapMarker.Length).ToList();
                        foreach (var note in toMod)
                        {
                            note.Note += 0x40; // Tap note in GH3_Plus
                        }
                    }
                }
                StarEntries = CalculateStarPowerNotes(chords, StarPowerPhrases, songQb);
                BattleStarEntries = CalculateStarPowerNotes(chords, BattleStarPhrases, songQb);
                if (FaceOffStarPhrases != null && diffName == EASY)
                {
                    // Should only activate on the easy difficulty and nowhere else
                    FaceOffStar = CalculateStarPowerNotes(chords, FaceOffStarPhrases, songQb);
                }

            }
            public List<StarPower> ProcessTapNotes(List<MidiData.Note> allNotes, List<MidiData.Chord> chords, SongQbFile songQb)
            {
                List<StarPower> allTaps = new List<StarPower>();
                var wtdeTaps = songQb.GetConsole() == CONSOLE_PC && songQb.GetGame() == GAME_GHWT;
                var gh3PlusTaps = songQb.GetGame() == GAME_GH3 && songQb.Gh3Plus;
                bool filterChords = !(wtdeTaps || gh3PlusTaps);
                // Extract Tap Notes 
                var tapNotes = allNotes.Where(x => x.NoteNumber == TapNote).ToList();
                foreach (var note in tapNotes)
                {
                    var currTime = note.Time;
                    int startTime;
                    int endTime;
                    int length;
                    int noteCount = 0;
                    if (filterChords)
                    {
                        var tapUnder = chords.Where(x => x.Time >= note.Time && x.Time < note.EndTime).ToList();
                        for (int i = 0; i < tapUnder.Count; i++)
                        {
                            var chord = tapUnder[i];
                            if (chord.Notes.Count > 1)
                            {
                                if (noteCount == 0)
                                {
                                    currTime = chord.EndTime;
                                    continue;
                                }
                                startTime = (int)Math.Round(songQb.TicksToMilliseconds(currTime));
                                endTime = (int)Math.Round(songQb.TicksToMilliseconds(chord.Time));
                                length = endTime - startTime;
                                allTaps.Add(new StarPower(startTime, length, 1));
                                currTime = chord.EndTime;
                                noteCount = 0;
                            }
                            else
                            {
                                noteCount++;
                            }
                        }
                    }
                    else
                    {
                        noteCount++;
                    }
                    if (noteCount != 0)
                    {
                        startTime = (int)Math.Round(songQb.TicksToMilliseconds(currTime));
                        endTime = (int)Math.Round(songQb.TicksToMilliseconds(note.EndTime));
                        length = endTime - startTime;
                        allTaps.Add(new StarPower(startTime, length, 1));
                    }
                }
                return allTaps;
            }
            public void ProcessDifficultyDrums(List<MidiData.Note> allNotes, int minNote, int maxNote, Dictionary<MidiTheory.NoteName, int> noteDict, int openNotes, SongQbFile songQb, List<MidiData.Note> StarPowerPhrases, List<MidiData.Note> BattleStarPhrases, List<MidiData.Note> FaceOffStarPhrases = null)
            {
                var currGame = songQb.GetGame();
                if (currGame == GAME_GH3 || currGame == GAME_GHA)
                {
                    return;
                }
                else if (currGame == GAME_GHWT)
                {
                    //openNotes = 0;
                }

                var notes = allNotes.Where(n => n.NoteNumber >= (minNote - openNotes) && n.NoteNumber <= maxNote);

                //var chords = notes.GetChords().ToList();
                var chords = GroupNotes(notes, DefaultTickThreshold * DefaultTPB / songQb.TPB).ToList();
                if (maxNote == ExpertNoteMax + 1)
                {
                    (chords, ExpertPlus) = NormalizeExpertPlus(chords);
                    if (currGame == GAME_GHWT)
                    {
                        var (expert, expertPlus) = SplitWTDrums(chords);
                        chords = expert;
                        if (ExpertPlus)
                        {
                            Console.WriteLine("Splitting WT drums into Expert and Expert Plus versions.");
                            ExPlusNotes = songQb.MakeDrums(expertPlus, noteDict);
                        }
                    }
                }

                PlayNotes = songQb.MakeDrums(chords, noteDict);
                StarEntries = CalculateStarPowerNotes(chords, StarPowerPhrases, songQb);
                BattleStarEntries = CalculateStarPowerNotes(chords, BattleStarPhrases, songQb);
                if (FaceOffStarPhrases != null && diffName == EASY)
                {
                    // Should only activate on the easy difficulty and nowhere else
                    FaceOffStar = CalculateStarPowerNotes(chords, FaceOffStarPhrases, songQb);
                }
            }
            private (List<MidiData.Chord>, bool) NormalizeExpertPlus(List<MidiData.Chord> chords)
            {
                bool isExpertPlus = false;
                var newNotes = new List<MidiData.Chord>();
                foreach (var chord in chords)
                {
                    bool hasNormalExpert = false;
                    bool hasExpertPlus = false;
                    MidiData.Note? xPlusNote = null;
                    var newChord = new List<MidiData.Note>();
                    foreach (var note in chord.Notes)
                    {
                        newChord.Add(note);
                        if (note.NoteName == NoteName.C)
                        {
                            hasNormalExpert = true;
                            xPlusNote = (MidiData.Note)note.Clone();
                            xPlusNote.NoteNumber -= (SevenBitNumber)1; // Make it a B note
                        }
                        else if (note.NoteName == NoteName.B)
                        {
                            hasExpertPlus = true;
                            isExpertPlus = true;
                        }
                    }
                    if (hasNormalExpert && !hasExpertPlus)
                    {
                        newChord.Add(xPlusNote);
                    }
                    newNotes.Add(new MidiData.Chord(newChord));
                }
                if (isExpertPlus)
                {
                    // If there are Expert Plus notes, return the new list
                    return (newNotes, isExpertPlus);
                }
                // Otherwise, return the original list
                return (chords, isExpertPlus);
            }
            private (List<MidiData.Chord> expert, List<MidiData.Chord> expertPlus) SplitWTDrums(List<MidiData.Chord> chords)
            {
                var expertNotes = new List<MidiData.Chord>();
                var expertPlusNotes = new List<MidiData.Chord>();

                foreach (var chord in chords)
                {
                    var expertChord = new List<MidiData.Note>();
                    var expertPlusChord = new List<MidiData.Note>();
                    foreach (var note in chord.Notes)
                    {
                        if (note.NoteName == NoteName.C)
                        {
                            expertChord.Add(note);
                        }
                        else if (note.NoteName == NoteName.B)
                        {
                            note.NoteNumber += (SevenBitNumber)1;
                            expertPlusChord.Add(note);
                        }
                        else
                        {
                            expertChord.Add(note);
                            expertPlusChord.Add(note);
                        }
                    }
                    if (expertChord.Count > 0)
                    {
                        expertNotes.Add(new MidiData.Chord(expertChord));
                    }
                    if (expertPlusChord.Count > 0)
                    {
                        expertPlusNotes.Add(new MidiData.Chord(expertPlusChord));
                    }
                }
                return (expertNotes, expertPlusNotes);
            }
            private List<StarPower> CalculateStarPowerNotes(List<MidiData.Chord> playNotes, List<MidiData.Note> starNotes, SongQbFile songQb)
            {
                List<StarPower> stars = new List<StarPower>();
                foreach (MidiData.Note SpPhrase in starNotes)
                {
                    var noteCount = playNotes.Where(x => x.Time >= SpPhrase.Time && x.Time < SpPhrase.EndTime).Count();
                    int startTime = (int)Math.Round(songQb.TicksToMilliseconds(SpPhrase.Time));
                    int endTime = (int)Math.Round(songQb.TicksToMilliseconds(SpPhrase.EndTime));
                    int length = endTime - startTime;
                    stars.Add(new StarPower(startTime, length, noteCount));
                }
                return stars;
            }
            public QBItem CreateNotesBase(string songName)
            {
                string fullName = $"{songName}_{diffName}";
                QBItem currItem = new QBItem();
                currItem.SetName(fullName);
                currItem.SetInfo(ARRAY);
                return currItem;
            }
            public QBItem CreateStarBase(string songName, string starType, bool isFaceoff = false)
            {
                string fullName;
                if (!isFaceoff)
                {
                    fullName = $"{songName}_{diffName}_{starType}";
                }
                else
                {
                    fullName = $"{songName}_{starType}";
                }
                QBItem currItem = new QBItem();
                currItem.SetName(fullName);
                currItem.SetInfo(ARRAY);
                return currItem;
            }
            public QBItem CreateGH3Notes(string songName, bool gh3Plus = false) // Combine this with SP
            {
                QBItem currItem = CreateNotesBase(songName);
                if (PlayNotes == null)
                {
                    currItem.MakeEmpty();
                    return currItem;
                }
                QBArrayNode notes = new QBArrayNode();
                foreach (PlayNote playNote in PlayNotes)
                {
                    notes.AddIntToArray(playNote.Time);
                    notes.AddIntToArray(playNote.Length);
                    if ((playNote.IsHopo || playNote.ForcedOn) && !playNote.ForcedOff)
                    {
                        if (BitOperations.IsPow2(playNote.Note) || gh3Plus) // Can't have chords being hopos (2025-07-16: Unless it's GH3+)
                        {
                            playNote.Note |= GH3FORCE;
                        }
                    }
                    notes.AddIntToArray(playNote.Note);
                }
                currItem.SetData(notes);
                return currItem;
            }
            public QBItem CreateGHWTNotes(string songName) // Combine this with SP
            {
                QBItem currItem = CreateNotesBase(songName);
                if (PlayNotes == null)
                {
                    currItem.MakeEmpty();
                    return currItem;
                }
                QBArrayNode notes = new QBArrayNode();
                foreach (PlayNote playNote in PlayNotes)
                {
                    notes.AddIntToArray(playNote.Time);

                    int noteData = playNote.Accents;
                    noteData <<= 1;  // Shift left to make space for hopoFlag.
                    noteData |= (playNote.IsHopo || playNote.ForcedOn) && !playNote.ForcedOff ? 1 : 0; // Add hopoFlag.
                    noteData <<= 6;  // Shift left to make space for noteString.
                    noteData |= playNote.Note & 0x3F; // Ensure note is only 6 bits and add it.
                    noteData <<= 16; // Shift left to make space for noteLength.
                    noteData |= playNote.Length & 0xFFFF; // Ensure length is only 16 bits and add it.

                    notes.AddIntToArray(noteData);
                }
                currItem.SetData(notes);
                return currItem;
            }
            public byte[] CreateGh5Notes(string name, ref int entries, bool ghwor = true)
            {

                var noteData = new List<byte>();
                uint notesSize = 8; // May need to change if I ever support Wii.

                uint tapSize = 8;

                bool drums = name == "drums";
                bool gh6drums = diffName == "expert" && ghwor && drums;
                string entryType;
                if (gh6drums)
                {
                    entryType = "gh6_expert_drum_note";
                    notesSize = 9;
                }
                else
                {
                    entryType = "gh5_instrument_note";
                }
                using (var stream = new MemoryStream())
                {
                    string notesName = $"{name}{diffName}instrument";
                    uint sectionQb = QBKeyUInt(notesName);
                    string spName = $"{name}{diffName}starpower";
                    uint spQb = QBKeyUInt(spName);
                    string tapName = $"{name}{diffName}tapping";
                    uint tapQb = QBKeyUInt(tapName);

                    MakeGh5NoteHeader(stream, notesName, PlayNotes.Count, entryType, notesSize);
                    if (gh6drums)
                    {
                        foreach (PlayNote note in PlayNotes)
                        {
                            // Unset any bits in note.Note that are also set in note.Ghosts
                            note.Note &= ~note.Ghosts;

                            _readWriteGh5.WriteInt32(stream, note.Time);
                            _readWriteGh5.WriteInt16(stream, (short)note.Length);
                            _readWriteGh5.WriteInt8(stream, (byte)note.Note);
                            _readWriteGh5.WriteInt8(stream, (byte)note.Accents);
                            _readWriteGh5.WriteInt8(stream, (byte)note.Ghosts);
                        }
                    }
                    else
                    {
                        foreach (PlayNote note in PlayNotes)
                        {
                            if ((note.IsHopo || note.ForcedOn) && !note.ForcedOff)
                            {
                                note.Note |= GH5FORCE;
                            }
                            _readWriteGh5.WriteInt32(stream, note.Time);
                            _readWriteGh5.WriteInt16(stream, (short)note.Length);
                            _readWriteGh5.WriteInt8(stream, (byte)note.Note);
                            _readWriteGh5.WriteInt8(stream, (byte)note.Accents);
                        }
                    }
                    entries++;

                    if (!drums)
                    {
                        MakeGh5NoteHeader(stream, tapName, TapNotes.Count, "gh5_tapping_note", tapSize);
                        foreach (StarPower tap in TapNotes)
                        {
                            _readWriteGh5.WriteInt32(stream, tap.Time);
                            _readWriteGh5.WriteInt32(stream, tap.Length);
                        }
                        entries++;
                    }

                    MakeNewStarPower(stream, spName, StarEntries, ref entries);


                    return stream.ToArray();
                }



            }
            public QBItem CreateStarPowerPhrases(string songName)
            {
                QBItem star = CreateStarBase(songName, STAR);
                if (StarEntries == null)
                {
                    star.MakeEmpty();
                    return star;
                }
                star.SetData(MakeStarArray(StarEntries));

                return star;
            }
            public QBItem CreateFaceOffStar(string songName)
            {
                QBItem star = CreateStarBase(songName, FACEOFFSTAR, true);
                if (FaceOffStar == null)
                {
                    star.MakeEmpty();
                    return star;
                }
                star.SetData(MakeStarArray(FaceOffStar));

                return star;
            }
            public QBItem CreateBattleStarPhrases(string songName, bool makeBlank)
            {
                QBItem starBM = CreateStarBase(songName, STAR_BM);
                if (StarEntries == null)
                {
                    starBM.MakeEmpty();
                    return starBM;
                }
                if (makeBlank)
                {
                    QBArrayNode starData = new QBArrayNode();
                    starData.MakeEmpty();
                    starBM.SetData(starData);
                }
                else
                {
                    starBM.SetData(MakeStarArray(BattleStarEntries));
                }


                return starBM;
            }
            public QBItem CreateTapPhrases(string songName)
            {
                QBItem tap = CreateStarBase(songName, TAP);
                if (TapNotes == null)
                {
                    tap.MakeEmpty();
                    return tap;
                }
                tap.SetData(MakeStarArray(TapNotes));

                return tap;
            }
            public QBItem CreateWhammyController(string songName)
            {
                QBItem whammy = CreateStarBase(songName, WHAMMYCONTROLLER);
                whammy.MakeEmpty();
                // No whammy data for now, need to figure out how to implement it
                return whammy;
            }
            private QBArrayNode MakeStarArray(List<StarPower> starList)
            {
                QBArrayNode starData = new QBArrayNode();
                foreach (StarPower star in starList)
                {
                    QBArrayNode starEntry = new QBArrayNode();
                    starEntry.AddIntToArray(star.Time);
                    starEntry.AddIntToArray(star.Length);
                    starEntry.AddIntToArray(star.NoteCount);
                    starData.AddArrayToArray(starEntry);
                }
                return starData;
            }
            private static int countSetBits(int n, bool oldGame = true)
            {
                if (oldGame)
                {
                    n &= 31;
                }
                int count = 0;
                while (n > 0)
                {
                    n &= (n - 1);
                    count++;
                }
                return count;
            }
            private static int GetMultiplier(int noteCount)
            {
                if (noteCount < 10)
                {
                    return 1;
                }
                else if (noteCount < 20)
                {
                    return 2;
                }
                else if (noteCount < 30)
                {
                    return 3;
                }
                else 
                {
                    return 4;
                }
            }
            /*
            public int GetBaseScore(List<int> fretbars)
            {

                var debugMeasures = new Dictionary<int, float>();
                if (PlayNotes.Count == 0)
                {
                    return 0;
                }
                var score = 0f;
                var baseScore = 0f;
                var multiplier = 1;
                var beat_time = fretbars[1] - fretbars[0];
                var orig_beat_time = beat_time;
                var origSustainCheck = orig_beat_time / 2.0;
                
                var fIndex = 0;
                var nIndex = 0;
                var sim_bot_note_count = 0;
                while (nIndex < PlayNotes.Count - 1)
                {
                    var note = PlayNotes[nIndex];
                    //note.Time += (66 + 66 + 14 + 33);
                    sim_bot_note_count++;
                    multiplier = GetMultiplier(sim_bot_note_count);
                    while (fretbars[fIndex + 1] <= note.Time)
                    {
                        beat_time = fretbars[fIndex+1] - fretbars[fIndex];
                        fIndex++;
                    }
                    int currMeasure = fIndex / 4;
                    if (!debugMeasures.ContainsKey(currMeasure))
                    {
                        debugMeasures[currMeasure] = 0;
                    }

                    beat_time = fretbars[fIndex + 1] - fretbars[fIndex];
                    var currSustainCheck = beat_time / 2.0f;
                    var currBeatLine = fretbars[fIndex];
                    var nextBeatLine = fretbars[fIndex + 1];
                    var length = note.Length;
                    var notesInChord = countSetBits(note.Note);
                    var baseNoteScore = notesInChord * BASENOTE;
                    baseScore += baseNoteScore;
                    score += baseNoteScore * multiplier;
                    debugMeasures[currMeasure] += baseNoteScore;
                    var toShorten = (WHAMMYSHORTENGH3 * beat_time);
                    var lengthEdit = length - toShorten;

                    if (lengthEdit > currSustainCheck)
                    {
                        var sustainPoints = 0f;
                        int totalNoteLength = length;
                        var sustainValueBeat = totalNoteLength;
                        var finished = false;
                        var sustainFretBarCount = fIndex;
                        var noteStart = true;
                        bool altPath = false;
                        
                        while (!finished)
                        {
                            var currSustainFret = fretbars[sustainFretBarCount];
                            var nextSustainFret = fretbars[sustainFretBarCount + 1];
                            var segmentDuration = nextSustainFret - currSustainFret;
                            var noteLength = segmentDuration;
                            if (currSustainFret <= note.Time)
                            {
                                var timeFromBeatLine = nextSustainFret - note.Time;
                                noteLength = timeFromBeatLine;
                            }
                            if (totalNoteLength <= noteLength)
                            {
                                finished = true;
                                noteLength = totalNoteLength;
                            }
                            else
                            {
                                totalNoteLength -= noteLength;
                            }

                            var additionalScore = POINTSPERBEAT;
                            additionalScore *= noteLength;
                            additionalScore /= segmentDuration;
                            sustainPoints += additionalScore;

                            int currSustainMeasure = sustainFretBarCount / 4;

                            if (debugMeasures.ContainsKey(currSustainMeasure))
                            {
                                debugMeasures[currSustainMeasure] += additionalScore;
                            }
                            else
                            {
                                debugMeasures[currSustainMeasure] = additionalScore;
                            }

                            sustainFretBarCount++;

                        }
                        var roundedWhammy = (int)(sustainPoints + 0.5f); // Round like the original
                        baseScore += roundedWhammy;
                        score += roundedWhammy * multiplier; // Apply streak multiplier

                    }
                    nIndex++;
                }

                return 0;
            }*/
            /// <summary>
            /// Calculates the base score and No-SP score for a song based on played notes and fretbar positions.
            /// </summary>
            /// <param name="fretbars">List of fretbar positions (in milliseconds) that define the beat structure</param>
            /// <returns>Tuple containing (baseScore, multipliedScore) where:
            ///     baseScore - The raw score without streak multipliers
            ///     multipliedScore - The score with streak multipliers applied</returns>
            public (int, int) GetSongScores(List<int> fretbars)
            {
                // Early exit if no notes were played
                if (PlayNotes.Count == 0)
                {
                    return (0, 0);
                }

                // Score tracking variables
                var score = 0f;          // Score with streak multipliers applied
                var baseScore = 0f;      // Raw score without multipliers
                var multiplier = 1;      // Current streak multiplier
                var lastMultiplier = 1;  // Previous streak multiplier (for detecting changes)

                // Timing variables
                var beat_time = fretbars[1] - fretbars[0];  // Duration of current beat
                var origSustainCheck = beat_time / 2.0f;    // Threshold for sustain notes (half beat)

                // Sustain tracking variables
                var activeSustainTime = 0;    // Start time of active sustain
                var activeSustainLength = 0;  // Length of active sustain

                // Position tracking variables
                var fIndex = 0;              // Current position in fretbars list
                var sim_bot_note_count = 0;  // Count of consecutive notes hit (for streak multiplier)

                // Process each played note
                foreach (var note in PlayNotes)
                {
                    // Update streak multiplier
                    sim_bot_note_count++;
                    multiplier = GetMultiplier(sim_bot_note_count);
                    bool multiplierChanged = multiplier != lastMultiplier;
                    lastMultiplier = multiplier;

                    // Advance fretbar index to current note's timing
                    UpdateFretIndex(ref fIndex, fretbars, note.Time);
                    beat_time = fretbars[fIndex + 1] - fretbars[fIndex];

                    // Calculate base score for this note (based on number of notes in chord)
                    var notesInChord = countSetBits(note.Note);
                    var baseNoteScore = notesInChord * BASENOTE;

                    // Add to both base and multiplied scores
                    baseScore += baseNoteScore;
                    score += baseNoteScore * multiplier;

                    // Check if this note qualifies for sustain points
                    var length = note.Length;
                    var timePlusLengthCurrent = note.Time + length;
                    var timePlusLengthCheck = activeSustainTime + activeSustainLength;
                    bool skipLengthCalc = timePlusLengthCurrent <= timePlusLengthCheck;
                    bool sustainCalcCheck = length > origSustainCheck;

                    if (sustainCalcCheck)
                    {
                        if (note.Time < timePlusLengthCheck && timePlusLengthCurrent > timePlusLengthCheck)
                        {
                            note.Time = timePlusLengthCheck;
                            length = timePlusLengthCurrent - timePlusLengthCheck;
                            while (fretbars[fIndex + 1] <= note.Time)
                            {
                                beat_time = fretbars[fIndex + 1] - fretbars[fIndex];
                                fIndex++;
                            }
                        }
                        activeSustainTime = note.Time;
                        activeSustainLength = length;


                        var sustainPoints = CalculateSustainPoints(note.Time, length, fIndex, fretbars, multiplier);
                        var roundedSustain = (int)(sustainPoints + 0.5f); // Round like the original
                        if ((!skipLengthCalc))
                        {
                            baseScore += roundedSustain;
                            score += roundedSustain * multiplier; // Apply streak multiplier
                        }
                        
                        

                    }
                    else if (multiplierChanged && sustainCalcCheck)
                    {
                        multiplier = 1; // This is if the multiplier changes on an extended sustain. You need to reset the multiplier to 1 to add the portion of the sustain that has an additional multiplier.

                        var sustainPoints = CalculateSustainPoints(note.Time, length, fIndex, fretbars, multiplier);

                        var roundedSustain = (int)(sustainPoints + 0.5f); // Round like the original
                        score += roundedSustain * multiplier; // Apply streak multiplier
                    }
                    multiplierChanged = false;
                }
                Console.WriteLine($"{nameProper} Base Score: {baseScore}, {nameProper} No SP Score: {score}");
                return ((int)baseScore, (int)score);
            }
            /// <summary>
            /// Updates the fretbar index to the current note's timing position
            /// </summary>
            /// <param name="fIndex">Current fretbar index (will be updated)</param>
            /// <param name="fretbars">List of fretbar positions</param>
            /// <param name="noteTime">Current note's time position</param>
            private void UpdateFretIndex(ref int fIndex, List<int> fretbars, int noteTime)
            {
                // Advance through fretbars until we find the one containing this note
                while (fIndex + 1 < fretbars.Count && fretbars[fIndex + 1] <= noteTime)
                {
                    fIndex++;
                }
            }

            /// <summary>
            /// Calculates additional score points for sustain notes
            /// </summary>
            /// <param name="noteTime">Start time of the note</param>
            /// <param name="length">Duration of the note</param>
            /// <param name="startFIndex">Starting fretbar index</param>
            /// <param name="fretbars">List of fretbar positions</param>
            /// <param name="scoreMultiplier">Multiplier to apply to sustain points</param>
            /// <returns>Calculated sustain points</returns>
            private float CalculateSustainPoints(
                int noteTime,
                int length,
                int startFIndex,
                List<int> fretbars,
                int scoreMultiplier)
            {
                var sustainPoints = 0f;
                int totalNoteLength = length;
                var sustainValueBeat = totalNoteLength;

                var finished = false;
                var sustainFretBarCount = startFIndex;

                while (!finished)
                {
                    var currSustainFret = fretbars[sustainFretBarCount];
                    var nextSustainFret = fretbars[sustainFretBarCount + 1];
                    var segmentDuration = nextSustainFret - currSustainFret;
                    var noteLength = segmentDuration;
                    if (currSustainFret <= noteTime)
                    {
                        var timeFromBeatLine = nextSustainFret - noteTime;
                        noteLength = timeFromBeatLine;
                    }
                    if (totalNoteLength <= noteLength)
                    {
                        finished = true;
                        noteLength = totalNoteLength;
                    }
                    else
                    {
                        totalNoteLength -= noteLength;
                    }


                    var additionalScore = POINTSPERBEAT;
                    additionalScore *= noteLength;
                    additionalScore /= segmentDuration;
                    sustainPoints += additionalScore;

                    sustainFretBarCount++;

                }

                return sustainPoints;
            }

            /* Base score calculation that works
             public (int, int) GetSongScores(List<int> fretbars)
            {
                var debugMeasures = new Dictionary<int, (float simScore, float otherScore)>();
                if (PlayNotes.Count == 0)
                {
                    return (0, 0);
                }
                var score = 0f;
                var baseScore = 0f;
                var multiplier = 1;
                var beat_time = fretbars[1] - fretbars[0];
                var orig_beat_time = beat_time;
                var origSustainCheck = orig_beat_time / 2.0;

                var fIndex = 0;
                var nIndex = 0;
                var sim_bot_note_count = 0;
                while (nIndex < PlayNotes.Count)
                {
                    var note = PlayNotes[nIndex];
                    //note.Time += (66 + 66 + 14 + 33);
                    sim_bot_note_count++;
                    multiplier = GetMultiplier(sim_bot_note_count);
                    while (fretbars[fIndex + 1] <= note.Time)
                    {
                        beat_time = fretbars[fIndex + 1] - fretbars[fIndex];
                        fIndex++;
                    }
                    int currMeasure = fIndex / 4;
                    if (!debugMeasures.ContainsKey(currMeasure+1))
                    {
                        debugMeasures[currMeasure+1] = (0, 0);
                    }
                    beat_time = fretbars[fIndex + 1] - fretbars[fIndex];
                    var currSustainCheck = beat_time / 2.0f;
                    var currBeatLine = fretbars[fIndex];
                    var nextBeatLine = fretbars[fIndex + 1];
                    var length = note.Length;
                    var notesInChord = countSetBits(note.Note);
                    var baseNoteScore = notesInChord * BASENOTE;
                    baseScore += baseNoteScore;
                    score += baseNoteScore * multiplier;
                    
                    debugMeasures[currMeasure+1] = (score, debugMeasures[currMeasure + 1].otherScore + baseNoteScore);
                    if (length > origSustainCheck)
                    {
                        var sustainPoints = 0f;
                        int totalNoteLength = length;
                        var sustainValueBeat = totalNoteLength;
                        var finished = false;
                        var sustainFretBarCount = fIndex;
                        var noteStart = true;
                        bool altPath = false;
                        int currSustainMeasure = sustainFretBarCount / 4;

                        while (!finished)
                        {
                            var currSustainFret = fretbars[sustainFretBarCount];
                            var nextSustainFret = fretbars[sustainFretBarCount + 1];
                            var segmentDuration = nextSustainFret - currSustainFret;
                            var noteLength = segmentDuration;
                            if (currSustainFret <= note.Time)
                            {
                                var timeFromBeatLine = nextSustainFret - note.Time;
                                noteLength = timeFromBeatLine;
                            }
                            if (totalNoteLength <= noteLength)
                            {
                                finished = true;
                                noteLength = totalNoteLength;
                            }
                            else
                            {
                                totalNoteLength -= noteLength;
                            }
                           

                            var additionalScore = POINTSPERBEAT;
                            additionalScore *= noteLength;
                            additionalScore /= segmentDuration;
                            sustainPoints += additionalScore;

                            currSustainMeasure = sustainFretBarCount / 4;

                            if (debugMeasures.ContainsKey(currSustainMeasure+1))
                            {
                                var simScore = debugMeasures[currSustainMeasure+1].simScore;
                                var otherScore = debugMeasures[currSustainMeasure+1].otherScore;
                                debugMeasures[currSustainMeasure+1] = (simScore + (additionalScore * multiplier), otherScore + additionalScore);
                            }
                            else
                            {
                                // If the measure doesn't exist, create it with the current score
                                var prevScore = debugMeasures[currSustainMeasure];
                                debugMeasures[currSustainMeasure+1] = (prevScore.simScore + (additionalScore * multiplier), additionalScore); ;
                            }

                            sustainFretBarCount++;

                        }
                        var roundedWhammy = (int)(sustainPoints + 0.5f); // Round like the original

                        baseScore += roundedWhammy;
                        score += roundedWhammy * multiplier; // Apply streak multiplier
                        debugMeasures[currSustainMeasure+1] = (score, debugMeasures[currSustainMeasure + 1].otherScore);

                    }
                    nIndex++;
                }
                Console.WriteLine($"{nameProper} Base Score: {baseScore}, {nameProper} No SP Score: {score}");
                return ((int)baseScore, (int)score);
            }
             */
        }
        [DebuggerDisplay("{Time} - {Text}")]
        public class Marker
        {
            public int Time { get; set; }
            public string Text { get; set; }
            public string QsKeyString
            {
                get
                {
                    return QBKeyQs(Text);
                }
            }
            public uint QsKey
            {
                get
                {
                    return QSKeyUInt(Text);
                }
            }
            public Marker(int time, string text)
            {
                Time = time;
                Text = text;
            }
            public QBStructData ToStruct(string console = CONSOLE_XBOX)
            {
                string markerType = console == CONSOLE_PS2 ? STRING : WIDESTRING;
                QBStructData marker = new QBStructData();
                marker.AddIntToStruct("Time", Time);
                if (Text.ToLower().StartsWith("0x"))
                {
                    throw new ArgumentException("Text cannot start with 0x");
                    //marker.AddVarToStruct("Marker", Text, POINTER);
                }
                else
                {
                    marker.AddVarToStruct("Marker", Text, markerType);
                }
                return marker;
            }
            public QBStructData ToStructQs()
            {
                string markerType = QSKEY;
                QBStructData marker = new QBStructData();
                marker.AddIntToStruct("Time", Time);
                marker.AddVarToStruct("Marker", QsKeyString, markerType);
                return marker;
            }
        }
        [DebuggerDisplay("{(float)Time/1000, nq}: {Length} ms long ({NoteCount} Notes)")]
        public class StarPower
        {
            public int Time { get; set; }
            public int Length { get; set; }
            public int NoteCount { get; set; }
            public StarPower()
            {

            }
            public StarPower(int time, int length, int noteCount)
            {
                Time = time;
                Length = length;
                NoteCount = noteCount;
            }
            public void SetTime(int time)
            {
                Time = time;
            }
            public void SetLength(int length)
            {
                Length = length;
            }
            public void SetNoteCount(int noteCount)
            {
                NoteCount = noteCount;
            }
        }
        [DebuggerDisplay("{(float)Time/1000, nq}: {Length} ms long ({Points} Points)")]
        public class Freeform
        {
            public int Time { get; set; }
            public int Length { get; set; }
            public int Points { get; set; }
            public Freeform(int time, int length, int points)
            {
                Time = time;
                Length = length;
                Points = points;
            }
        }
        [DebuggerDisplay("{(float)Time/1000, nq}: {NoteColor, nq} - {Length} ms long (Hopo: {IsHopo}, Force On: {ForcedOn}, Force Off: {ForcedOff})")]
        public class PlayNote
        {
            public int Time { get; set; }
            public int Note { get; set; }
            public int Length { get; set; }
            public int Accents { get; set; } = AllAccents;
            public int Ghosts { get; set; }
            public bool ForcedOn { get; set; }
            public bool ForcedOff { get; set; }
            public bool IsHopo { get; set; } // A natural Hopo from being within a certain distance of another note
            public bool IsForcedGh3 { get; set; } // A forced note. It flips whatever the note is currently. Should only be set if reading GH3 notes
            public string Type { get; set; }

            // Property to get color name
            public string NoteColor
            {
                get
                {
                    if (Type == "Vocals")
                    {
                        return $"{Note}";
                    }
                    else
                    {
                        List<string> colors = new List<string>();
                        foreach (Colours color in Enum.GetValues(typeof(Colours)))
                        {
                            if ((Note & (int)color) == (int)color)
                            {
                                colors.Add(color.ToString());
                            }
                        }

                        return colors.Count > 0 ? string.Join("+", colors) : "None";
                    }
                }
            }
            public PlayNote(int time, int note, int length, string type = "")
            {
                Time = time;
                Note = note;
                Length = length;
                Type = type;
            }
            public PlayNote(int time, int note, int length, int accents, bool isHopo)
            {
                Time = time;
                Note = note;
                Length = length;
                Accents = accents;
                IsHopo = isHopo;
            }
            public PlayNote(int time, int note, int length, bool isForced)
            {
                Time = time;
                Note = note;
                Length = length;
                IsForcedGh3 = isForced;
            }
        }
        public class KickNote
        {
            public long EndTimeTicks { get; set; }
            public int NoteBit { get; set; }
            public KickNote(long endTimeTicks)
            {
                EndTimeTicks = endTimeTicks;
            }
        }
        [DebuggerDisplay("{Time} - {Text}")]
        public class VocalPhrase
        {

            public long Time { get; set; }
            public long EndTime { get; set; }
            public int TimeMills { get; set; }
            public int Player { get; set; }
            public string Text { get; set; }
            public VocalPhraseType Type { get; set; }
            public VocalPhrase(long time, long endTime, int player)
            {
                Time = time;
                EndTime = endTime;
                Player = player;
                // Can be 0, 1, 2, or 3
                // 0 = No player
                // 1 = Player 1
                // 2 = Player 2
                // 3 = Both players
            }
            public void SetTimeMills(int time)
            {
                TimeMills = time;
            }
            public void SetPlayer(int player)
            {
                Player = player;
            }
            public void SetText(string text)
            {
                Text = text;
                if (text == string.Empty)
                {
                    SetPlayer(0);
                }
            }
            public void SetType(VocalPhraseType type)
            {
                Type = type;
            }
        }
        [DebuggerDisplay("{Time} - {Text}")]
        public class VocalLyrics
        {
            public int Time { get; set; }
            public string Text { get; set; }
            public VocalLyrics(int time, string text)
            {
                Time = time;
                Text = text;
            }
        }
        [DebuggerDisplay("{(float)Time/1000, nq}: {Length} ms long")]
        public class FaceOffSection
        {
            public int Time { get; set; }
            public int Length { get; set; }
            public FaceOffSection(int time, int length)
            {
                Time = time;
                Length = length;
            }
        }
        [DebuggerDisplay("{(float)Time/1000, nq}: Note {Note}, Velocity: {Velocity}, {Length} ms long")]
        public class AnimNote // This is for all animation notes, not just the "anim_notes" array. Also cameras, lights, etc.
        {
            public int Time { get; set; }
            public int Note { get; set; }
            public int Length { get; set; }
            public int Velocity { get; set; }
            public AnimNote(int time, int note, int length, int velocity)
            {
                Time = time;
                Note = Math.Min(note, 255); // Ensure note does not exceed 255
                Length = Math.Min(length, 65535); // Ensure length does not exceed 65535
                Velocity = Math.Min(velocity, 255); // Ensure velocity does not exceed 255
            }
            public QBArrayNode ToGH3Anim()
            {
                QBArrayNode animNote = new QBArrayNode();
                animNote.AddIntToArray(Time);
                animNote.AddIntToArray(Note);
                animNote.AddIntToArray(Length);
                return animNote;
            }
            public void ToWtAnim(QBArrayNode animNote)
            {
                animNote.AddIntToArray(Time);

                // Construct the integer with velocity, note, and length.
                // Velocity occupies the highest 8 bits, so it's shifted left by 24 bits.
                // Note is next, so it's shifted left by 16 bits.
                // Length occupies the lowest 16 bits, so it's added directly without shifting.
                int velocityNoteLength = (Velocity << 24) | (Note << 16) | Length;

                animNote.AddIntToArray(velocityNoteLength);
                return;
            }
        }
        public class ScriptStruct
        {
            public int Time { get; set; }
            public string Script { get; set; }
            public QBStructData? Params { get; set; }
            public ScriptStruct(int time, string script, QBStructData? parameters = null)
            {
                Time = time;
                Script = script;
                if (parameters != null)
                {
                    Params = parameters;
                }
            }
            public QBStructData ToStruct()
            {
                QBStructData light = new QBStructData();
                light.AddIntToStruct("Time", Time);
                light.AddVarToStruct("scr", Script, QBKEY);
                if (Params != null)
                {
                    light.AddStructToStruct("Params", Params);
                }
                return light;
            }
        }
        private void GetTimeSigs(TrackChunk conductorTrack)
        {
            // Assuming the first track is the conductor track
            if (conductorTrack != null)
            {
                var events = conductorTrack.GetTimedEvents();
                foreach (var midiEvent in events)
                {
                    double timeInMilliseconds = 0;

                    if (midiEvent.Event is TimeSignatureEvent timeSignatureEvent)
                    {
                        // Convert ticks to time and store in milliseconds
                        timeInMilliseconds = midiEvent.TimeAs<MetricTimeSpan>(SongTempoMap).TotalMilliseconds;
                        if (timeInMilliseconds > 0 && TimeSigs.Count == 0)
                        {
                            TimeSigs.Add(new TimeSig(0, 4, 4)); // Default to 4/4 time signature if none present
                        }
                        TimeSigs.Add(new TimeSig((int)Math.Round(timeInMilliseconds), timeSignatureEvent.Numerator, timeSignatureEvent.Denominator));
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Conductor track not found.");
            }
            if (TimeSigs.Count == 0)
            {
                // If no time signatures were found, add a default one
                TimeSigs.Add(new TimeSig(0, 4, 4)); // Default to 4/4 time signature
            }
        }
        private void CalculateFretbars()
        {
            long lastEventTick = SongMidiFile.GetTimedEvents().Max(e => e.Time);
            LastEventTick = lastEventTick;
            var beatsInMilliseconds = new List<double>();

            var timeSignatureEvents = SongMidiFile.GetTrackChunks()
                                              .First()
                                              .GetTimedEvents()
                                              .Where(e => e.Event is TimeSignatureEvent)
                                              .Select(e => new
                                              {
                                                  Event = (TimeSignatureEvent)e.Event,
                                                  e.Time
                                              })
                                              .OrderBy(e => e.Time)
                                              .ToList();

            int currentTsIndex = 0;
            TimeSignature currentTimeSignature = timeSignatureEvents.Any() ?
            new TimeSignature(timeSignatureEvents[0].Event.Numerator,
                                timeSignatureEvents[0].Event.Denominator)
                                : TimeSignature.Default;
            Tempo currentTempo = Tempo.Default;
            long beatLengthInTicks = GetBeatLengthInTicks(currentTimeSignature);
            double beatLengthInMilliseconds;
            for (long tick = 0; tick <= lastEventTick;)
            {
                beatLengthInMilliseconds = TicksToMilliseconds(tick);

                Fretbars.Add((int)Math.Round(beatLengthInMilliseconds));

                tick += beatLengthInTicks;
                if (currentTsIndex + 1 < timeSignatureEvents.Count && tick >= timeSignatureEvents[currentTsIndex + 1].Time)
                {
                    // Update to the next time signature
                    currentTsIndex++;
                    var nextTsEvent = new TimeSignature(timeSignatureEvents[currentTsIndex].Event.Numerator,
                                timeSignatureEvents[currentTsIndex].Event.Denominator);
                    currentTimeSignature = new TimeSignature(nextTsEvent.Numerator, nextTsEvent.Denominator);
                    // There's a bug where it skips a fret bar if the TS Denom changes
                    beatLengthInMilliseconds = TicksToMilliseconds(tick);

                    Fretbars.Add((int)Math.Round(beatLengthInMilliseconds));

                    beatLengthInTicks = GetBeatLengthInTicks(currentTimeSignature);
                    tick += beatLengthInTicks;
                }
            }
            // Add the last fret bar for padding
            var fretCount = Fretbars.Count;
            var lastFretbar = Fretbars[fretCount - 1];
            var secondLastFretbar = Fretbars[fretCount - 2];
            var lastDiff = lastFretbar - secondLastFretbar;
            for (int i = 1; i < 5; i++)
            {
                var newFret = lastFretbar + (lastDiff * i);
                Fretbars.Add(newFret);
            }
            SetMeasuresForDebugging();

        }
        private void SetMeasuresForDebugging()
        {
            if (Fretbars.Count == 0)
            {
                throw new InvalidOperationException("Fretbars have not been calculated. Call CalculateFretbars() first.");
            }
            var fretbarIndex = 0;
            TimeSig currTs;
            TimeSig nextTs;
            bool lastTs;
            var currBeat = 0;
            var currMeasure = 0;
            (currTs, nextTs, lastTs) = GetNextTs(fretbarIndex);
            for (int i = 0; i < Fretbars.Count; i++)
            {
                var currFret = Fretbars[i];
                if (currFret >= nextTs.Time && !lastTs)
                {
                    if (currFret != nextTs.Time)
                    {
                        Console.WriteLine($"Fretbar {currFret} does not match next time signature {nextTs.Time}. This may cause issues in the song."); 
                    }
                    // Move to the next time signature
                    fretbarIndex++;
                    (currTs, nextTs, lastTs) = GetNextTs(fretbarIndex);
                    currBeat = 0; // Reset the beat count for the new time signature
                }
                var beatInMeasure = currBeat % currTs.Numerator;
                if (beatInMeasure == 0)
                {
                    currMeasure++;
                }
                Measures.Add((currMeasure, beatInMeasure + 1)); // Add the index of the measure
                currBeat++;
            }
            if (Fretbars.Count != Measures.Count)
            {
                Console.WriteLine($"Warning: Fretbars count ({Fretbars.Count}) does not match Measures count ({Measures.Count}). This may indicate a problem with the time signature calculations.");
            }
        }
        private (TimeSig, TimeSig, bool) GetNextTs(int tsIndex)
        {
            TimeSig currTs = TimeSigs[tsIndex];
            bool lastTs = false;
            TimeSig nextTs;
            try
            {
                nextTs = TimeSigs[tsIndex + 1];
            }
            catch
            {
                nextTs = currTs; // If there's no next time signature, use the current one
                lastTs = true;
            }
            return (currTs, nextTs, lastTs);
        }
        private void SetMidiInfo(string midiPath)
        {
            ReadingSettings readingSettings = new ReadingSettings
            {
                TextEncoding = Encoding.Latin1
            };
            SongMidiFile = MidiFile.Read(midiPath, readingSettings);
            var timeDivision = SongMidiFile.TimeDivision;
            if (timeDivision is TicksPerQuarterNoteTimeDivision ticksPerQuarterNoteTimeDivision)
            {
                // Get Ticks Per Quarter Note
                TPB = ticksPerQuarterNoteTimeDivision.TicksPerQuarterNote;
            }
            else
            {
                throw new NotSupportedException("MIDI file does not use ticks as a time measurement.");
            }
            if (TPB != 480 && !FromChart)
            {
                HopoThreshold = HopoThreshold * TPB / 480;
            }

        }
        private bool IsInTimeRange(long time, List<MidiData.Note> notes)
        {
            foreach (var note in notes)
            {
                if (time >= note.Time && time < note.EndTime)
                    return true;
            }
            return false;
        }
        private string GetTrackName(TrackChunk trackChunk)
        {
            foreach (var midiEvent in trackChunk.Events)
            {
                // Check if the event is a SequenceTrackNameEvent
                if (midiEvent is SequenceTrackNameEvent trackNameEvent)
                {
                    // Output the track name
                    return trackNameEvent.Text;
                }
            }
            return "None";
        }
        private long GetBeatLengthInTicks(TimeSignature currTS)
        {
            // Implement logic based on the time signature
            return TPB * 4 / currTS.Denominator;
        }

        internal double TicksToMilliseconds(long ticks)
        {
            // Convert ticks to milliseconds
            var timeSpan = TimeConverter.ConvertTo<MetricTimeSpan>(ticks, SongTempoMap);
            return timeSpan.TotalMilliseconds;
        }
        internal int GameMilliseconds(long ticks)
        {
            return (int)Math.Round(TicksToMilliseconds(ticks));
        }
        public static SongQbFile TokenizePak(string pakPath)
        {
            string endian = "big";
            string extension = Path.GetExtension(pakPath).ToLowerInvariant();
            if (extension == ".ps2")
            {
                endian = "little";
            }
            string fileName = Path.GetFileNameWithoutExtension(pakPath);
            string hasSongPattern = @"(_song|_s)?\.pak(\.xen|\.ps3|\.ps2)?$";
            string songName = Regex.Replace(fileName, hasSongPattern, "", RegexOptions.IgnoreCase).ToLower();
            string dlcPattern = @"(a|b|c)dlc";
            songName = Regex.Replace(songName, dlcPattern, "dlc", RegexOptions.IgnoreCase);
            var pakEntries = PakEntryDictFromFile(pakPath);
            byte[]? midQb = null;
            Dictionary<uint, string>? midQs = new Dictionary<uint, string>();
            byte[]? songScripts = null;
            byte[]? notes = null;
            byte[]? perf = null;
            byte[]? perfXml = null;
            foreach (var entry in pakEntries)
            {
                var entryName = entry.Key;
                var entryData = entry.Value;
                if (entryName.Contains("song_scripts.qb"))
                {
                    songScripts = entryData.EntryData;
                }
                else if (entryName.Contains(".mid.qb"))
                {
                    midQb = entryData.EntryData;
                }
                //else if (entryName.Contains(".mid.qs"))
                else if (Regex.Match(entryName, @"(\.mid\.qs$)|(\.mid\.qs\.en$)", RegexOptions.IgnoreCase).Success)
                {
                    var hexPairs = Encoding.Unicode.GetString(RemoveBom(entryData.EntryData));
                    midQs = GetQsDictFromString(hexPairs);
                }
                else if (entryName.Contains(".note"))
                {
                    notes = entryData.EntryData;
                }
                else if (entryName.Contains(".perf.xml"))
                {
                    perfXml = entryData.EntryData;
                }
                else if (entryName.Contains(".perf"))
                {
                    perf = entryData.EntryData;
                }
            }
            var songData = new SongQbFile(songName, midQb, midQs, songScripts, notes, perf, perfXml, endian);
            return songData;
        }
    }
}
