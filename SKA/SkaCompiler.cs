using GH_Toolkit_Core.MIDI;
using System.Text.RegularExpressions;
using static GH_Toolkit_Core.QB.QBConstants;

namespace GH_Toolkit_Core.SKA
{
    public class SkaCompiler
    {
        private readonly string _savePath;
        private readonly string _saveName;
        private readonly string _songFolder;
        private readonly string _songName;
        private readonly string _game;
        private readonly string _gameConsole;
        private readonly string _consoleExt;

        private readonly SongQbFile _midiFile;
        private readonly string _skaPath;
        private readonly string _skaSource;
        private readonly string _gender;
        private readonly bool _isSteven;

        public SkaCompiler(
        string savePath,
        string saveName,
        string songFolder,
        string songName,
        string game,
        string gameConsole,
        string consoleExt,
        SongQbFile midiFile,
        string skaPath,
        string skaSource,
        string gender,
        bool isSteven)
        {
            _savePath = savePath;
            _saveName = saveName;
            _songFolder = songFolder;
            _songName = songName;
            _game = game;
            _gameConsole = gameConsole;
            _consoleExt = consoleExt;

            _midiFile = midiFile;
            _skaPath = skaPath;
            _skaSource = skaSource;
            _gender = gender;
            _isSteven = isSteven;
        }

        public void Compile()
        {
            float skaMultiplier = DetermineMultiplier();

            var skaFiles = GetSkaFileList();
            var readSkaFiles = LoadSkaFiles(skaFiles);

            bool ps2SkaProcessed = false;
            byte[]? skaScripts = null;

            foreach (var skaParsed in readSkaFiles)
            {
                var skaFileName = NormalizeSkaName(skaParsed.Key);

                bool isGuitarist = IsGuitaristSka(skaParsed.Key, skaFileName);
                string skaType = DetermineSkeletonType(isGuitarist);

                TryWriteConvertedSka(
                    skaParsed.Key,
                    skaFileName,
                    skaParsed.Value,
                    skaMultiplier,
                    skaType,
                    ref ps2SkaProcessed,
                    ref skaScripts
                );
            }

            WritePs2SkaScriptsIfNeeded(skaScripts);
        }

        private float DetermineMultiplier()
        {
            if (_gameConsole == CONSOLE_PS2)
            {
                switch (_skaSource)
                {
                    case GAME_GH3:
                        return _game switch
                        {
                            GAME_GH3 => 2.0f,
                            GAME_GHA => 1.0f,
                            _ => 1.0f
                        };

                    case GAME_GHA:
                        return 1.0f;

                    default:
                        return 2.0f;
                }
            }

            if (_game == _skaSource) return 1.0f;
            if (_game == GAME_GH3) return 0.5f;

            return _skaSource == GAME_GH3 ? 2.0f : 1.0f;
        }
        private List<string> GetSkaFileList()
        {
            var skaFiles = Directory.GetFiles(_skaPath).ToList();

            var skaFilesToCompress = Path.Combine(_skaPath, "Compress");
            if (Directory.Exists(skaFilesToCompress))
            {
                skaFiles.AddRange(Directory.GetFiles(skaFilesToCompress));
            }

            return skaFiles;
        }

        private Dictionary<string, SkaFile> LoadSkaFiles(List<string> skaFiles)
        {
            var readSkaFiles = new Dictionary<string, SkaFile>();

            foreach (var skaFile in skaFiles)
            {
                try
                {
                    var skaTest = new SkaFile(skaFile, "big");
                    readSkaFiles.Add(skaFile, skaTest);
                }
                catch (Exception ex)
                {
                    var fileSka = Path.GetFileNameWithoutExtension(skaFile);
                    Console.WriteLine($"{fileSka}: {ex.Message}\n");
                }
            }

            return readSkaFiles;
        }
        private static string NormalizeSkaName(string skaPath)
        {
            var skaFileName = Path.GetFileName(skaPath);

            if (skaFileName.ToLower().StartsWith("0x"))
                skaFileName = skaFileName.Substring(0, skaFileName.IndexOf('.'));

            return skaFileName;
        }
        private bool IsGuitaristSka(string skaFullPath, string skaFileName)
        {
            string skaPatternGuit = @"\d+b\.ska(\.xen)?$";
            bool isPatternMatch = Regex.IsMatch(skaFullPath, skaPatternGuit);
            return isPatternMatch || _midiFile.GtrSkaAnims.Contains(skaFileName);
        }
        private string DetermineSkeletonType(bool isGuitarist)
        {
            switch (_game)
            {
                case GAME_GH3:
                    if (isGuitarist && _gameConsole != CONSOLE_PS2)
                        return SKELETON_GH3_GUITARIST;

                    return _gameConsole == CONSOLE_PS2
                        ? SKELETON_GH3_SINGER_PS2
                        : SKELETON_GH3_SINGER;

                case GAME_GHA:
                    return isGuitarist
                        ? SKELETON_GH3_GUITARIST
                        : (_isSteven ? SKELETON_STEVE : SKELETON_GHA_SINGER);

                default:
                    return SKELETON_WT_ROCKER;
            }
        }
        private void TryWriteConvertedSka(
        string skaPath,
        string skaFileName,
        SkaFile skaFile,
        float skaMultiplier,
        string skaType,
        ref bool ps2SkaProcessed,
        ref byte[]? skaScripts)
        {
            byte[] convertedSka;
            string skaSave;

            try
            {
                if (_game == GAME_GH3 || _game == GAME_GHA)
                {
                    if (_gameConsole == CONSOLE_PS2)
                    {
                        if (!ps2SkaProcessed)
                        {
                            if (_gender.ToLower() != "none")
                            {
                                skaScripts = SongQbFile.MakePs2SkaScript(_gender, _songName);
                            }
                            ps2SkaProcessed = true;
                        }

                        if (_midiFile.GtrSkaAnims.Contains(skaFileName))
                            return;

                        convertedSka = skaFile.WritePs2StyleSka(skaMultiplier);

                        string skaFolderPs2 = Path.Combine(_savePath, "PS2 SKA Files");
                        Directory.CreateDirectory(skaFolderPs2);

                        skaSave = Path.Combine(
                            skaFolderPs2,
                            Path.GetFileName(skaPath).Replace(DOTXEN, DOTPS2)
                        );
                    }
                    else
                    {
                        convertedSka = skaFile.WriteGh3StyleSka(skaType, skaMultiplier);
                        skaSave = Path.Combine(_saveName, Path.GetFileName(skaPath));
                    }
                }
                else
                {
                    if (skaFile.IsSingleFrame)
                    {
                        if (_game != GAME_GHWT)
                        {
                            Console.WriteLine($"{Path.GetFileNameWithoutExtension(skaPath)}: Single frame SKA files not supported for {_game}.\n");
                            return;
                        }

                        convertedSka = File.ReadAllBytes(skaPath);
                    }
                    else
                    {
                        convertedSka = skaFile.WriteModernStyleSka(skaType, _game, skaMultiplier);
                    }

                    skaSave = Path.Combine(_saveName, Path.GetFileName(skaPath));
                }
            }
            catch
            {
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(skaPath)}: Could not convert ska file.\n");
                return;
            }

            if (convertedSka.Length > 0)
            {
                File.WriteAllBytes(skaSave, convertedSka);
            }
        }
        private void WritePs2SkaScriptsIfNeeded(byte[]? skaScripts)
        {
            if (skaScripts == null)
                return;

            string ps2SkaScriptSave = Path.Combine(_songFolder, _songName + $"_song_scripts.qb{_consoleExt}");
            File.WriteAllBytes(ps2SkaScriptSave, skaScripts);
        }
    }
}

