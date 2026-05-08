using GH_Toolkit_Core.MIDI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Other
{
    public class MiscMethods
    {
        private static string[]? GetAllPaks(string folderPath)
        {
            string[]? filePaths = null;
            if (Directory.Exists(folderPath))
            {
                filePaths = Directory.GetFiles(folderPath, "*_s*.pak.*", SearchOption.AllDirectories);
            }
            else if (File.Exists(folderPath))
            {
                filePaths = [folderPath];
            }
            else
            {
                Console.WriteLine("Invalid path.");
                return null;
            }
            return filePaths;
        }
        public static void DuplicateChecker(string folderPath)
        {
            string[]? filePaths = GetAllPaks(folderPath);

            foreach (string file in filePaths) 
            {
                var songData = SongQbFile.TokenizePak(file);
                songData.Drums.CheckForDuplicates();
                Console.WriteLine(songData.GetErrorListAsString());
            }
        }
        public static void OverlapSustainChecker(string folderPath)
        {
            string[]? filePaths = GetAllPaks(folderPath);

            foreach (string file in filePaths)
            {
                var songData = SongQbFile.TokenizePak(file);
                songData.Guitar.CheckForOverlaps();
                Console.WriteLine(songData.GetErrorListAsString());
            }
        }
        [Flags]
        public enum Instrument
        {
            Guitar = 1,
            Rhythm = 2,
            Drums = 4
        }
        [Flags]
        public enum ScoreType
        {
            BaseScore = 1,
            NoSpScore = 2,
            NoteCount = 4
        }
        public static string? GetBaseScoreCSV(string filePath, int instrument = 1, int scoreType = 1)
        {
            if (!Directory.Exists(filePath) && !File.Exists(filePath))
            {
                Console.WriteLine("Invalid path. Please provide a valid folder or file path.");
                return null;
            }
            Instrument instrumentFlag = (Instrument)instrument;
            ScoreType scoreTypeFlag = (ScoreType)scoreType;
            string header = BuildScoreHeader(scoreTypeFlag);
            string csvPath;
            if (Directory.Exists(filePath))
            {
                string[]? filePaths = GetAllPaks(filePath);
                if (filePaths == null || filePaths.Length == 0)
                {
                    return null;
                }
                csvPath = Path.Combine(filePath, "baseScores.csv");
                using (var sw = new StreamWriter(csvPath))
                {
                    sw.WriteLine(header);
                    foreach (string file in filePaths)
                    {
                        var songData = SongQbFile.TokenizePak(file);
                        sw.Write(GetAllBaseScoresAsCSV(songData, instrumentFlag, scoreTypeFlag));
                    }
                }
            }
            else if (filePath.Contains(".pak", StringComparison.OrdinalIgnoreCase))
            {
                string? dirName = Path.GetDirectoryName(filePath);
                if (dirName == null)
                {
                    return null;
                }
                csvPath = Path.Combine(dirName, "baseScores.csv");
                using (var sw = new StreamWriter(csvPath))
                {
                    sw.WriteLine(header);
                    var songData = SongQbFile.TokenizePak(filePath);
                    sw.Write(GetAllBaseScoresAsCSV(songData, instrumentFlag, scoreTypeFlag));
                }
            }
            else if (filePath.Contains(".midi", StringComparison.OrdinalIgnoreCase) || filePath.Contains(".mid", StringComparison.OrdinalIgnoreCase))
            {
                string? dirName = Path.GetDirectoryName(filePath);
                if (dirName == null)
                {
                    return null;
                }
                csvPath = Path.Combine(dirName, "baseScores.csv");
                using (var sw = new StreamWriter(csvPath))
                {
                    sw.WriteLine(header);
                    var filename = Path.GetFileNameWithoutExtension(filePath);
                    var midiConvert = new SongQbFile(filePath, filename);
                    midiConvert.ParseMidiGH();
                    sw.Write(GetAllBaseScoresAsCSV(midiConvert, instrumentFlag, scoreTypeFlag));
                }
            }
            else
            {
                return null;
            }
            return csvPath;
        }

        private static string BuildScoreHeader(ScoreType scoreTypeFlag)
        {
            bool includeBase = scoreTypeFlag.HasFlag(ScoreType.BaseScore);
            bool includeNoSp = scoreTypeFlag.HasFlag(ScoreType.NoSpScore);
            bool includeNoteCount = scoreTypeFlag.HasFlag(ScoreType.NoteCount);
            var sb = new StringBuilder();
            sb.Append("Song Name, Instrument");
            string[] difficulties = ["Easy", "Medium", "Hard", "Expert"];
            foreach (var diff in difficulties)
            {
                if (includeBase)
                    sb.Append($", {diff} Base Score");
                if (includeNoSp)
                    sb.Append($", {diff} No SP Score");
                if (includeNoteCount)
                    sb.Append($", {diff} Note Count");
            }
            return sb.ToString();
        }

        private static string GetAllBaseScoresAsCSV(SongQbFile songData, Instrument instrumentFlag, ScoreType scoreTypeFlag)
        {
            var sb = new StringBuilder();
            var songName = songData.SongName;
            var baseScore = songData.CalculateBaseScore();
            bool includeBase = scoreTypeFlag.HasFlag(ScoreType.BaseScore);
            bool includeNoSp = scoreTypeFlag.HasFlag(ScoreType.NoSpScore);
            bool includeNoteCount = scoreTypeFlag.HasFlag(ScoreType.NoteCount);
            if (instrumentFlag.HasFlag(Instrument.Guitar))
            {
                sb.AppendLine($"{songName}, {Instrument.Guitar}, {baseScore.GuitarToCSV(includeBase, includeNoSp, includeNoteCount)}");
                if (baseScore.GuitarCoop.HasAnyScore)
                {
                    sb.AppendLine($"{songName}, {Instrument.Guitar} Coop, {baseScore.GuitarCoopToCSV(includeBase, includeNoSp, includeNoteCount)}");
                }
            }
            if (instrumentFlag.HasFlag(Instrument.Rhythm))
            {
                if (baseScore.Bass.HasAnyScore)
                {
                    sb.AppendLine($"{songName}, {Instrument.Rhythm}, {baseScore.BassToCSV(includeBase, includeNoSp, includeNoteCount)}");
                }
                if (baseScore.RhythmCoop.HasAnyScore)
                {
                    sb.AppendLine($"{songName}, {Instrument.Rhythm} Coop, {baseScore.RhythmCoopToCSV(includeBase, includeNoSp, includeNoteCount)}");
                }
            }
            if (instrumentFlag.HasFlag(Instrument.Drums))
            {
                sb.AppendLine($"{songName}, {Instrument.Drums}, {baseScore.DrumsToCSV(includeBase, includeNoSp, includeNoteCount)}");
            }
            return sb.ToString();
        }

        public static void CheckForErrors(string folderPath)
        {
            string[]? filePaths = GetAllPaks(folderPath);
            foreach (string file in filePaths)
            {
                var songData = SongQbFile.TokenizePak(file);
                songData.GetAllErrors();
                var errors = songData.GetErrorListAsString();
                if (string.IsNullOrWhiteSpace(errors))
                {
                    continue;
                }
                Console.WriteLine($"Errors for {songData.SongName}:");
                Console.WriteLine(errors);
            }
        }
    }
}
