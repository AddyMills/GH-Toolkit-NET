using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using static GH_Toolkit_Core.MIDI.MidiDefs;
using static GH_Toolkit_Core.QB.QBConstants;

using MidiTheory = Melanchall.DryWetMidi.MusicTheory;
using MidiData = Melanchall.DryWetMidi.Interaction;

namespace GH_Toolkit_Core.MIDI
{
    public partial class SongQbFile
    {
        public partial class Instrument
        {
            internal abstract class InstrumentGHParser
            {
                protected readonly Instrument _instrument;
                protected readonly TrackChunk _trackChunk;
                protected readonly SongQbFile _songQb;
                protected readonly List<MidiData.Note> _allNotes;
                protected readonly List<TimedEvent> _timedEvents;
                protected readonly List<TimedEvent> _textEvents;
                protected Dictionary<MidiTheory.NoteName, int>? _noteDict;
                protected int _openNotes;

                protected InstrumentGHParser(Instrument instrument, TrackChunk trackChunk, SongQbFile songQb)
                {
                    _instrument = instrument;
                    _trackChunk = trackChunk;
                    _songQb = songQb;
                    instrument._songQb = songQb;

                    _allNotes = trackChunk.GetNotes().ToList();
                    _timedEvents = trackChunk.GetTimedEvents().ToList();
                    _textEvents = _timedEvents.Where(e => e.Event is TextEvent).ToList();

                    _openNotes = IsOldGame() ? 0 : 1;
                }

                public void Parse()
                {
                    SetupDictionaries();
                    if (_noteDict == null)
                    {
                        throw new Exception("Note dictionary not set up correctly.");
                    }
                    ProcessFaceOffNotes();
                    ProcessPerformanceScripts();
                    ExtractStarPowerPhrases();
                    ProcessAnimsAndDifficulties();
                    FillEmptyDifficulties();
                    PostProcess();
                }

                protected abstract void SetupDictionaries();
                protected abstract void ProcessAnimsAndDifficulties();
                protected virtual void PostProcess() { }

                protected bool IsOldGame()
                {
                    return _songQb.Game == GAME_GH3 || _songQb.Game == GAME_GHA;
                }

                private void ProcessFaceOffNotes()
                {
                    var faceOffP1Notes = _allNotes.Where(x => x.NoteNumber == FaceOffP1Note).ToList();
                    _instrument.FaceOffP1 = _instrument.ProcessOtherSections(faceOffP1Notes, _songQb);
                    var faceOffP2Notes = _allNotes.Where(x => x.NoteNumber == FaceOffP2Note).ToList();
                    _instrument.FaceOffP2 = _instrument.ProcessOtherSections(faceOffP2Notes, _songQb);
                }

                private void ProcessPerformanceScripts()
                {
                    bool isOldGame = IsOldGame();
                    if ((isOldGame && _instrument.TrackName != DRUMS_NAME) || !isOldGame)
                    {
                        try
                        {
                            _instrument.PerformanceScript = _songQb.InstrumentScripts(
                                _textEvents, ActorNameFromTrack[_instrument.TrackName]);
                        }
                        catch
                        {
                            // Nothing to do here
                        }
                    }
                }

                private void ExtractStarPowerPhrases()
                {
                    _instrument.StarPowerPhrases = _allNotes.Where(x => x.NoteNumber == StarPowerNote).ToList();
                    _instrument.BattleStarPhrases = _allNotes.Where(x => x.NoteNumber == BattleStarNote).ToList();
                    if (_instrument.BattleStarPhrases.Count == 0)
                    {
                        _instrument.BattleStarPhrases = _instrument.StarPowerPhrases;
                    }
                    _instrument.FaceOffStarPhrases = _allNotes.Where(x => x.NoteNumber == FaceOffStarNote).ToList();
                    if (_instrument.FaceOffStarPhrases.Count == 0)
                    {
                        _instrument.FaceOffStarPhrases = _instrument.StarPowerPhrases;
                    }
                }

                private void FillEmptyDifficulties()
                {
                    if (_instrument.Hard.PlayNotes.Count == 0)
                    {
                        _instrument.Hard.PlayNotes = _instrument.Expert.PlayNotes;
                    }
                    if (_instrument.Medium.PlayNotes.Count == 0)
                    {
                        _instrument.Medium.PlayNotes = _instrument.Hard.PlayNotes;
                    }
                    if (_instrument.Easy.PlayNotes.Count == 0)
                    {
                        _instrument.Easy.PlayNotes = _instrument.Medium.PlayNotes;
                    }
                    _instrument.FaceOffStar = _instrument.Easy.FaceOffStar;
                }
            }

            internal class GuitarGHParser : InstrumentGHParser
            {
                private Dictionary<int, int> _animDict = new Dictionary<int, int>();
                private List<TimedEvent>? _sysExEvents;
                private int _easyOpens;
                private List<StarPower> _sysexTaps = new List<StarPower>();
                private Dictionary<int, List<StarPower>> _sysexOpens = new Dictionary<int, List<StarPower>>
                {
                    { 0, new List<StarPower>() },
                    { 1, new List<StarPower>() },
                    { 2, new List<StarPower>() },
                    { 3, new List<StarPower>() }
                };

                public GuitarGHParser(Instrument instrument, TrackChunk trackChunk, SongQbFile songQb)
                    : base(instrument, trackChunk, songQb)
                {
                }

                protected override void SetupDictionaries()
                {
                    if (IsOldGame())
                    {
                        SetupOldGameDictionaries();
                    }
                    else
                    {
                        SetupNewGameDictionaries();
                    }
                    ProcessSysExEvents();
                }

                private void SetupOldGameDictionaries()
                {
                    _noteDict = Gh3Notes;
                    _easyOpens = 0;
                    _animDict = leftHandMappingsGh3.GetValueOrDefault(
                        _instrument.TrackName, leftHandMappingsGh3[RHYTHM_NAME]);
                    if (_songQb.Game == GAME_GH3 && _songQb.Gh3Plus)
                    {
                        _openNotes = 1;
                        _sysExEvents = _timedEvents.Where(e => e.Event is NormalSysExEvent).ToList();
                    }
                }

                private void SetupNewGameDictionaries()
                {
                    _noteDict = Gh4Notes;
                    _easyOpens = _songQb.EasyOpens ? 1 : 0;
                    _sysExEvents = _timedEvents.Where(e => e.Event is NormalSysExEvent).ToList();
                    _animDict = leftHandMappingsWt.GetValueOrDefault(
                        _instrument.TrackName, leftHandMappingsWt[""]);
                    if (_easyOpens == 1 && _animDict.ContainsKey(59))
                    {
                        _animDict[58] = _animDict[59];
                        _animDict.Remove(59);
                    }
                }

                private void ProcessSysExEvents()
                {
                    if (_sysExEvents != null)
                    {
                        (_sysexTaps, _sysexOpens) = _instrument.SplitSysEx(_sysExEvents);
                    }
                }

                protected override void ProcessAnimsAndDifficulties()
                {
                    _instrument.AnimNotes = _instrument.InstrumentAnims(
                        _allNotes, GuitarAnimStart, GuitarAnimEnd, _animDict, _songQb);

                    var sp = _instrument.StarPowerPhrases;
                    var bm = _instrument.BattleStarPhrases;
                    var fo = _instrument.FaceOffStarPhrases;

                    _instrument.Easy.ProcessDifficultyGuitar(_allNotes, EasyNoteMin, EasyNoteMax, _noteDict,
                        _easyOpens, _songQb, sp, bm, fo,
                        sysexTaps: _sysexTaps, sysexOpens: _sysexOpens[0], trackName: _instrument.TrackName);
                    _instrument.Medium.ProcessDifficultyGuitar(_allNotes, MediumNoteMin, MediumNoteMax, _noteDict,
                        _openNotes, _songQb, sp, bm,
                        sysexTaps: _sysexTaps, sysexOpens: _sysexOpens[1], trackName: _instrument.TrackName);
                    _instrument.Hard.ProcessDifficultyGuitar(_allNotes, HardNoteMin, HardNoteMax, _noteDict,
                        _openNotes, _songQb, sp, bm,
                        sysexTaps: _sysexTaps, sysexOpens: _sysexOpens[2], trackName: _instrument.TrackName);
                    _instrument.Expert.ProcessDifficultyGuitar(_allNotes, ExpertNoteMin, ExpertNoteMax, _noteDict,
                        _openNotes, _songQb, sp, bm,
                        sysexTaps: _sysexTaps, sysexOpens: _sysexOpens[3], trackName: _instrument.TrackName);
                }

                protected override void PostProcess()
                {
                    if (_songQb.Game == GAME_GHWT && GamePlatform == CONSOLE_PC)
                    {
                        _instrument.SoloMarker = _instrument.ProcessStartEndArrays(
                            _allNotes.Where(x => x.NoteNumber == SoloNote).ToList(), _songQb, true);
                    }
                }
            }

            internal class DrumGHParser : InstrumentGHParser
            {
                private Dictionary<int, int> _drumAnimDict = new Dictionary<int, int>();

                public DrumGHParser(Instrument instrument, TrackChunk trackChunk, SongQbFile songQb)
                    : base(instrument, trackChunk, songQb)
                {
                }

                protected override void SetupDictionaries()
                {
                    if (IsOldGame())
                    {
                        _noteDict = Gh3Notes;
                        _drumAnimDict = drumKeyMapRB_gh3;
                    }
                    else
                    {
                        _noteDict = Gh4Drums;
                        _drumAnimDict = _songQb.Game == GAME_GHWOR ? drumKeyMapRB_wor : drumKeyMapRB_wt;
                    }
                }

                protected override void ProcessAnimsAndDifficulties()
                {
                    var drumFillNotes = _allNotes.Where(x => x.NoteNumber == TapNote).ToList();
                    _instrument.AnimNotes = _instrument.InstrumentAnims(
                        _allNotes, DrumAnimStart, DrumAnimEnd, _drumAnimDict, _songQb, true);
                    _instrument.DrumFill = _instrument.ProcessStartEndArrays(drumFillNotes, _songQb);

                    var sp = _instrument.StarPowerPhrases;
                    var bm = _instrument.BattleStarPhrases;
                    var fo = _instrument.FaceOffStarPhrases;

                    _instrument.Easy.ProcessDifficultyDrums(_allNotes, EasyNoteMin, EasyNoteMax + 1,
                        _noteDict, 0, _songQb, sp, bm, fo);
                    _instrument.Medium.ProcessDifficultyDrums(_allNotes, MediumNoteMin, MediumNoteMax + 1,
                        _noteDict, 0, _songQb, sp, bm);
                    _instrument.Hard.ProcessDifficultyDrums(_allNotes, HardNoteMin, HardNoteMax + 1,
                        _noteDict, 0, _songQb, sp, bm);
                    _instrument.Expert.ProcessDifficultyDrums(_allNotes, ExpertNoteMin, ExpertNoteMax + 1,
                        _noteDict, _openNotes, _songQb, sp, bm);
                }
            }
        }
    }
}
