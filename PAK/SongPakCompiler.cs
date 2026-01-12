using GH_Toolkit_Core.MIDI;
using GH_Toolkit_Core.SKA;
using static GH_Toolkit_Core.Methods.Exceptions;
using static GH_Toolkit_Core.MIDI.SongQbFile;
using static GH_Toolkit_Core.PAK.PAK;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.Checksum.CRC;

namespace GH_Toolkit_Core.PAK
{
    public sealed class SongPakCompiler
    {
        // ===== Inputs =====
        private readonly string _midiPath;
        private readonly string _savePath;
        private readonly string _songName;
        private readonly string _game;
        private readonly string _gameConsole;
        private readonly string _consoleExt;
        public string SkaPath { get; set; } = "";
        public string PerfOverride { get; set; } = "";
        public string SongScripts { get; set; } = "";
        public string SkaSource { get; set; } = "GHWT";
        public string VenueSource { get; set; } = "";
        public int HopoThreshold { get; set; } = 170;
        public int HopoType { get; set; } = 0;
        public bool RhythmTrack { get; set; }
        public bool OverrideBeat { get; set; }
        public bool IsSteven { get; set; }
        public bool EasyOpens { get; set; }
        public bool Gh3Plus { get; set; }
        public Dictionary<string, int>? Diffs { get; set; }
        public string Gender { get; set; } = "Male";
        public string EffectiveSongName { get; set; }
        // ===== Internal state =====
        private SongQbFile? _midiFile;
        private byte[]? _midQb;
        private byte[]? _midQbExpertPlus;
        private bool _hasBuilt;        
        // ===== Results =====
        private string? _mainPakPath;
        private string? _expertPlusPakPath;
        private bool _doubleKick;
        // ===== Constructor =====
        public SongPakCompiler(
            string midiPath,
            string savePath,
            string songName,
            string game,
            string gameConsole)
        {
            _midiPath = midiPath;
            _savePath = savePath;
            _songName = songName;
            _game = game;
            _gameConsole = gameConsole;
            _consoleExt = gameConsole == CONSOLE_PS2 ? DOTPS2 : DOTXEN;
            EffectiveSongName = songName;
        }
        // ===== Public API =====
        public void Build()
        {
            if (_hasBuilt)
                throw new InvalidOperationException("This compiler instance has already been used. Create a new instance.");
            _hasBuilt = true;
            NormalizeGender();
            ParseMidi();
            ValidateMidi();
            BuildPakOutputs();
        }
        public string GetMainPakPath()
        => _mainPakPath ?? throw new InvalidOperationException("Build() not called.");

        public string? GetExpertPlusPakPath() => _expertPlusPakPath;

        public bool GetDoubleKick() => _doubleKick;

        public (string mainPak, bool doubleKick, string? xPlusPak) GetResults()
            => (GetMainPakPath(), GetDoubleKick(), GetExpertPlusPakPath());
        // ===== Internal steps =====
        private void NormalizeGender()
        {
            if (Gender.Equals("none", StringComparison.OrdinalIgnoreCase))
                Gender = "none";
            else if (!Gender.Equals("female", StringComparison.OrdinalIgnoreCase))
                Gender = "Male";
        }
        private void ParseMidi()
        {
            var midiExt = Path.GetExtension(_midiPath);
            var midiHopoType = (MidiDefs.HopoType)HopoType;

            if (midiExt == ".mid" || midiExt == ".chart")
            {
                bool fromChart = false;
                string path = _midiPath;

                if (midiExt == ".chart")
                {
                    var chart = new Chart(_midiPath);
                    chart.ConvertChartToMid();
                    chart.WriteMidToFile();

                    path = chart.GetMidiPath();
                    HopoThreshold = chart.GetHopoResolution();
                    midiHopoType = MidiDefs.HopoType.GH3;
                    EasyOpens = true;
                    fromChart = true;
                }

                _midiFile = new SongQbFile(
                    path,
                    songName: _songName,
                    game: _game,
                    console: _gameConsole,
                    hopoThreshold: HopoThreshold,
                    perfOverride: PerfOverride,
                    songScriptOverride: SongScripts,
                    venueSource: VenueSource,
                    rhythmTrack: RhythmTrack,
                    overrideBeat: OverrideBeat,
                    hopoType: midiHopoType,
                    easyOpens: EasyOpens,
                    skaPath: SkaPath,
                    fromChart: fromChart,
                    gh3Plus: Gh3Plus
                );

                _midQb = _midiFile.ParseMidiToQb();
                EffectiveSongName = _midiFile.GetSongName();

                if (_midiFile.Drums.HasExpertPlus && _game == GAME_GHWT)
                {
                    _midQbExpertPlus = _midiFile.ParseMidiToQb(true);
                }
            }
            else if (midiExt == ".q")
            {
                _midiFile = new SongQbFile(_songName, _midiPath, SongScripts, game: _game, console: _gameConsole);
                _midQb = _midiFile.MakeConsoleQb();
            }
            else
            {
                throw new Exception("Invalid file type. Must be .mid, .chart, or .q");
            }
        }
        private void ValidateMidi()
        {
            var errors = _midiFile!.GetErrorListAsString();
            var warnings = _midiFile.GetWarningListAsString();

            if (!string.IsNullOrEmpty(errors))
                throw new MidiCompileException(errors);

            if (!string.IsNullOrEmpty(warnings))
            {
                Console.WriteLine("WARNINGS:");
                Console.WriteLine(warnings);
            }
        }
        private void BuildPakOutputs()
        {
            _mainPakPath = BuildPak(isExpertPlus: false, qbArray: _midQb!);

            if (Diffs != null)
            {
                _midiFile!.SetEmptyTracksToDiffZero(Diffs);
            }

            if (_midQbExpertPlus != null)
            {
                _expertPlusPakPath = BuildPak(isExpertPlus: true, qbArray: _midQbExpertPlus);
            }

            _doubleKick = _midiFile!.DoubleKick;
        }
        private sealed class PakBuildScope
        {
            public required string SongName { get; init; }
            public required string SaveName { get; init; }
            public required string SongFolder { get; init; }
            public required string QbSave { get; init; }
        }
        private PakBuildScope CreateScope(bool isExpertPlus)
        {
            string scopedSongName = isExpertPlus ? EffectiveSongName + "X" : EffectiveSongName;

            string saveName = Path.Combine(_savePath, $"{scopedSongName}_{_gameConsole}");
            string pakFolder = _gameConsole == CONSOLE_PS2 ? "data\\songs" : "songs";
            string songFolder = Path.Combine(saveName, pakFolder);
            string qbSave = Path.Combine(songFolder, scopedSongName + $".mid.qb{_consoleExt}");

            return new PakBuildScope
            {
                SongName = scopedSongName,
                SaveName = saveName,
                SongFolder = songFolder,
                QbSave = qbSave
            };
        }
        private string BuildPak(bool isExpertPlus, byte[] qbArray)
        {
            var scope = CreateScope(isExpertPlus);

            Directory.CreateDirectory(scope.SongFolder);

            // Save MIDI QB
            File.WriteAllBytes(scope.QbSave, qbArray);

            WriteSongScriptsIfNeeded(scope);
            WriteGh5WorExtrasIfNeeded(scope);
            CompileSkaIfNeeded(scope);
            WriteQsIfNeeded(scope);

            return CompilePakAndCleanup(scope);
        }
        private void WriteSongScriptsIfNeeded(PakBuildScope scope)
        {
            if (_gameConsole == CONSOLE_PS2)
                return;

            bool isNew = _game == GAME_GH5 || _game == GAME_GHWOR;
            string scriptName = isNew ? ".perf.xml" : "_song_scripts";
            string songScriptsSave =
                Path.Combine(scope.SongFolder, scope.SongName + $"{scriptName}.qb{_consoleExt}");

            byte[]? songScriptsQb = _midiFile!.MakeSongScripts();
            if (songScriptsQb != null)
            {
                File.WriteAllBytes(songScriptsSave, songScriptsQb);
            }
        }
        private void WriteGh5WorExtrasIfNeeded(PakBuildScope scope)
        {
            if (_game != GAME_GHWOR && _game != GAME_GH5)
                return;

            var noteBytes = _midiFile.MakeGh5Notes();
            var perfBytes = _midiFile.MakeGh5Perf();

            string noteSave = Path.Combine(scope.SongFolder, scope.SongName + ".note" + _consoleExt);
            string perfSave = Path.Combine(scope.SongFolder, scope.SongName + ".perf" + _consoleExt);

            File.WriteAllBytes(noteSave, noteBytes);
            File.WriteAllBytes(perfSave, perfBytes);
        }
        private void CompileSkaIfNeeded(PakBuildScope scope)
        {
            if (!Directory.Exists(SkaPath))
                return;

            var skaCompiler = new SkaCompiler(
                savePath: _savePath,
                saveName: scope.SaveName,
                songFolder: scope.SongFolder,
                songName: scope.SongName,
                game: _game,
                gameConsole: _gameConsole,
                consoleExt: _consoleExt,
                midiFile: _midiFile!,
                skaPath: SkaPath,
                skaSource: SkaSource,
                gender: Gender,
                isSteven: IsSteven
            );

            skaCompiler.Compile();
        }
        private void WriteQsIfNeeded(PakBuildScope scope)
        {
            if (_midiFile == null || _midiFile.QsList.Count == 0)
                return;

            QsWriter.Write(
                qsList: _midiFile.QsList,
                songFolder: scope.SongFolder,
                songName: scope.SongName,
                game: _game,
                consoleExt: _consoleExt
            );
        }
        private string CompilePakAndCleanup(PakBuildScope scope)
        {
            string? assetContext = _game == GAME_GHWOR ? scope.SongName : null;

            var pakCompiler = new PakCompiler(
                game: _game,
                console: _gameConsole,
                assetContext: assetContext
            );

            var (pakData, pabData, qsStrings) = pakCompiler.CompilePAK(scope.SaveName);

            string songPrefix = _gameConsole == CONSOLE_PS2 ? "" : "_song";
            string pakSave = Path.Combine(_savePath, scope.SongName + $"{songPrefix}.pak{_consoleExt}");

            File.WriteAllBytes(pakSave, pakData);

            Directory.Delete(scope.SaveName, true);

            return pakSave;
        }
    }
}
