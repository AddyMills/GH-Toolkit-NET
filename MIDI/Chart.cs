using GH_Toolkit_Core.INI;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using MidiTheory = Melanchall.DryWetMidi.MusicTheory;
using MidiData = Melanchall.DryWetMidi.Interaction;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GH_Toolkit_Core.MIDI
{
    public partial class Chart
    {
        private string ChartFilePath { get; set; }
        private string IniFilePath { get; set; }
        private string MidiFilepath => Path.ChangeExtension(ChartFilePath, ".mid");
        private int? Resolution { get; set; }
        private bool HasResolution => Resolution.HasValue && Resolution.Value > 0;
        private int MinLength { get; set; } // Default minimum length for notes
        private DrumParseMode DrumMode { get; set; } = DrumParseMode.Neither; // Default to neither RB nor GH drum mode
        private MidiFile MidiFile { get; set; } = new MidiFile();
        private List<string> Song { get; set; } = new List<string>();
        private List<string> SyncTrack { get; set; } = new List<string>();
        private List<string> Events { get; set; } = new List<string>();
        private Dictionary<string, List<string>> Single { get; set; } = new Dictionary<string, List<string>>() // Lead Guitar
        {
            {"Expert", new List<string>() },
            {"Hard", new List<string>() },
            {"Medium", new List<string>() },
            {"Easy", new List<string>() },
        };

        private Dictionary<string, List<string>> DoubleGuitar { get; set; } = new Dictionary<string, List<string>>() // Co-op Guitar
        {
            {"Expert", new List<string>() },
            {"Hard", new List<string>() },
            {"Medium", new List<string>() },
            {"Easy", new List<string>() },
        };
        private Dictionary<string, List<string>> DoubleBass { get; set; } = new Dictionary<string, List<string>>() // Bass
        {
            {"Expert", new List<string>() },
            {"Hard", new List<string>() },
            {"Medium", new List<string>() },
            {"Easy", new List<string>() },
        };
        private Dictionary<string, List<string>> DoubleRhythm { get; set; } = new Dictionary<string, List<string>>() // Rhythm Guitar
        {
            {"Expert", new List<string>() },
            {"Hard", new List<string>() },
            {"Medium", new List<string>() },
            {"Easy", new List<string>() },
        };
        private Dictionary<string, List<string>> Keyboard { get; set; } = new Dictionary<string, List<string>>() // Keys
        {
            {"Expert", new List<string>() },
            {"Hard", new List<string>() },
            {"Medium", new List<string>() },
            {"Easy", new List<string>() },
        };
        private Dictionary<string, List<string>> Drums { get; set; } = new Dictionary<string, List<string>>() // Keys
        {
            {"Expert", new List<string>() },
            {"Hard", new List<string>() },
            {"Medium", new List<string>() },
            {"Easy", new List<string>() },
        };
        public Chart(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The chart file '{filePath}' does not exist.");
            }

            // Load the chart file
            ChartFilePath = filePath;
            CheckForSongIniAndParse();
        }
        public string GetMidiPath()
        {
            if (string.IsNullOrWhiteSpace(MidiFilepath))
            {
                throw new InvalidOperationException("MIDI file path is not set. Ensure the chart has been parsed.");
            }
            return MidiFilepath;
        }
        public int GetHopoResolution()
        {
            if (!HasResolution)
            {
                throw new InvalidOperationException("Resolution is not set. Ensure the chart has been parsed and a resolution is defined.");
            }
            int hopoRes = (int)(Resolution * HopoThreshold);
            return hopoRes;
        }
        private void CheckForSongIniAndParse()
        {
            if (!Path.Exists(ChartFilePath))
            {
                throw new FileNotFoundException($"The chart file '{ChartFilePath}' does not exist.");
            }
            var chartDir = Path.GetDirectoryName(ChartFilePath);
            IniFilePath = Path.Combine(chartDir, "song.ini");
            if (File.Exists(IniFilePath))
            {
                // Parse the song.ini file to get the resolution and other settings
                var iniData = iniParser.ReadIniFromPath(IniFilePath);
                var iniParsed = iniParser.ParseSongIni(iniData, "Song");
            }
            else
            {
                Console.WriteLine($"No song.ini found at {IniFilePath}. Using default resolution of {Resolution}.");
            }
        }
        private void SetResolutionFromSong()
        {
            foreach (var item in Song)
            {
                var match = Regex.Match(item, @"Resolution\s*=\s*(\d+)");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int resolution))
                    {
                        Resolution = resolution;
                        MinLength = Math.Max(2, 10 * resolution/480); // Default minimum length for notes, at least 2 ticks
                        Console.WriteLine($"Resolution set to {Resolution}.");
                        return;
                    }
                }
            }
        }
        private void CreateTempoTrack()
        {
            using (var tempoMapManager = new TempoMapManager(new TicksPerQuarterNoteTimeDivision((short)Resolution)))
            {
                TempoMap tempoMap = tempoMapManager.TempoMap;
                foreach (var sync in SyncTrack)
                {
                    var split = sync.Split('=', StringSplitOptions.TrimEntries);
                    var time = long.TryParse(split[0], out long tick) ? tick : -1;
                    var valueSplit = split[1].Split(' ', StringSplitOptions.TrimEntries);
                    var eventType = valueSplit[0];
                    switch (eventType)
                    {
                        case "TS":
                            // Time Signature event
                            var numerator = int.Parse(valueSplit[1]); // Default to 4
                            int denominator = 4;
                            if (valueSplit.Length > 2)
                            {
                                denominator = 2 ^ int.Parse(valueSplit[2]); // Default to 4
                            }
                            var timeSignature = new TimeSignature(numerator, denominator);
                            tempoMapManager.SetTimeSignature(time, timeSignature);
                            break;
                        case "B":
                            // BPM event
                            int bpm = int.Parse(valueSplit[1]);
                            if (bpm > 0)
                            {
                                var floatTempo = bpm / 1000.0f;
                                var tempoEvent = Tempo.FromBeatsPerMinute(floatTempo);
                                tempoMapManager.SetTempo(time, tempoEvent);
                            }
                            else
                            {
                                Console.WriteLine($"Invalid BPM value: {bpm} at time {time}. Skipping.");
                            }
                            break;
                    }
                }
                MidiFile.ReplaceTempoMap(tempoMapManager.TempoMap);
            }
            //MidiFile.Write(MidiFilepath, true);
        }
        private void ProcessEvents()
        {
            var trackChunk = new TrackChunk();
            trackChunk.Events.Add(new SequenceTrackNameEvent("EVENTS"));
            using (var manager = new TimedObjectsManager(trackChunk.Events, ObjectType.Note | ObjectType.TimedEvent))
            {
                foreach (var textEvent in Events)
                {
                    var split = textEvent.Split('=', StringSplitOptions.TrimEntries);
                    var time = long.TryParse(split[0], out long tick) ? tick : -1;

                    var valueSplit = split[1].Split(' ', 2, StringSplitOptions.TrimEntries);
                    if (valueSplit[0] == "E")
                    {
                        var newText = CleanEvent(valueSplit[1]);
                        if (newText == null)
                        {
                            continue;
                        }
                        var midiText = new TextEvent(newText);
                        var timedEvent = new TimedEvent(midiText, time);
                        manager.Objects.Add(timedEvent);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown event type '{valueSplit[0]}' in event: {textEvent}. Skipping.");
                    }
                }
            }
            // Add the processed track to the MIDI file
            if (trackChunk.Events.Count > 0)
            {
                MidiFile.Chunks.Add(trackChunk);
            }
            else
            {
                Console.WriteLine("No valid events found. Skipping event track.");
            }
        }
        private string? CleanEvent(string text)
        {
            if (text.StartsWith("\"") && text.EndsWith("\""))
            {
                text = text[1..^1]; // Remove surrounding quotes
            }
            if (text.StartsWith("[") && text.EndsWith("]"))
            {
                text = text[1..^1];
            }
            if (text.Contains("lyric ") || text == "phrase_start" || text == "phrase_end")
            {
                return null;
            }
            text = $"[{text}]"; // Ensure it starts and ends with a bracket
            return text;
        }
        private void ProcessInstrument(Dictionary<string, List<string>> inst, string name)
        {
            if (inst["Expert"].Count == 0 && inst["Hard"].Count == 0 && inst["Medium"].Count == 0 && inst["Easy"].Count == 0)
            {
                Console.WriteLine($"No notes found for {name}. Skipping.");
                return;
            }
            Console.WriteLine($"Processing {name} notes.");
            var trackChunk = new TrackChunk();
            trackChunk.Events.Add(new SequenceTrackNameEvent($"PART {name}"));
            using (var manager = new TimedObjectsManager(trackChunk.Events, ObjectType.Note | ObjectType.TimedEvent))
            {
                foreach (var diff in DiffBaseNote.Keys)
                {
                    var baseNote = DiffBaseNote[diff];
                    bool tapRange = false;
                    int notesSinceTap = 0;
                    long lastNoteTime = 0;
                    long lastTapTime = 0;
                    foreach (var note in inst[diff])
                    {
                        var split = note.Split('=', StringSplitOptions.TrimEntries);
                        var time = long.TryParse(split[0], out long tick) ? tick : -1;
                        var valueSplit = split[1].Split(' ', StringSplitOptions.TrimEntries);
                        var eventType = valueSplit[0];
                        int length;
                        SevenBitNumber midiNote;
                        switch (eventType)
                        {
                            case "N": // Note event
                                int chartNote = int.Parse(valueSplit[1]);
                                length = int.Parse(valueSplit[2]);
                                if (length == 0)
                                {
                                    length = MinLength;
                                }
                                if (NoteModifier.ContainsKey(chartNote))
                                {
                                    if (tapRange && lastNoteTime != time)
                                    {
                                        notesSinceTap += 1;
                                        lastNoteTime = time;
                                    }
                                    if (notesSinceTap > 1 && tapRange)
                                    {
                                        tapRange = false;
                                        var sysexEnd = CreateSysexEvent(diff, SYSTAP, false);
                                        var sysexEndEvent = new NormalSysExEvent(sysexEnd);
                                        var timedEnd = new TimedEvent(sysexEndEvent, lastTapTime);
                                        manager.Objects.Add(timedEnd);
                                    }
                                    int noteMod = NoteModifier[chartNote];
                                    midiNote = (SevenBitNumber)(baseNote + noteMod);
                                    var noteEvent = new MidiData.Note(midiNote, length, time);
                                    manager.Objects.Add(noteEvent);
                                }
                                else if (chartNote == TAPNOTE)
                                {
                                    if (!tapRange)
                                    {
                                        tapRange = true;
                                        var sysexStart = CreateSysexEvent(diff, SYSTAP, true);
                                        var sysexStartEvent = new NormalSysExEvent(sysexStart);
                                        var timedStart = new TimedEvent(sysexStartEvent, time);
                                        manager.Objects.Add(timedStart);
                                    }
                                    notesSinceTap = 0; // Reset tap count for a new tap note
                                    lastTapTime = time + length;
                                    /*
                                    var sysexEnd = CreateSysexEvent(diff, SYSTAP, false);

                                    
                                    var sysexEndEvent = new NormalSysExEvent(sysexEnd);

                                    
                                    var timedEnd = new TimedEvent(sysexEndEvent, time + length);
                                    
                                    manager.Objects.Add(timedEnd);*/
                                }
                                break;
                            case "S":
                                // Special Event
                                if (diff != "Expert")
                                {
                                    // Only process special events for Expert difficulty
                                    continue;
                                }
                                int specialNote = int.Parse(valueSplit[1]);
                                length = int.Parse(valueSplit[2]);
                                if (length == 0)
                                {
                                    length = 10;
                                }
                                switch (specialNote)
                                {
                                    case 0:
                                    case 1: // Face-off Notes
                                        midiNote = (SevenBitNumber)(FACEOFFBASENOTE + specialNote);
                                        break;
                                    case 2: // Star Power
                                        midiNote = (SevenBitNumber)STARPOWERNOTE;
                                        break;
                                    default:
                                        Console.WriteLine($"Unknown special note type: {specialNote} at time {time}. Skipping.");
                                        continue;
                                }
                                var specialNoteEvent = new MidiData.Note(midiNote, length, time);
                                manager.Objects.Add(specialNoteEvent);
                                break;
                        }
                    }
                    if (tapRange)
                    {
                        // If we ended a tap range, add the end event
                        var sysexEnd = CreateSysexEvent(diff, SYSTAP, false);
                        var sysexEndEvent = new NormalSysExEvent(sysexEnd);
                        var timedEnd = new TimedEvent(sysexEndEvent, lastTapTime);
                        manager.Objects.Add(timedEnd);
                    }
                }
            }
            // Add the processed track to the MIDI file
            MidiFile.Chunks.Add(trackChunk);
        }
        private byte[] CreateSysexEvent(string diff, byte noteMod, bool start)
        {
            var diffByte = DiffSysex[diff];
            var endByte = start ? (byte)0x01 : (byte)0x00;
            List<byte> sysexEvent = [ .. PS_SYSEX, SYSMOD, diffByte, noteMod, endByte, SYSEND];
            return sysexEvent.ToArray();
        }
        public void ConvertChartToMid()
        {
            if (!File.Exists(ChartFilePath))
            {
                throw new FileNotFoundException($"The chart file '{ChartFilePath}' does not exist.");
            }

            Console.WriteLine($"Converting chart from {ChartFilePath} to MIDI.");

            var chartParsed = ParseChart(ChartFilePath);
            foreach (var item in chartParsed)
            {
                switch (item.Key)
                {
                    case "Song":
                        Song = item.Value;
                        break;
                    case "SyncTrack":
                        SyncTrack = item.Value;
                        break;
                    case "Events":
                        Events = item.Value;
                        break;
                    case "ExpertSingle":
                        Single["Expert"] = item.Value;
                        break;
                    case "HardSingle":
                        Single["Hard"] = item.Value;
                        break;
                    case "MediumSingle":
                        Single["Medium"] = item.Value;
                        break;
                    case "EasySingle":
                        Single["Easy"] = item.Value;
                        break;
                    case "ExpertDoubleGuitar":
                        DoubleGuitar["Expert"] = item.Value;
                        break;
                    case "HardDoubleGuitar":
                        DoubleGuitar["Hard"] = item.Value;
                        break;
                    case "MediumDoubleGuitar":
                        DoubleGuitar["Medium"] = item.Value;
                        break;
                    case "EasyDoubleGuitar":
                        DoubleGuitar["Easy"] = item.Value;
                        break;
                    case "ExpertDoubleBass":
                        DoubleBass["Expert"] = item.Value;
                        break;
                    case "HardDoubleBass":
                        DoubleBass["Hard"] = item.Value;
                        break;
                    case "MediumDoubleBass":
                        DoubleBass["Medium"] = item.Value;
                        break;
                    case "EasyDoubleBass":
                        DoubleBass["Easy"] = item.Value;
                        break;
                    case "ExpertDoubleRhythm":
                        DoubleRhythm["Expert"] = item.Value;
                        break;
                    case "HardDoubleRhythm":
                        DoubleRhythm["Hard"] = item.Value;
                        break;
                    case "MediumDoubleRhythm":
                        DoubleRhythm["Medium"] = item.Value;
                        break;
                    case "EasyDoubleRhythm":
                        DoubleRhythm["Easy"] = item.Value;
                        break;
                    case "ExpertKeyboard":
                        Keyboard["Expert"] = item.Value;
                        break;
                    case "HardKeyboard":
                        Keyboard["Hard"] = item.Value;
                        break;
                    case "MediumKeyboard":
                        Keyboard["Medium"] = item.Value;
                        break;
                    case "EasyKeyboard":
                        Keyboard["Easy"] = item.Value;
                        break;
                    case "ExpertDrums":
                        Drums["Expert"] = item.Value;
                        break;
                    case "HardDrums":
                        Drums["Hard"] = item.Value;
                        break;
                    case "MediumDrums":
                        Drums["Medium"] = item.Value;
                        break;
                    case "EasyDrums":
                        Drums["Easy"] = item.Value;
                        break;
                }

            }
            SetResolutionFromSong();
            CreateTempoTrack();
            ProcessInstrument(Drums, "DRUMS");
            ProcessInstrument(Single, "GUITAR");
            ProcessInstrument(DoubleGuitar, "GUITAR COOP");
            ProcessInstrument(DoubleBass, "BASS");
            ProcessInstrument(DoubleRhythm, "RHYTHM");
            ProcessInstrument(Keyboard, "KEYS");
            ProcessEvents();
        }
        public void WriteMidToFile()
        {
            MidiFile.Write(MidiFilepath, true);
            Console.WriteLine($"MIDI file from .chart written to {MidiFilepath}.");
        }
        private static Dictionary<string, List<string>> ParseChart(string filePath)
        {
            var result = new Dictionary<string, List<string>>();
            string currentCategory = null;
            List<string> currentItems = null;

            foreach (var line in File.ReadLines(filePath))
            {
                string trimmedLine = line.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Check for category header [CategoryName]
                var categoryMatch = Regex.Match(trimmedLine, @"^\[([^\]]+)\]$");
                if (categoryMatch.Success)
                {
                    // Save previous category if exists
                    if (currentCategory != null)
                    {
                        result[currentCategory] = currentItems;
                    }

                    // Start new category
                    currentCategory = categoryMatch.Groups[1].Value;
                    currentItems = new List<string>();
                    continue;
                }

                // Check for opening brace (ignore it)
                if (trimmedLine == "{")
                    continue;

                // Check for closing brace
                if (trimmedLine == "}")
                {
                    if (currentCategory != null)
                    {
                        result[currentCategory] = currentItems;
                        currentCategory = null;
                        currentItems = null;
                    }
                    continue;
                }

                // If we're inside a category block, add the line to current items
                if (currentItems != null)
                {
                    currentItems.Add(trimmedLine);
                }
            }

            // Add the last category if file doesn't end with a closing brace
            if (currentCategory != null && currentItems != null)
            {
                result[currentCategory] = currentItems;
            }

            return result;
        }
    }
}
