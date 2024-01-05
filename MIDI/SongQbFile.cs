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

namespace GH_Toolkit_Core.MIDI
{
    public class SongQbFile
    {
        private const string PARTDRUMS = "PART DRUMS";
        private const string PARTGUITAR = "PART GUITAR";
        private const string PARTRHYTHM = "PART RHYTHM";
        private const string PARTGUITARCOOP = "PART GUITAR COOP";
        private const string PARTBASS = "PART BASS";
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
        private MidiFile SongMidiFile { get; set; }
        private TempoMap SongTempoMap { get; set; }
        private int TPB {  get; set; }
        public static string Game {  get; set; }
        public SongQbFile(string midiPath, string game = "GH3") 
        {
            Game = game;
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
                    case PARTBASS:
                        break;
                    case PARTGUITAR:
                        Guitar = new Instrument(trackChunk);
                        break;
                    case PARTGUITARCOOP:
                    case PARTRHYTHM:
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
            public List<PlayNote> Easy { get; set; }
            public List<PlayNote> Medium { get; set; }
            public List<PlayNote> Hard { get; set; }
            public List<PlayNote> Expert { get; set; }
            public List<int> StarPower { get; set; }
            public List<int> BattleStar { get; set; }
            public List<int> FaceOffP1 { get; set; }
            public List<int> FaceOffP2 { get; set; }
            public Instrument(TrackChunk trackChunk, bool drums = false)
            {
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

                // Filter notes for each difficulty level
                var easyNotes = allNotes.Where(n => n.NoteNumber >= 60 && n.NoteNumber <= 64).ToList();
                var easyChords = easyNotes.GetChords().ToList();
                var easyOn = allNotes.Where(n => n.NoteNumber == 65).ToList();
                var easyOff = allNotes.Where(n => n.NoteNumber == 66).ToList();

                var mediumNotes = allNotes.Where(n => n.NoteNumber >= (72 - openNotes) && n.NoteNumber <= (76 + drumsMode)).ToList();
                var mediumChords = mediumNotes.GetChords().ToList();
                var mediumOn = allNotes.Where(n => n.NoteNumber == 77).ToList();
                var mediumOff = allNotes.Where(n => n.NoteNumber == 78).ToList();

                var hardNotes = allNotes.Where(n => n.NoteNumber >= (84 - openNotes) && n.NoteNumber <= (88 + drumsMode)).ToList();
                var hardChords = hardNotes.GetChords().ToList();
                var hardOn = allNotes.Where(n => n.NoteNumber == 89).ToList();
                var hardOff = allNotes.Where(n => n.NoteNumber == 90).ToList();

                var expertNotes = allNotes.Where(n => n.NoteNumber >= (96 - openNotes) && n.NoteNumber <= (100 + drumsMode)).ToList();
                var expertChords = expertNotes.GetChords().ToList();
                var expertOn = allNotes.Where(n => n.NoteNumber == 101).ToList();
                var expertOff = allNotes.Where(n => n.NoteNumber == 102).ToList();
                Expert = MakeGuitar(expertChords, expertOn, expertOff, noteDict);
            }
            public List<PlayNote> MakeGuitar(List<Chord> chords, List<Note> forceOn, List<Note> forceOff, Dictionary<MidiTheory.NoteName, int> noteDict)
            {
                long prevTime = 0;
                for (int i = 0; i < chords.Count; i++)
                {
                    Chord notes = chords[i];
                    int noteVal = 0;
                    long endTime = notes.EndTime;
                    foreach (Note note in notes.Notes)
                    {
                        noteVal += noteDict[note.NoteName];
                        if (note.EndTime < endTime)
                        {
                            endTime = note.EndTime;
                        }
                    }
                }
            }
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

        public class Difficulty
        {
            public List<PlayNote> PlayNotes { get; set; } = new List<PlayNote>();

            private void GuitarNotes()
            {

            }
            private void DrumNotes()
            {

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
        public class PlayNote
        {
            public int Time { get; set; }
            public int Note { get; set; }
            public int Length { get; set; }
            public bool ForcedOn { get; set; }
            public bool ForcedOff { get; set; }
            public bool IsHopo {  get; set; } // A natural Hopo from being within a certain distance of another note
            public PlayNote()
            {

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

        private double TicksToMilliseconds(long ticks)
        {
            // Convert ticks to milliseconds
            var timeSpan = TimeConverter.ConvertTo<MetricTimeSpan>(ticks, SongTempoMap);
            return timeSpan.TotalMilliseconds;
        }
    }
}
