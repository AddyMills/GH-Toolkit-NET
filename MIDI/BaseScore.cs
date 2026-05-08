using System.Text;

namespace GH_Toolkit_Core.MIDI
{
    public class BaseScore
    {
        public InstrumentScore Guitar { get; set; } = new InstrumentScore();
        public InstrumentScore GuitarCoop { get; set; } = new InstrumentScore();
        public InstrumentScore Bass { get; set; } = new InstrumentScore();
        public InstrumentScore RhythmCoop { get; set; } = new InstrumentScore();
        public InstrumentScore Drums { get; set; } = new InstrumentScore();

        public InstrumentScore GetInstrumentScore(string instrument)
        {
            return instrument.ToLower() switch
            {
                "guitar" => Guitar,
                "guitarcoop" => GuitarCoop,
                "bass" => Bass,
                "rhythmcoop" => RhythmCoop,
                "drums" => Drums,
                _ => throw new ArgumentException($"Unknown instrument: {instrument}")
            };
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Guitar: {Guitar}");
            sb.AppendLine($"Guitar Coop: {GuitarCoop}");
            sb.AppendLine($"Bass: {Bass}");
            sb.AppendLine($"Rhythm Coop: {RhythmCoop}");
            sb.Append($"Drums: {Drums}");
            return sb.ToString();
        }

        public string GuitarToCSV()
        {
            return Guitar.ToCSV();
        }
        public string GuitarToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount)
        {
            return Guitar.ToCSV(includeBase, includeNoSp, includeNoteCount);
        }
        public string GuitarCoopToCSV()
        {
            return GuitarCoop.ToCSV();
        }
        public string GuitarCoopToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount)
        {
            return GuitarCoop.ToCSV(includeBase, includeNoSp, includeNoteCount);
        }
        public string BassToCSV()
        {
            return Bass.ToCSV();
        }
        public string BassToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount)
        {
            return Bass.ToCSV(includeBase, includeNoSp, includeNoteCount);
        }
        public string RhythmCoopToCSV()
        {
            return RhythmCoop.ToCSV();
        }
        public string RhythmCoopToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount)
        {
            return RhythmCoop.ToCSV(includeBase, includeNoSp, includeNoteCount);
        }
        public string DrumsToCSV()
        {
            return Drums.ToCSV();
        }
        public string DrumsToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount )
        {
            return Drums.ToCSV(includeBase, includeNoSp, includeNoteCount);
        }
    }

    public class InstrumentScore
    {
        public DifficultyScore Easy { get; set; } = new DifficultyScore();
        public DifficultyScore Medium { get; set; } = new DifficultyScore();
        public DifficultyScore Hard { get; set; } = new DifficultyScore();
        public DifficultyScore Expert { get; set; } = new DifficultyScore();
        public bool HasAnyScore => Easy.BasePoints > 0 || Medium.BasePoints > 0 || Hard.BasePoints > 0 || Expert.BasePoints > 0;

        public DifficultyScore GetDifficultyScore(string difficulty)
        {
            return difficulty.ToLower() switch
            {
                "easy" => Easy,
                "medium" => Medium,
                "hard" => Hard,
                "expert" => Expert,
                _ => throw new ArgumentException($"Unknown difficulty: {difficulty}")
            };
        }

        public override string ToString()
        {
            return $"Easy: {Easy}, Medium: {Medium}, Hard: {Hard}, Expert: {Expert}";
        }

        public string ToCSV()
        {
            return $"{Easy.ToCSV()}, {Medium.ToCSV()}, {Hard.ToCSV()}, {Expert.ToCSV()}";
        }
        public string ToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount)
        {
            return $"{Easy.ToCSV(includeBase, includeNoSp, includeNoteCount)}, {Medium.ToCSV(includeBase, includeNoSp, includeNoteCount)}, {Hard.ToCSV(includeBase, includeNoSp, includeNoteCount)}, {Expert.ToCSV(includeBase, includeNoSp, includeNoteCount)}";
        }
    }

    public class DifficultyScore
    {
        public int BasePoints { get; set; }
        public int NoSpScore { get; set; }
        public int NoteCount { get; set; }

        public DifficultyScore() { }

        public DifficultyScore(int basePoints, int noSpScore, int noteCount)
        {
            BasePoints = basePoints;
            NoSpScore = noSpScore;
            NoteCount = noteCount;
        }

        public override string ToString()
        {
            return $"(Base: {BasePoints}, NoSP: {NoSpScore})";
        }
        public string ToCSV()
        {
            return $"{BasePoints}, {NoSpScore}";
        }
        public string ToCSV(bool includeBase, bool includeNoSp, bool includeNoteCount)
        {
            if (includeBase && includeNoSp && includeNoteCount)
                return $"{BasePoints}, {NoSpScore}, {NoteCount}";
            if (includeBase && includeNoSp)
                return $"{BasePoints}, {NoSpScore}";
            if (includeBase && includeNoteCount)
                return $"{BasePoints}, {NoteCount}";
            if (includeNoSp && includeNoteCount)
                return $"{NoSpScore}, {NoteCount}";
            if (includeBase)
                return $"{BasePoints}";
            if (includeNoSp)
                return $"{NoSpScore}";
            if (includeNoteCount)
                return $"{NoteCount}";
            return "";
        }
    }
}
