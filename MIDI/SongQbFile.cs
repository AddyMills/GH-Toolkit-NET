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
        public static string? PerfOverride { get; set; }
        public static string? Game { get; set; }
        public static string? SongName { get; set; }
        public static string? Console { get; set; }
        public SongQbFile(string midiPath, string songName, string game = GAME_GH3, string console = CONSOLE_XBOX, int hopoThreshold = 170, string perfOverride = "")
        {
            Game = game;
            SongName = songName;
            Console = console;
            HopoThreshold = hopoThreshold;
            PerfOverride = perfOverride;
            SetMidiInfo(midiPath);
        }
        public List<QBItem> ParseMidi()
        {
            // Getting the tempo map to convert ticks to time
            SongTempoMap = SongMidiFile.GetTempoMap();
            var trackChunks = SongMidiFile.GetTrackChunks();
            GetTimeSigs(trackChunks.First());
            CalculateFretbars();
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
                        Aux.MakeInstrument(trackChunk, this);
                        break;
                    case PARTVOCALS:
                        Vocals.MakeInstrument(trackChunk, this);
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
                gameQb.AddRange(Rhythm.ProcessQbEntriesGH3(SongName));
                gameQb.AddRange(GuitarCoop.ProcessQbEntriesGH3(SongName));
                gameQb.AddRange(RhythmCoop.ProcessQbEntriesGH3(SongName));
                if (Game == GAME_GHA)
                {
                    gameQb.AddRange(Aux.ProcessQbEntriesGH3(SongName));
                }
                gameQb.AddRange(Guitar.MakeFaceOffQb(SongName));
                gameQb.AddRange(MakeBossBattleQb());
                gameQb.AddRange(MakeFretbarsAndTimeSig());
                gameQb.Add(ProcessMarkers());
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
                gameQb.Add(CombinePerformanceScripts_GH3($"{SongName}_performance"));
            }
            return gameQb;
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
                tsEntry.AddToArray(ts.Time);
                tsEntry.AddToArray(ts.Numerator);
                tsEntry.AddToArray(ts.Denominator);
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
            if (RhythmCoop.AnimNotes.Count != 0)
            {
                AnimNotes.AddRange(RhythmCoop.AnimNotes);
            }
            else
            {
                AnimNotes.AddRange(Rhythm.AnimNotes);
            }
            if (Game == GAME_GHA)
            {
                AnimNotes.AddRange(Aux.AnimNotes);
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
                throw new NotImplementedException("Coming soon");
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
        public QBItem ProcessMarkers()
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
            if (Game == GAME_GH3 || Game == GAME_GHA)
            {
                markerName = $"{SongName}_markers";
            }
            else
            {
                markerName = $"{SongName}_guitar_markers";
            }
            QBItem qbItem = new QBItem(markerName, markerArray);

            return qbItem;
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
            switch (Game)
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
            List<AnimNote> cameraAnimNotes = new List<AnimNote>();
            for (int i = 0; i < cameraNotes.Count; i++)
            {
                Note note = cameraNotes[i];
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
        public QBItem CombinePerformanceScripts_GH3(string songName)
        {
            var perfScripts = new List<(int, QBStructData)>();
            //QBItem perfScripts = new QBItem(songName, Guitar.PerformanceScript);
            perfScripts.AddRange(Guitar.PerformanceScript);
            perfScripts.AddRange(Rhythm.PerformanceScript);
            if (Game == GAME_GHA)
            {
                perfScripts.AddRange(Aux.PerformanceScript);
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

            foreach (Note note in lightNotes)
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
        private QBArrayNode Gh3LightsNote107(List<Note> notes)
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
            List<Chord> chords = GroupNotes(notes, blendReduction);
            foreach (Chord lightGroup in chords)
            {
                long blendNoteTime = lightGroup.Time - blendReduction;
                if (blendNoteTime < 0)
                {
                    blendNoteTime = 0;
                }
                long maxLen = 0;
                bool isBlended = false;
                foreach (Note note in lightGroup.Notes)
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
                    notes.Add(new Note((SevenBitNumber)blendLookup[0], defaultTick, blendNoteTime));
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
                            notes.Add(new Note((SevenBitNumber)blendNote, defaultTick, blendNoteTime));
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
            // Define constants for note ranges
            private const int EasyNoteMin = 60;
            private const int EasyNoteMax = 64;
            private const int MediumNoteMin = 72;
            private const int MediumNoteMax = 76;
            private const int HardNoteMin = 84;
            private const int HardNoteMax = 88;
            private const int ExpertNoteMin = 96;
            private const int ExpertNoteMax = 100;
            private const int SoloNote = 103;
            private const int TapNote = 104;
            private const int FaceOffP1Note = 105;
            private const int FaceOffP2Note = 106;
            private const int FaceOffStarNote = 107;
            private const int BattleStarNote = 115;
            private const int StarPowerNote = 116;

            private const int GuitarAnimStart = 40;
            private const int GuitarAnimEnd = 59;

            private const int DrumAnimStart = 20;
            private const int DrumHiHatOpen = 25;
            private const int DrumHiHatRight = 31;
            private const int DrumHiHatLeft = 30;
            private const int DrumAnimEnd = 51;

            public Difficulty Easy { get; set; } = new Difficulty(EASY);
            public Difficulty Medium { get; set; } = new Difficulty(MEDIUM);
            public Difficulty Hard { get; set; } = new Difficulty(HARD);
            public Difficulty Expert { get; set; } = new Difficulty(EXPERT);
            public List<StarPower>? FaceOffStar { get; set; } = null; // In-game. Based off of the easy chart.
            public QBArrayNode FaceOffP1 { get; set; }
            public QBArrayNode FaceOffP2 { get; set; }
            public List<AnimNote> AnimNotes { get; set; } = new List<AnimNote>();
            public List<(int, QBStructData)> PerformanceScript { get; set; } = new List<(int, QBStructData)>();
            internal List<Note> StarPowerPhrases { get; set; }
            internal List<Note> BattleStarPhrases { get; set; }
            internal List<Note> FaceOffStarPhrases { get; set; }
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

                int openNotes = Game == GAME_GH3 ? 0 : 1;
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
                FaceOffP1 = ProcessFaceOffSections(faceOffP1Notes, songQb);
                var faceOffP2Notes = allNotes.Where(x => x.NoteNumber == FaceOffP2Note).ToList();
                FaceOffP2 = ProcessFaceOffSections(faceOffP2Notes, songQb);

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
                    Easy.ProcessDifficultyGuitar(allNotes, EasyNoteMin, EasyNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases, FaceOffStarPhrases);
                    Medium.ProcessDifficultyGuitar(allNotes, MediumNoteMin, MediumNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                    Hard.ProcessDifficultyGuitar(allNotes, HardNoteMin, HardNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                    Expert.ProcessDifficultyGuitar(allNotes, ExpertNoteMin, ExpertNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);
                }
                else
                {
                    AnimNotes = InstrumentAnims(allNotes, DrumAnimStart, DrumAnimEnd, drumAnimDict, songQb, true);
                    Easy.ProcessDifficultyDrums(allNotes, EasyNoteMin, EasyNoteMax, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases, FaceOffStarPhrases);
                    Medium.ProcessDifficultyDrums(allNotes, MediumNoteMin, MediumNoteMax, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases);
                    Hard.ProcessDifficultyDrums(allNotes, HardNoteMin, HardNoteMax, noteDict, 0, songQb, StarPowerPhrases, BattleStarPhrases);
                    Expert.ProcessDifficultyDrums(allNotes, ExpertNoteMin, ExpertNoteMax, noteDict, openNotes, songQb, StarPowerPhrases, BattleStarPhrases);

                }
                FaceOffStar = Easy.FaceOffStar;
            }
            private List<AnimNote> InstrumentAnims(List<Note> allNotes, int minNote, int maxNote, Dictionary<int, int> animDict, SongQbFile songQb, bool allowMultiTime = false)
            {
                int AnimNoteMin = 22;
                List<AnimNote> animNotes = new List<AnimNote>();
                var notes = allNotes.Where(n => n.NoteNumber >= minNote && n.NoteNumber <= maxNote && n.NoteNumber != DrumHiHatOpen).ToList();
                var hihatNotes = allNotes.Where(n => n.NoteNumber == DrumHiHatOpen).ToList();
                var prevTime = 0;
                var prevNote = 0;
                foreach (Note note in notes)
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
                list.Add(Easy.CreateGH3StarPower(starName));
                list.Add(Medium.CreateGH3StarPower(starName));
                list.Add(Hard.CreateGH3StarPower(starName));
                list.Add(Expert.CreateGH3StarPower(starName));
                list.Add(Easy.CreateGH3BattleStar(starName, blankBM));
                list.Add(Medium.CreateGH3BattleStar(starName, blankBM));
                list.Add(Hard.CreateGH3BattleStar(starName, blankBM));
                list.Add(Expert.CreateGH3BattleStar(starName, blankBM));

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
            private QBArrayNode ProcessFaceOffSections(List<Note> faceOffNotes, SongQbFile songQb)
            {
                QBArrayNode faceOffs = new QBArrayNode();
                if (faceOffNotes.Count == 0)
                {
                    faceOffs.MakeEmpty();
                }
                else
                {
                    foreach (Note foNote in faceOffNotes)
                    {
                        QBArrayNode faceOffEntry = new QBArrayNode();
                        int startTime = (int)Math.Round(songQb.TicksToMilliseconds(foNote.Time));
                        int endTime = (int)Math.Round(songQb.TicksToMilliseconds(foNote.EndTime));
                        int length = endTime - startTime;
                        faceOffEntry.AddToArray(startTime);
                        faceOffEntry.AddToArray(length);
                        faceOffs.AddArrayToArray(faceOffEntry);
                    }
                }

                return faceOffs;
            }
        }
        public class VocalsInstrument
        {
            public List<(int, QBStructData)> PerformanceScript { get; set; } = new List<(int, QBStructData)>();
            // GHWT stuff to come later
            public VocalsInstrument()
            {

            }
            public void MakeInstrument(TrackChunk trackChunk, SongQbFile songQb)
            {
                if (trackChunk == null || songQb == null)
                {
                    throw new ArgumentNullException("trackChunk or songQb is null");
                }
                var timedEvents = trackChunk.GetTimedEvents().ToList();
                var textEvents = timedEvents.Where(e => e.Event is TextEvent).ToList();
                PerformanceScript = songQb.InstrumentScripts(textEvents, VOCALIST);
            }
        }
        public List<PlayNote> MakeGuitar(List<Chord> chords, List<Note> forceOn, List<Note> forceOff, Dictionary<MidiTheory.NoteName, int> noteDict)
        {
            List<PlayNote> noteList = new List<PlayNote>();
            long prevTime = 0;
            int prevNote = 0;
            for (int i = 0; i < chords.Count; i++)
            {
                Chord notes = chords[i];
                long currTime = notes.Time;
                int noteVal = 0;
                long endTimeTicks = notes.EndTime;
                foreach (Note note in notes.Notes)
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
                if (prevTime != 0 && BitOperations.IsPow2(noteVal) && prevNote != noteVal) // If it's not the first note in the chart and a single note
                {
                    currNote.IsHopo = (currTime - prevTime) < HopoThreshold;
                }
                currNote.ForcedOn = IsInTimeRange(currTime, forceOn);
                currNote.ForcedOff = IsInTimeRange(currTime, forceOff);
                prevTime = currTime;
                prevNote = noteVal;
                noteList.Add(currNote);
            }

            return noteList;
        }
        public List<PlayNote> MakeDrums(List<Chord> chords, Dictionary<MidiTheory.NoteName, int> noteDict)
        {
            List<PlayNote> noteList = new List<PlayNote>();
            long prevTime = 0;
            for (int i = 0; i < chords.Count; i++)
            {
                Chord notes = chords[i];
                KickNote? kickNote = null;
                // If the kick note and hand-notes are different lengths, this is made to make a potential second entry

                long currTime = notes.Time;
                int noteVal = 0;
                int accentVal = AllAccents;
                int ghostVal = 0;
                byte numAccents = 0;
                long endTimeTicks = notes.EndTime;
                foreach (Note note in notes.Notes)
                {
                    int noteBit = noteDict[note.NoteName];
                    if (note.NoteName != MidiTheory.NoteName.C && note.NoteName != MidiTheory.NoteName.B)
                    {
                        noteVal += noteBit;
                        if (note.EndTime < endTimeTicks)
                        {
                            endTimeTicks = note.EndTime;
                        }
                        if (note.Velocity != AccentVelocity)
                        {
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
            public Difficulty(string name)
            {
                diffName = name;
            }
            public void ProcessDifficultyGuitar(List<Note> allNotes, int minNote, int maxNote, Dictionary<MidiTheory.NoteName, int> noteDict, int openNotes, SongQbFile songQb, List<Note> StarPowerPhrases, List<Note> BattleStarPhrases, List<Note> FaceOffStarPhrases = null)
            {
                var notes = allNotes.Where(n => n.NoteNumber >= (minNote - openNotes) && n.NoteNumber <= maxNote).ToList();
                //var chords = notes.GetChords().ToList();
                var chords = GroupNotes(notes, DefaultTickThreshold * songQb.TPB / DefaultTPB).ToList();
                var onNotes = allNotes.Where(n => n.NoteNumber == maxNote + 1).ToList();
                var offNotes = allNotes.Where(n => n.NoteNumber == maxNote + 2).ToList();

                PlayNotes = songQb.MakeGuitar(chords, onNotes, offNotes, noteDict);
                StarEntries = CalculateStarPowerNotes(chords, StarPowerPhrases, songQb);
                BattleStarEntries = CalculateStarPowerNotes(chords, BattleStarPhrases, songQb);
                if (FaceOffStarPhrases != null && diffName == EASY)
                {
                    // Should only activate on the easy difficulty and nowhere else
                    FaceOffStar = CalculateStarPowerNotes(chords, FaceOffStarPhrases, songQb);
                }

            }
            public void ProcessDifficultyDrums(List<Note> allNotes, int minNote, int maxNote, Dictionary<MidiTheory.NoteName, int> noteDict, int openNotes, SongQbFile songQb, List<Note> StarPowerPhrases, List<Note> BattleStarPhrases, List<Note> FaceOffStarPhrases = null)
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
            private List<StarPower> CalculateStarPowerNotes(List<Chord> playNotes, List<Note> starNotes, SongQbFile songQb)
            {
                List<StarPower> stars = new List<StarPower>();
                foreach (Note SpPhrase in starNotes)
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
            public QBItem CreateStarBase(string songName, string starType)
            {
                string fullName = $"{songName}_{diffName}_{starType}";
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
                    notes.AddToArray(playNote.Time);
                    notes.AddToArray(playNote.Length);
                    if ((!playNote.IsHopo && playNote.ForcedOn) || (playNote.IsHopo && playNote.ForcedOff))
                    {
                        playNote.Note += GH3FORCE;
                    }
                    notes.AddToArray(playNote.Note);
                }
                currItem.SetData(notes);
                return currItem;
            }
            public QBItem CreateGH3StarPower(string songName)
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
            public QBItem CreateGH3BattleStar(string songName, bool makeBlank)
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
            private QBArrayNode MakeStarArray(List<StarPower> starList)
            {
                QBArrayNode starData = new QBArrayNode();
                foreach (StarPower star in starList)
                {
                    QBArrayNode starEntry = new QBArrayNode();
                    starEntry.AddToArray(star.Time);
                    starEntry.AddToArray(star.Length);
                    starEntry.AddToArray(star.NoteCount);
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
                string markerType = console == CONSOLE_XBOX ? WIDESTRING : STRING;
                QBStructData marker = new QBStructData();
                marker.AddToStruct("Time", Time);
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
        [DebuggerDisplay("{(float)Time/1000, nq}: {NoteColor, nq} - {Length} ms long (Hopo: {IsHopo}, Force On: {ForcedOn}, Force Off: {ForcedOff})")]
        public class PlayNote
        {
            public int Time { get; set; }
            public int Note { get; set; }
            public int Length { get; set; }
            public int Accents { get; set; }
            public int Ghosts { get; set; }
            public bool ForcedOn { get; set; }
            public bool ForcedOff { get; set; }
            public bool IsHopo { get; set; } // A natural Hopo from being within a certain distance of another note


            // Property to get color name
            public string NoteColor
            {
                get
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
            public PlayNote(int time, int note, int length)
            {
                Time = time;
                Note = note;
                Length = length;
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
                Note = note;
                Length = length;
                Velocity = velocity;
            }
            public QBArrayNode ToGH3Anim()
            {
                QBArrayNode animNote = new QBArrayNode();
                animNote.AddToArray(Time);
                animNote.AddToArray(Note);
                animNote.AddToArray(Length);
                return animNote;
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
                light.AddToStruct("Time", Time);
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
        private bool IsInTimeRange(long time, List<Note> notes)
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
