using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QBStruct;
using static GH_Toolkit_Core.MIDI.MidiDefs;


using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Drawing.Printing;
using MidiTheory = Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Multimedia;
using System.Numerics;
using System.Diagnostics;
using System.Drawing;

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


        public List<int> Fretbars = new List<int>();
        public List<TimeSig> TimeSigs = new List<TimeSig>();
        public Instrument Guitar {  get; set; }
        public Instrument Rhythm { get; set; }
        public Instrument Drums { get; set; }
        public Instrument Aux {  get; set; }
        public Instrument GuitarCoop { get; set; }
        public Instrument RhythmCoop { get; set; }
        public List<AnimNote> ScriptNotes { get; set; }
        public List<AnimNote> AnimNotes { get; set; }
        public List<AnimNote> TriggersNotes { get; set; }
        public List<AnimNote> CamerasNotes { get; set; }
        public List<AnimNote> LightshowNotes { get; set; }
        public List<AnimNote> CrowdNotes { get; set; }
        public List<AnimNote> DrumsNotes { get; set; }
        public List<AnimNote> PerformanceNotes { get; set; }
        public List<Marker> Markers { get; set; }
        internal MidiFile SongMidiFile { get; set; }
        internal TempoMap SongTempoMap { get; set; }
        private int TPB {  get; set; }
        private int HopoThreshold { get; set; }
        public static string Game {  get; set; }
        public SongQbFile(string midiPath, string game = "GH3", int hopoThreshold = 170) 
        {
            Game = game;
            HopoThreshold = hopoThreshold;
            SetMidiInfo(midiPath);
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
                        Drums = new Instrument(trackChunk, this, drums: true);
                        break;
                    case PARTBASS:
                        Rhythm = new Instrument(trackChunk, this);
                        break;
                    case PARTGUITAR:
                        Guitar = new Instrument(trackChunk, this);
                        break;
                    case PARTGUITARCOOP:
                        GuitarCoop = new Instrument(trackChunk, this);
                        break;
                    case PARTRHYTHM:
                        RhythmCoop = new Instrument(trackChunk, this);
                        break;
                    case PARTAUX:
                        Aux = new Instrument(trackChunk, this);
                        break;
                    case CAMERAS:
                    case LIGHTSHOW:
                        break;
                    default:
                        break;
                }
            }
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
            public Difficulty Easy { get; set; }
            public Difficulty Medium { get; set; }
            public Difficulty Hard { get; set; }
            public Difficulty Expert { get; set; }
            public List<StarPower>? FaceOffStar { get; set; } = null; // In-game. Based off of the easy chart.
            public List<FaceOffSection> FaceOffP1 { get; set; }
            public List<FaceOffSection> FaceOffP2 { get; set; }
            internal List<Note> StarPowerPhrases { get; set; }
            internal List<Note> BattleStarPhrases { get; set; }
            internal List<Note> FaceOffStarPhrases { get; set; }

            public Instrument(TrackChunk trackChunk, SongQbFile songQb, bool drums = false)
            {
                if (trackChunk == null || songQb == null)
                {
                    throw new ArgumentNullException("trackChunk or songQb is null");
                }

                int openNotes = Game == "GH3" ? 0 : 1;
                int drumsMode = !drums ? 0 : 1;

                Dictionary<MidiTheory.NoteName, int> noteDict;

                if (Game == "GH3")
                {
                    noteDict = Gh3Notes;
                }
                else if (drums)
                {
                    noteDict = Gh4Drums;
                }
                else
                {
                    noteDict = Gh4Notes;
                }

                // Extract all notes from the track once
                var allNotes = trackChunk.GetNotes().ToList();

                // Extract Face-Off Notes
                var faceOffP1Notes = allNotes.Where(x => x.NoteNumber == FaceOffP1Note).ToList();
                FaceOffP1 = ProcessFaceOffSections(faceOffP1Notes, songQb);
                var faceOffP2Notes = allNotes.Where(x => x.NoteNumber == FaceOffP2Note).ToList();
                FaceOffP2 = ProcessFaceOffSections(faceOffP2Notes, songQb);

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
                    // Process notes for each difficulty level
                    Easy = ProcessDifficultyGuitar(allNotes, EasyNoteMin, EasyNoteMax, noteDict, openNotes, songQb);
                    Medium = ProcessDifficultyGuitar(allNotes, MediumNoteMin, MediumNoteMax, noteDict, openNotes, songQb);
                    Hard = ProcessDifficultyGuitar(allNotes, HardNoteMin, HardNoteMax, noteDict, openNotes, songQb);
                    Expert = ProcessDifficultyGuitar(allNotes, ExpertNoteMin, ExpertNoteMax, noteDict, openNotes, songQb);
                }
                else
                {

                }

            }
            private Difficulty ProcessDifficultyGuitar(List<Note> allNotes, int minNote, int maxNote, Dictionary<MidiTheory.NoteName, int> noteDict, int openNotes, SongQbFile songQb)
            {
                var notes = allNotes.Where(n => n.NoteNumber >= (minNote - openNotes) && n.NoteNumber <= maxNote).ToList();
                var chords = notes.GetChords().ToList();
                var onNotes = allNotes.Where(n => n.NoteNumber == maxNote + 1).ToList();
                var offNotes = allNotes.Where(n => n.NoteNumber == maxNote + 2).ToList();

                var difficulty = new Difficulty(songQb.MakeGuitar(chords, onNotes, offNotes, noteDict));
                difficulty.StarEntries = CalculateStarPowerNotes(chords, StarPowerPhrases, songQb);
                difficulty.BattleStarEntries = CalculateStarPowerNotes(chords, BattleStarPhrases, songQb);
                if (FaceOffStar == null)
                {
                    // Should only activate on the easy difficulty and nowhere else
                    FaceOffStar = CalculateStarPowerNotes(chords, FaceOffStarPhrases, songQb);
                }
                

                return difficulty;
            }
            private List<StarPower> CalculateStarPowerNotes(List<Chord> playNotes, List<Note> starNotes, SongQbFile songQb)
            {
                List<StarPower> stars = new List<StarPower>();
                foreach (Note SpPhrase in StarPowerPhrases)
                {
                    var noteCount = playNotes.Where(x => x.Time >= SpPhrase.Time && x.Time < SpPhrase.EndTime).Count();
                    int startTime = (int)Math.Round(songQb.TicksToMilliseconds(SpPhrase.Time));
                    int endTime = (int)Math.Round(songQb.TicksToMilliseconds(SpPhrase.EndTime));
                    int length = endTime - startTime;
                    stars.Add(new StarPower(startTime, length, noteCount));
                }
                return stars;
            }
            private List<FaceOffSection> ProcessFaceOffSections(List<Note> faceOffNotes, SongQbFile songQb)
            {
                List<FaceOffSection> faceOffs = new List<FaceOffSection>();
                foreach (Note foNote in faceOffNotes)
                {
                    int startTime = (int)Math.Round(songQb.TicksToMilliseconds(foNote.Time));
                    int endTime = (int)Math.Round(songQb.TicksToMilliseconds(foNote.EndTime));
                    int length = endTime - startTime;
                    faceOffs.Add(new FaceOffSection(startTime, length));
                }
                return faceOffs;
            }
        }
        public List<PlayNote> MakeGuitar(List<Chord> chords, List<Note> forceOn, List<Note> forceOff, Dictionary<MidiTheory.NoteName, int> noteDict)
        {
            List<PlayNote> noteList = new List<PlayNote>();
            long prevTime = 0;
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
                PlayNote currNote = new PlayNote(startTime, noteVal, length);
                if (prevTime != 0 && BitOperations.IsPow2(noteVal)) // If it's not the first note in the chart and a single note
                {
                    currNote.IsHopo = (currTime - prevTime) < HopoThreshold;
                }
                currNote.ForcedOn = IsInTimeRange(currTime, forceOn);
                currNote.ForcedOff = IsInTimeRange(currTime, forceOff);
                prevTime = currTime;
                noteList.Add(currNote);
                //PlayNote currNote = new PlayNote();
            }

            return noteList;
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
            public List<PlayNote> PlayNotes { get; set; }
            public List<StarPower> StarEntries { get; set; }
            public List<StarPower> BattleStarEntries { get; set; }
            public Difficulty(List<PlayNote> playNotes)
            {
                PlayNotes = playNotes;
            }
        }
        public class Marker
        {
            public int Time { get; set; }
            public string Text { get; set; }
            public Marker(int time, string text) 
            {
                Time = time;
                Text = text;
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
            public bool ForcedOn { get; set; }
            public bool ForcedOff { get; set; }
            public bool IsHopo {  get; set; } // A natural Hopo from being within a certain distance of another note
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
        public class AnimNote // This is for all animation notes, not just the "anim_notes" array. Also cameras, lights, etc.
        {
            public int Time { get; set; }
            public int Note {  get; set; }
            public int Length { get; set; }
            public int Velocity { get; set; }
            public AnimNote(int time, int note, int length, int velocity) 
            { 
                Time = time; 
                Note = note; 
                Length = length;
                Velocity = velocity;
            }
        }
        public class ScriptArray
        {
            public int Time { get; set; }
            public string Script { get; set; }
            public QBStructItem? Params { get; set; }
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
                HopoThreshold = HopoThreshold * TPB / 480 ;
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
    }
}
