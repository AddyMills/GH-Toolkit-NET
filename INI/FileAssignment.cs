using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.INI
{
    public class FileAssignment
    {
        public List<string> BackingTracks { get; set; } = new List<string>();
        public string? Guitar { get; set; }
        public string? Rhythm { get; set; }
        public string? Bass { get; set; }
        public string? Crowd { get; set; }
        public string? Preview { get; set; }
        public string? KickDrum { get; set; }
        public string? SnareDrum { get; set; }
        public string? Toms { get; set; }
        public string? Cymbals { get; set; }
        public string? Vocals { get; set; }
        public bool RenderedPreview { get; set; }
        public string? MidiFile { get; set; }
        public string? PerfOverride { get; set; }
        public string? SongScripts { get; set; }
        public string? LipsyncFiles { get; set; }
        public string? SkaFiles { get; set; }
        public bool DoesAudioExist { get { return VerifyAudio(); } }
        public bool DoesMidiExist { get { return VerifyMidi(); } }
        public bool DoesPerfExist { get { return VerifyPerf(); } }
        public bool DoesSongScriptsExist { get { return VerifySongScripts(); } }
        public bool DoesLipsyncExist { get { return VerifyLipsync(); } }
        public bool DoesSkaExist { get { return VerifySka(); } }

        private bool VerifyAudio()
        {
            // Check if any backing track path exists
            if (BackingTracks.Any(track => Path.Exists(track)))
            {
                return true;
            }

            // Collect all individual paths into a list
            var paths = new[] { Guitar, Rhythm, Bass, Crowd, Preview, KickDrum, SnareDrum, Toms, Cymbals, Vocals };

            // Check if any of the paths exist
            return paths.Any(path => path != null && Path.Exists(path));
        }
        private bool VerifyMidi()
        {
            return MidiFile != null && File.Exists(MidiFile);
        }
        private bool VerifyPerf()
        {
            return PerfOverride != null && Directory.Exists(PerfOverride);
        }
        private bool VerifySongScripts()
        {
            return SongScripts != null && Directory.Exists(SongScripts);
        }
        private bool VerifyLipsync()
        {
            return LipsyncFiles != null && Directory.Exists(LipsyncFiles);
        }
        private bool VerifySka()
        {
            return SkaFiles != null && Directory.Exists(SkaFiles);
        }
    }
}
