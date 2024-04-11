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
using MidiTheory = Melanchall.DryWetMidi.MusicTheory;
using MidiData = Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using static GH_Toolkit_Core.MIDI.SongQbFile;
using System.Collections.Specialized;
using System.Text;
using GH_Toolkit_Core.Checksum;
using System.Security.Policy;

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
        private const string PARTDRUMS = "PART DRUMS";
        private const string PARTGUITAR = "PART GUITAR";
        private const string PARTRHYTHM = "PART RHYTHM";
        private const string PARTGUITARCOOP = "PART GUITAR COOP";
        private const string PARTBASS = "PART BASS";
        private const string PARTAUX = "PART AUX";
        private const string PARTVOCALS = "PART VOCALS";
        private const string EVENTS = "EVENTS";
        private const string BEAT = "BEAT";
        private const string CAMERAS = "CAMERAS";
        private const string LIGHTSHOW = "LIGHTSHOW";

        private const string EMPTYSTRING = "";
        private const string SECTION_OLD = "section ";
        private const string SECTION_NEW = "prc_";
        private const string CROWD_CHECK = "crowd";
        private const string SECTION_EVENT = "section";
        private const string CROWD_EVENT = "crowd_";

        private const string SCR = "scr";
        private const string TIME = "time";

        private const string EASY = "Easy";
        private const string MEDIUM = "Medium";
        private const string HARD = "Hard";
        private const string EXPERT = "Expert";

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
        public QBArrayNode? LightshowScripts { get; set; }
        public QBArrayNode? CrowdScripts { get; set; }
        public QBArrayNode? DrumsScripts { get; set; }
        public QBArrayNode? PerformanceScripts { get; set; }
        public List<Marker>? Markers { get; set; }
        internal MidiFile SongMidiFile { get; set; }
        internal TempoMap SongTempoMap { get; set; }
        private int TPB { get; set; }
        private int HopoThreshold { get; set; }
        private long LastEventTick { get; set; }
        public static string? PerfOverride { get; set; }
        public static string? SongScriptOverride { get; set; }
        public static string? VenueSource { get; set; }
        public static bool RhythmTrack { get; set; }
        public static string? Game { get; set; }
        public static string? SongName { get; set; }
        public static string? Console { get; set; }
        public static bool OverrideBeat { get; set; }
        public static HopoType HopoMethod { get; set; }
        public Dictionary<string, string> QsList { get; set; } = new Dictionary<string, string>();
        private List<string> ErrorList { get; set; } = new List<string>();
        public SongQbFile(string midiPath, string songName, string game = GAME_GH3, string console = CONSOLE_XBOX, int hopoThreshold = 170, string perfOverride = "", string songScriptOverride = "", string venueSource = "", bool rhythmTrack = false, bool overrideBeat = false, int hopoType = 0)
        {
            Game = game;
            SongName = songName;
            Console = console;
            HopoThreshold = hopoThreshold;
            PerfOverride = perfOverride;
            SongScriptOverride = songScriptOverride;
            VenueSource = venueSource == "" ? Game : venueSource;
            RhythmTrack = rhythmTrack;
            OverrideBeat = overrideBeat;
            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                HopoMethod = 0;
            }
            else
            {
                HopoMethod = (HopoType)hopoType;
            }
            SetMidiInfo(midiPath);
        }
        public string GetGame()
        {
            return Game!;
        }
        public string GetConsole()
        {
            return Console!;
        }
        public void AddToErrorList(string error)
        {
            ErrorList.Add(error);
        }
        public string GetErrorListAsString()
        {
            return string.Join("\n", ErrorList);
        }
        public void AddTimedError(string error, string part, long ticks)
        {
            AddToErrorList($"{part}: {error} found at {TicksToMilliseconds(ticks) / 1000}");
        }
        public List<QBItem> ParseMidi()
        {
            // Getting the tempo map to convert ticks to time
            SongTempoMap = SongMidiFile.GetTempoMap();
            var trackChunks = SongMidiFile.GetTrackChunks();
            GetTimeSigs(trackChunks.First());
            CalculateFretbars();
            bool noAux = true;
            foreach (var trackChunk in trackChunks.Skip(1))
            {
                string trackName = GetTrackName(trackChunk);
                switch (trackName)
                {
                    case PARTDRUMS:
                        Drums.MakeInstrument(trackChunk, this, drums: true);
                        if (Drums.AnimNotes.Count > 0)
                        {
                            DrumsNotes = Drums.AnimNotes;
                        }
                        break;
                    case PARTBASS:
                        Rhythm.MakeInstrument(trackChunk, this);
                        break;
                    case PARTGUITAR:
                        Guitar.MakeInstrument(trackChunk, this);
                        break;
                    case PARTGUITARCOOP:
                        GuitarCoop.MakeInstrument(trackChunk, this);
                        break;
                    case PARTRHYTHM:
                        RhythmCoop.MakeInstrument(trackChunk, this);
                        break;
                    case PARTAUX:
                        noAux = false;
                        Aux.MakeInstrument(trackChunk, this);
                        break;
                    case PARTVOCALS:
                        Vocals.MakeInstrument(trackChunk, this, LastEventTick);
                        break;
                    case CAMERAS:
                        ProcessCameras(trackChunk);
                        break;
                    case LIGHTSHOW:
                        ProcessLights(trackChunk);
                        break;
                    case EVENTS:
                        ProcessEvents(trackChunk);
                        break;
                    case BEAT:
                        if (OverrideBeat)
                        {
                            ProcessBeat(trackChunk);
                        }
                        break;
                    default:
                        break;
                }
            }
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
                    if (noAux)
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
                gameQb.Add(CombinePerformanceScripts($"{SongName}_performance"));
            }
            else if (Game == GAME_GHWT)
            {
                gameQb.AddRange(MakeFretbarsAndTimeSig());
                gameQb.AddRange(Guitar.ProcessQbEntriesGHWT(SongName));
                gameQb.AddRange(Rhythm.ProcessQbEntriesGHWT(SongName));
                gameQb.AddRange(Drums.ProcessQbEntriesGHWT(SongName));
                gameQb.AddRange(Aux.ProcessQbEntriesGHWT(SongName));
                gameQb.AddRange(GuitarCoop.ProcessQbEntriesGHWT(SongName));
                gameQb.AddRange(RhythmCoop.ProcessQbEntriesGHWT(SongName));
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
                gameQb.Add(CombinePerformanceScripts($"{SongName}_performance"));
                var (voxQb, QsDict) = Vocals.AddVoxToQb(SongName);
                QsList = QsDict;
                gameQb.AddRange(voxQb);
            }
            return gameQb;
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
            int animMod;
            if (RhythmCoop.AnimNotes.Count != 0)
            {
                newAux = Rhythm.AnimNotes.Count != 0 ? Rhythm : RhythmCoop;
                animMod = 1;
            }
            else
            {
                newAux = Guitar;
                animMod = 2;
            }
            List<AnimNote> newAuxNotes = new List<AnimNote>();
            foreach (AnimNote note in newAux.AnimNotes)
            {
                newAuxNotes.Add(new AnimNote(note.Time, note.Note - (animMod * 17), note.Length, note.Velocity));
            }
            Aux.AnimNotes = newAuxNotes;
            Aux.Expert = newAux.Expert;
        }
        public byte[] ParseMidiToQb()
        {
            var gameQb = ParseMidi();
            string songMid;
            if (Console == CONSOLE_PS2)
            {
                songMid = $"data\\songs\\{SongName}.mid.qb";
            }
            else
            {
                songMid = $"songs\\{SongName}.mid.qb";
            }
            byte[] bytes = CompileQbFile(gameQb, songMid, Game, Console);
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
        public List<QBItem> ProcessMarkers()
        {
            if (Markers == null)
            {
                Markers = [new Marker(0, "start")];
            }
            QBArrayNode markerArray = new QBArrayNode();
            foreach (Marker marker in Markers)
            {
                QBStructData markerEntry = marker.ToStruct(Console);
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
            int MinCameraWor = 3;
            int MaxCameraWor = 99;

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
                case GAME_GHWOR:
                    minCamera = MinCameraWor;
                    maxCamera = MaxCameraWor;
                    break;
                default:
                    throw new NotImplementedException("Unknown game found");
            }

            var cameraNotes = trackChunk.GetNotes().Where(x => x.NoteNumber >= minCamera && x.NoteNumber <= maxCamera).ToList();
            if (Game != VenueSource)
            {
                Dictionary<int, int> cameraMap;
                try
                {
                    switch (Game)
                    {
                        case GAME_GH3:
                            cameraMap = cameraToGh3[VenueSource];
                            break;
                        case GAME_GHA:
                            cameraMap = cameraToGha[VenueSource];
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
                int startTime = GameMilliseconds(note.Time);
                int length;
                if (i == cameraNotes.Count - 1)
                {
                    length = 20000;
                }
                else
                {
                    length = GameMilliseconds(cameraNotes[i + 1].Time) - startTime;
                }
                int noteVal = note.NoteNumber;
                int velocity = note.Velocity;
                cameraAnimNotes.Add(new AnimNote(startTime, noteVal, length, velocity));
            }
            CamerasNotes = cameraAnimNotes;
        }
        public QBItem CombinePerformanceScripts(string songName)
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
            perfScripts.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            PerformanceScripts = new QBArrayNode();
            foreach (var script in perfScripts)
            {
                PerformanceScripts.AddStructToArray(script.Item2);
            }
            QBItem PerfScriptQb = new QBItem(songName, PerformanceScripts);
            return PerfScriptQb;
        }
        // Method to generate the scripts for the instrument
        public List<(int, QBStructData)> InstrumentScripts(List<TimedEvent> events, string actor)
        {
            bool isOldGame = Game == GAME_GH3 || Game == GAME_GHA;
            bool isGtrOrSinger = actor == GUITARIST || actor == VOCALIST;
            bool isGtrAndPs2 = actor == GUITARIST && Console == CONSOLE_PS2;
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
            var lightNotes = trackChunk.GetNotes().ToList();
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
            animParams.MakeWalkToNode(actor, nodeType, Console!);
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
                            string markerName = inDict ? sectionName! : FormatString(eventData);
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
                            break;
                    }
                }
            }
            if (Markers.Count == 0)
            {
                Markers = [new Marker(0, "start")];
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
        public static string FormatString(string inputString)
        {
            string formattedString = inputString.Replace("_", " ");
            formattedString = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedString.ToLower());
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
        public class Instrument
        {
            public Difficulty Easy { get; set; } = new Difficulty(EASY);
            public Difficulty Medium { get; set; } = new Difficulty(MEDIUM);
            public Difficulty Hard { get; set; } = new Difficulty(HARD);
            public Difficulty Expert { get; set; } = new Difficulty(EXPERT);
            public List<StarPower>? FaceOffStar { get; set; } = null; // In-game. Based off of the easy chart.
            public QBArrayNode FaceOffP1 { get; set; }
            public QBArrayNode FaceOffP2 { get; set; }
            public QBArrayNode DrumFill { get; set; }
            public List<AnimNote> AnimNotes { get; set; } = new List<AnimNote>();
            public List<(int, QBStructData)> PerformanceScript { get; set; } = new List<(int, QBStructData)>();
            internal List<MidiData.Note> StarPowerPhrases { get; set; }
            internal List<MidiData.Note> BattleStarPhrases { get; set; }
            internal List<MidiData.Note> FaceOffStarPhrases { get; set; }
            internal string TrackName { get; set; }
            public Instrument(string trackName) // Default empty instrument
            {
                TrackName = trackName;
            }
            public void MakeInstrument(TrackChunk trackChunk, SongQbFile songQb, bool drums = false)
            {
                if (trackChunk == null || songQb == null)
                {
                    throw new ArgumentNullException("trackChunk or songQb is null");
                }

                int openNotes = Game == GAME_GH3 || Game == GAME_GHA ? 0 : 1;
                int drumsMode = !drums ? 0 : 1;

                Dictionary<MidiTheory.NoteName, int> noteDict;
                Dictionary<int, int> animDict = new Dictionary<int, int>();
                Dictionary<int, int> drumAnimDict = new Dictionary<int, int>();

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
                    noteDict = Gh4Notes;
                    try
                    {
                        animDict = leftHandMappingsWt[TrackName];
                    }
                    catch (KeyNotFoundException)
                    {
                        animDict = leftHandMappingsWt[""];
                    }

                }

                // Extract all notes from the track once
                var allNotes = trackChunk.GetNotes().ToList();

                // Extract Face-Off Notes
                var faceOffP1Notes = allNotes.Where(x => x.NoteNumber == FaceOffP1Note).ToList();
                FaceOffP1 = ProcessOtherSections(faceOffP1Notes, songQb);
                var faceOffP2Notes = allNotes.Where(x => x.NoteNumber == FaceOffP2Note).ToList();
                FaceOffP2 = ProcessOtherSections(faceOffP2Notes, songQb);

                
                // TapNotes = ProcessOtherSections(tapNotes, songQb, isTapNote:true);

                // Create performance scripts for the instrument
                var timedEvents = trackChunk.GetTimedEvents().ToList();
                var textEvents = timedEvents.Where(e => e.Event is TextEvent).ToList();

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
                    Easy.ProcessDifficultyGuitar(allNotes, EasyNoteMin, EasyNoteMax, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases, FaceOffStarPhrases);
                    Medium.ProcessDifficultyGuitar(allNotes, MediumNoteMin, MediumNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                    Hard.ProcessDifficultyGuitar(allNotes, HardNoteMin, HardNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                    Expert.ProcessDifficultyGuitar(allNotes, ExpertNoteMin, ExpertNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                }
                else
                {
                    var drumFillNotes = allNotes.Where(x => x.NoteNumber == TapNote).ToList();
                    AnimNotes = InstrumentAnims(allNotes, DrumAnimStart, DrumAnimEnd, drumAnimDict, songQb, true);
                    DrumFill = ProcessDrumFills(drumFillNotes, songQb);
                    // Process notes for each difficulty level
                    Easy.ProcessDifficultyDrums(allNotes, EasyNoteMin, EasyNoteMax + 1, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases, FaceOffStarPhrases);
                    Medium.ProcessDifficultyDrums(allNotes, MediumNoteMin, MediumNoteMax + 1, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases);
                    Hard.ProcessDifficultyDrums(allNotes, HardNoteMin, HardNoteMax + 1, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases);
                    Expert.ProcessDifficultyDrums(allNotes, ExpertNoteMin, ExpertNoteMax + 1, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);

                }
                FaceOffStar = Easy.FaceOffStar;
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
                    if (allowMultiTime && note.NoteNumber >= 22) // Basicailly all notes below 22 do not need to be processed for practice mode
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
            public List<QBItem> ProcessQbEntriesGH3(string name, bool blankBM = true)
            {
                var list = new List<QBItem>();
                string playName = $"{name}_song{TrackName}";
                string starName = $"{name}{TrackName}";
                list.Add(Easy.CreateGH3Notes(playName));
                list.Add(Medium.CreateGH3Notes(playName));
                list.Add(Hard.CreateGH3Notes(playName));
                list.Add(Expert.CreateGH3Notes(playName));
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
            public List<QBItem> ProcessQbEntriesGHWT(string name)
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
                list.AddRange(MakeFaceOffQb(SongName));
                
                return list;
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
            private QBArrayNode ProcessDrumFills(List<MidiData.Note> entryNotes, SongQbFile songQb)
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
            public List<QBArrayNode>? Markers { get; set; } = new List<QBArrayNode>();
            private string Name { get; set; }
            // GHWT stuff to come later
            public VocalsInstrument()
            {

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
                        if (note.Note == 2)
                        {
                            PlayNote prevNote = Notes[i - 1];
                            PlayNote nextNote = Notes[i + 1];
                            note.Time = prevNote.Time + prevNote.Length;
                            note.Length = nextNote.Time - note.Time;
                        }
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
                var timedEvents = trackChunk.GetTimedEvents().ToList();
                var textEvents = timedEvents.Where(e => e.Event is TextEvent || e.Event is LyricEvent).ToList();
                PerformanceScript = songQb.InstrumentScripts(textEvents, VOCALIST);
                if (Game != GAME_GH3 && Game != GAME_GHA)
                {
                    var allNotes = trackChunk.GetNotes().ToList();
                    var singNotes = new Dictionary<long, MidiData.Note>();
                    var phraseNotes = new Dictionary<long, VocalPhrase>();
                    var freeformNotes = new Dictionary<long, int>();
                    var lyrics = new Dictionary<long, string>();
                    foreach (var textData in textEvents)
                    {
                        string eventText = textData.Event switch
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
                        if (note.NoteNumber >= VocalMin && note.NoteNumber <= VocalMax)
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
                                    lyric = TrimEndIfMatched(lyric, RANGE_SHIFT_LYRIC, UNKNOWN_LYRIC, TALKIE_LYRIC, TALKIE_LYRIC2);

                                    // After trimming, check if the specific cases for setting the note number need to be handled
                                    if (lyric.EndsWith(TALKIE_LYRIC) || lyric.EndsWith(TALKIE_LYRIC2))
                                    {
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
                                            var prevTime = lyricTimeList.IndexOf(note.Time) - 1;
                                            var prevNote = singNotes[lyricTimeList[prevTime]];
                                            var newNote = new MidiData.Note((SevenBitNumber)2, note.Time - prevNote.EndTime, prevNote.EndTime);
                                            singNotes.Add(newNote.Time, newNote);
                                            slideTimeList.Add(note.Time);
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
                                    singNotes.Add(note.Time, note);
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
                                int points = note.Channel == 1 ? 0 : noteLength/6;
                                FreeformPhrases.Add(new Freeform(noteTime, noteLength, points));
                            }
                            else
                            {
                                songQb.AddTimedError("Duplicate freeform note found", "PART VOCALS", note.Time);
                            }
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
                    PlayNote sepKick = new PlayNote(startTime, kickNote.NoteBit, kickLength);
                    noteList.Add(sepKick);
                }
            }

            return noteList;
        }
        public byte[] MakePs2SkaScript(string gender = "Male")
        {
            string qbName = $"data\\songs\\{SongName}_song_scripts.qb";
            string skaScript = $"""
                script {SongName}_song_startup
                    animload_Singer_{gender}_{SongName} <...>
                endscript
                """;
            var skaScriptList = ParseQFile(skaScript);
            var scriptCompiled = CompileQbFile(skaScriptList, qbName, game:GAME_GH3, console:CONSOLE_PS2);
            return scriptCompiled;
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
            public List<PlayNote>? PlayNotes { get; set; }
            public List<StarPower>? StarEntries { get; set; }
            public List<StarPower>? BattleStarEntries { get; set; }
            public List<StarPower>? FaceOffStar { get; set; }
            public List<StarPower>? TapNotes { get; set; }
            public Difficulty(string name)
            {
                diffName = name;
            }
            public void ProcessDifficultyGuitar(List<MidiData.Note> allNotes, int minNote, int maxNote, Dictionary<MidiTheory.NoteName, int> noteDict, int openNotes, SongQbFile songQb, List<MidiData.Note> StarPowerPhrases, List<MidiData.Note> BattleStarPhrases, List<MidiData.Note> FaceOffStarPhrases = null)
            {
                var notes = allNotes.Where(n => n.NoteNumber >= (minNote - openNotes) && n.NoteNumber <= maxNote).ToList();
                //var chords = notes.GetChords().ToList();
                var chords = GroupNotes(notes, DefaultTickThreshold * songQb.TPB / DefaultTPB).ToList();
                var onNotes = allNotes.Where(n => n.NoteNumber == maxNote + 1).ToList();
                var offNotes = allNotes.Where(n => n.NoteNumber == maxNote + 2).ToList();
                
                PlayNotes = songQb.MakeGuitar(chords, onNotes, offNotes, noteDict);
                // Process tap notes
                TapNotes = ProcessTapNotes(allNotes, chords, songQb);
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
                bool filterChords = songQb.GetConsole() != CONSOLE_PC && songQb.GetGame() == GAME_GHWT;
                // Extract Tap Notes
                var tapNotes = allNotes.Where(x => x.NoteNumber == TapNote).ToList();
                foreach (var note in tapNotes)
                {
                    var tapUnder = chords.Where(x => x.Time >= note.Time && x.Time < note.EndTime).ToList();
                    var currTime = note.Time;
                    int startTime;
                    int endTime;
                    int length;
                    int noteCount = 0;
                    if (filterChords)
                    {
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
                var notes = allNotes.Where(n => n.NoteNumber >= (minNote - openNotes) && n.NoteNumber <= maxNote);

                //var chords = notes.GetChords().ToList();
                var chords = GroupNotes(notes, DefaultTickThreshold * DefaultTPB / songQb.TPB).ToList();

                PlayNotes = songQb.MakeDrums(chords, noteDict);
                StarEntries = CalculateStarPowerNotes(chords, StarPowerPhrases, songQb);
                BattleStarEntries = CalculateStarPowerNotes(chords, BattleStarPhrases, songQb);
                if (FaceOffStarPhrases != null && diffName == EASY)
                {
                    // Should only activate on the easy difficulty and nowhere else
                    FaceOffStar = CalculateStarPowerNotes(chords, FaceOffStarPhrases, songQb);
                }
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
            public QBItem CreateGH3Notes(string songName) // Combine this with SP
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
                    if ((!playNote.IsHopo && playNote.ForcedOn) || (playNote.IsHopo && playNote.ForcedOff))
                    {
                        playNote.Note += GH3FORCE;
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

                    int noteData = playNote.Accents;  // Starts as the 8 MSBs.
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
        }
        [DebuggerDisplay("{Time} - {Text}")]
        public class Marker
        {
            public int Time { get; set; }
            public string Text { get; set; }
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
                marker.AddVarToStruct("Marker", Text, markerType);
                return marker;
            }
        }
        [DebuggerDisplay("{(float)Time/1000, nq}: {Length} ms long ({NoteCount} Notes)")]
        public class StarPower
        {
            public int Time { get; set; }
            public int Length { get; set; }
            public int NoteCount { get; set; }
            public StarPower(int time, int length, int noteCount)
            {
                Time = time;
                Length = length;
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
                        TimeSigs.Add(new TimeSig((int)Math.Round(timeInMilliseconds), timeSignatureEvent.Numerator, timeSignatureEvent.Denominator));
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Conductor track not found.");
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

        }
        private void SetMidiInfo(string midiPath)
        {
            SongMidiFile = MidiFile.Read(midiPath);
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
            if (TPB != 480)
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
    }
}
