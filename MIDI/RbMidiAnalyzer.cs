using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Text;
using static GH_Toolkit_Core.MIDI.MidiDefs;

/*
 * Standalone analyzer for Rock Band (and future) MIDI files.
 * Operates independently of the GH-specific SongQbFile parsing pipeline.
 */

namespace GH_Toolkit_Core.MIDI
{
    /// <summary>
    /// Result of a single overdrive/star-power phrase analysis.
    /// </summary>
    public class OverdrivePhraseDiagnostic
    {
        public string TrackName { get; }
        public string Difficulty { get; }
        public int StartTimeMs { get; }
        public int NoteCount { get; }

        public bool IsEmpty => NoteCount == 0;

        public OverdrivePhraseDiagnostic(string trackName, string difficulty, int startTimeMs, int noteCount)
        {
            TrackName = trackName;
            Difficulty = difficulty;
            StartTimeMs = startTimeMs;
            NoteCount = noteCount;
        }

        public override string ToString()
        {
            var ts = TimeSpan.FromMilliseconds(StartTimeMs);
            string timestamp = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
            return IsEmpty
                ? $"{TrackName} [{Difficulty}]: Overdrive phrase at {timestamp} has no notes"
                : $"{TrackName} [{Difficulty}]: Overdrive phrase at {timestamp} has {NoteCount} note(s)";
        }
    }

    /// <summary>
    /// Collected results from <see cref="RbMidiAnalyzer.Analyze"/>.
    /// </summary>
    public class MidiAnalysisResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public List<OverdrivePhraseDiagnostic> OverdrivePhrases { get; } = new();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;

        public string GetErrorsAsString() => string.Join("\r\n", Errors);
        public string GetWarningsAsString() => string.Join("\r\n", Warnings);
    }

    /// <summary>
    /// Analyzes a MIDI file for Rock Band-specific issues such as overdrive phrases
    /// with no notes underneath them. Extend by overriding <see cref="AnalyzeTrack"/>.
    /// </summary>
    public class RbMidiAnalyzer
    {
        // Playable note ranges shared by guitar, bass, drums, and keys in RB
        protected static readonly IReadOnlyDictionary<string, (int Min, int Max)> DifficultyRanges =
            new Dictionary<string, (int Min, int Max)>
            {
                { "easy",   (60, 64) },
                { "medium", (72, 76) },
                { "hard",   (84, 88) },
                { "expert", (96, 100) },
            };

        // Tracks that carry playable notes and overdrive phrases
        protected static readonly HashSet<string> DefaultInstrumentTracks =
            new(StringComparer.OrdinalIgnoreCase)
            {
                PARTGUITAR, PARTBASS, PARTDRUMS, PARTKEYS,
                PARTRHYTHM, PARTGUITARCOOP,
                PARTREALGUITAR, PARTREALBASS,
                PARTREALKEYS_X, PARTREALKEYS_H, PARTREALKEYS_M, PARTREALKEYS_E,
                PARTREALDRUMS,
            };

        // Event types that are expected in the EVENTS track
        protected static readonly HashSet<Type> StandardEventTypes =
            new() { typeof(TextEvent), typeof(LyricEvent), typeof(SequenceTrackNameEvent) };

        protected const int OverdriveNote = 116;

        private readonly MidiFile _midiFile;
        protected readonly TempoMap TempoMap;

        /// <param name="midiPath">Path to the .mid file to analyze.</param>
        public RbMidiAnalyzer(string midiPath)
        {
            var settings = new ReadingSettings { TextEncoding = Encoding.Latin1 };
            _midiFile = MidiFile.Read(midiPath, settings);
            TempoMap = _midiFile.GetTempoMap();
        }

        /// <param name="midiFile">Already-loaded <see cref="MidiFile"/> instance.</param>
        public RbMidiAnalyzer(MidiFile midiFile)
        {
            _midiFile = midiFile;
            TempoMap = midiFile.GetTempoMap();
        }

        /// <summary>
        /// Runs all checks and returns the collected results.
        /// </summary>
        public MidiAnalysisResult Analyze()
        {
            var result = new MidiAnalysisResult();

            foreach (var track in _midiFile.GetTrackChunks().Skip(1))
            {
                var name = GetTrackName(track);

                if (name.Equals(EVENTS, StringComparison.OrdinalIgnoreCase))
                {
                    AnalyzeEventsTrack(track, result);
                }
                else if (DefaultInstrumentTracks.Contains(name))
                {
                    AnalyzeTrack(track, name, result);
                }
            }

            return result;
        }     

        /// <summary>
        /// Override to add game-specific checks per instrument track.
        /// The base implementation checks for overdrive phrases without notes.
        /// </summary>
        protected virtual void AnalyzeTrack(TrackChunk track, string trackName, MidiAnalysisResult result)
        {
            var allNotes = track.GetNotes().ToList();
            CheckEmptyOverdrivePhrases(allNotes, trackName, result);
        }

        /// <summary>
        /// Override to add checks for the EVENTS track.
        /// The base implementation reports any event that is not a
        /// <see cref="TextEvent"/>, <see cref="LyricEvent"/>, or <see cref="SequenceTrackNameEvent"/>.
        /// </summary>
        protected virtual void AnalyzeEventsTrack(TrackChunk track, MidiAnalysisResult result)
        {
            CheckNonStandardTextEvents(track, result);
        }

        /// <summary>
        /// Warns about events in the EVENTS track whose type is not
        /// <see cref="TextEvent"/>, <see cref="LyricEvent"/>, or <see cref="SequenceTrackNameEvent"/>.
        /// </summary>
        protected void CheckNonStandardTextEvents(TrackChunk track, MidiAnalysisResult result)
        {
            foreach (var timedEvent in track.GetTimedEvents())
            {
                var e = timedEvent.Event;
                if (StandardEventTypes.Contains(e.GetType()))
                    continue;

                int timeMs = TicksToMs(timedEvent.Time);
                var ts = TimeSpan.FromMilliseconds(timeMs);
                string timestamp = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
                result.Warnings.Add(
                    $"{EVENTS}: Unexpected event type '{e.GetType().Name}' at {timestamp}");
            }
        }

        /// <summary>
        /// For each overdrive phrase on the track, checks every difficulty range for
        /// the presence of at least one playable note. Reports empty phrases as errors.
        /// </summary>
        protected void CheckEmptyOverdrivePhrases(
            IReadOnlyList<Note> allNotes,
            string trackName,
            MidiAnalysisResult result)
        {
            var phrases = allNotes
                .Where(n => n.NoteNumber == OverdriveNote)
                .ToList();

            if (phrases.Count == 0)
            {
                result.Warnings.Add($"{trackName}: No overdrive phrases found.");
                return;
            }

            foreach (var phrase in phrases)
            {
                int startMs = TicksToMs(phrase.Time);

                foreach (var (diff, (min, max)) in DifficultyRanges)
                {
                    int noteCount = allNotes.Count(n =>
                        n.NoteNumber >= min && n.NoteNumber <= max &&
                        n.Time >= phrase.Time && n.Time < phrase.EndTime);

                    var diagnostic = new OverdrivePhraseDiagnostic(trackName, diff, startMs, noteCount);
                    result.OverdrivePhrases.Add(diagnostic);

                    if (diagnostic.IsEmpty)
                        result.Errors.Add(diagnostic.ToString());
                }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        protected int TicksToMs(long ticks) =>
            (int)Math.Round(
                TimeConverter.ConvertTo<MetricTimeSpan>(ticks, TempoMap).TotalMilliseconds);

        protected static string GetTrackName(TrackChunk track)
        {
            foreach (var e in track.Events)
            {
                if (e is SequenceTrackNameEvent nameEvent)
                    return nameEvent.Text;
            }
            return "Unknown";
        }
    }
}
