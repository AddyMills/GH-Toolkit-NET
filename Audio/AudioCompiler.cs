using ICSharpCode.SharpZipLib.Checksum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QBConstants;

namespace GH_Toolkit_Core.Audio
{
    public class AudioCompiler
    {
        // Global Settings
        private string Checksum {  get; set; }
        private string CompilePath { get; set; }
        private string SavePath { get
            {
                return Path.Combine(CompilePath, SaveFolderName);
            }
        } // Just so it doesn't save everything in the same folder.
        private string SaveFolderName { get; set; }
        private string CurrentGame {  get; set; }
        private decimal PreviewStart { get; set; }
        private decimal PreviewLength { get; set; }
        private decimal PreviewVolume { get; set; }
        private decimal FadeIn { get; set; }
        private decimal FadeOut { get; set; }
        private bool CustomPreview { get; set; }
        private bool CoopAudio { get; set; } // Only used for GH3/GHA to determine if co-op audio should be generated.
        // Drums Audio (GHWT+ only)
        private string KickPath { get; set; }
        private string SnarePath { get; set; }
        private string CymbalsPath { get; set; }
        private string TomsPath { get; set; }
        // Other Playable Audio (G/B used globally, vocals for GHWT+ only)
        private string GuitarPath { get; set; }
        private string RhythmPath { get; set; } // Mainly bass guitar. Also used for rhythm guitar if no co-op tracks present in GH3/GHA
        private string VocalsPath { get; set; }
        // Backing Audio
        private string[] BackingPaths { get; set; }
        private string CrowdPath { get; set; }
        private string PreviewPath { get; set; } // Used only when preview audio boolean is true. To insert a custom preview instead of generating it.
        // GH3/GHA Co-op Audio Paths
        private string GuitarCoopPath { get; set; }
        private string RhythmCoopPath { get; set; }
        private string[] BackingCoopPaths { get; set; }
        private string FsbOut {  get; set; }
        private string DatOut { get; set; }
        public AudioCompiler() 
        {  
        }
        public (string fsbout, string datout) getFsbDat()
        {
            return (FsbOut, DatOut);
        }
        public static AudioCompiler CreateGh3Compiler(
            string checksum,
            string compilePath,
            string saveFolder,
            string game,
            int previewStart,
            int previewLength,
            decimal previewVolume,
            decimal fadeIn,
            decimal fadeOut,
            bool customPreview,
            bool coopAudio,
            string guitarPath,
            string rhythmPath,
            string[] backingPaths,
            string crowdPath,
            string previewPath,
            string guitarCoopPath,
            string rhythmCoopPath,
            string[] backingCoopPaths)
        {
            decimal prevStart = previewStart / 1000m;
            decimal prevLength = previewLength / 1000m;
            var compiler = new AudioCompiler()
            {
                Checksum = checksum,
                CompilePath = compilePath,
                SaveFolderName = saveFolder,
                CurrentGame = game,
                PreviewStart = prevStart,
                PreviewLength = prevLength,
                PreviewVolume = previewVolume,
                FadeIn = fadeIn,
                FadeOut = fadeOut,
                CustomPreview = customPreview,
                CoopAudio = coopAudio,
                GuitarPath = guitarPath,
                RhythmPath = rhythmPath,
                BackingPaths = backingPaths,
                CrowdPath = crowdPath,
                PreviewPath = previewPath,
                GuitarCoopPath = guitarCoopPath,
                RhythmCoopPath = rhythmCoopPath,
                BackingCoopPaths = backingCoopPaths,
            };
            return compiler;
        }
        public async Task GH3AudioCompile()
        {
            string gtrOutput = Path.Combine(CompilePath, $"{Checksum}_guitar.mp3");
            string rhythmOutput = Path.Combine(CompilePath, $"{Checksum}_rhythm.mp3");
            string backingOutput = Path.Combine(CompilePath, $"{Checksum}_song.mp3");
            string coopGtrOutput = Path.Combine(CompilePath, $"{Checksum}_coop_guitar.mp3");
            string coopRhythmOutput = Path.Combine(CompilePath, $"{Checksum}_coop_rhythm.mp3");
            string coopBackingOutput = Path.Combine(CompilePath, $"{Checksum}_coop_song.mp3");
            string crowdOutput = Path.Combine(CompilePath, $"{Checksum}_crowd.mp3");
            string previewOutput = Path.Combine(CompilePath, $"{Checksum}_preview.mp3");
            string[] spFiles = { gtrOutput, rhythmOutput, backingOutput };
            string[] coopFiles = { coopGtrOutput, coopRhythmOutput, coopBackingOutput };
            var filesToProcess = new List<string>();
            filesToProcess.AddRange(spFiles);
            filesToProcess.AddRange(coopFiles);
            filesToProcess.Add(crowdOutput);
            filesToProcess.Add(previewOutput);
            string fsbOutput = Path.Combine(CompilePath, Checksum);
            try
            {
                Console.WriteLine("Compiling Audio...");

                FSB fsb = new FSB();

                Task gtrStem = fsb.ConvertToMp3(GuitarPath, gtrOutput);
                Task rhythmStem = fsb.ConvertToMp3(RhythmPath, rhythmOutput);
                Task backingStem = fsb.MixFiles(BackingPaths, backingOutput);

                var tasksToAwait = new List<Task> { gtrStem, rhythmStem, backingStem };
                if (CurrentGame == GAME_GHA && File.Exists(CrowdPath))
                {
                    Task crowdStem = fsb.ConvertToMp3(CrowdPath, crowdOutput);
                    tasksToAwait.Add(crowdStem);
                }
                if (CoopAudio)
                {
                    Task coopGtrStem = fsb.ConvertToMp3(GuitarCoopPath, coopGtrOutput);
                    Task coopRhythmStem = fsb.ConvertToMp3(RhythmCoopPath, coopRhythmOutput);
                    Task coopBackingStem = fsb.MixFiles(BackingCoopPaths, coopBackingOutput);
                    tasksToAwait.AddRange(new List<Task> { coopGtrStem, coopRhythmStem, coopBackingStem });
                }

                // Create the preview audio
                Task previewStem;
                if (CustomPreview && File.Exists(PreviewPath))
                {
                    previewStem = fsb.ConvertToMp3(PreviewPath, previewOutput);

                }
                else
                {
                    previewStem = fsb.MakePreview(spFiles, previewOutput, PreviewStart, PreviewLength, FadeIn, FadeOut, PreviewVolume);
                }
                tasksToAwait.Add(previewStem);

                // Await all started tasks. This ensures all conversions are completed before moving on.
                await Task.WhenAll(tasksToAwait.ToArray());

                Console.WriteLine("Combining Audio...");
                (FsbOut, DatOut) = fsb.CombineFSB3File(filesToProcess, fsbOutput);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                foreach (string file in filesToProcess)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        public async Task GH3AudioCompilePs2()
        {
            var msv = new MSV();
            string[] allPaths = BackingPaths.Concat([GuitarPath, RhythmPath]).ToArray();

            var previewSave = Path.Combine(CompilePath, "preview.wav");

            var audioTask = msv.CreatePs2Msv(GuitarPath, RhythmPath, BackingPaths, GuitarCoopPath, RhythmCoopPath, BackingCoopPaths, CompilePath, 33075);
            var previewTask = msv.MakePreviewPs2(allPaths, previewSave, PreviewStart, PreviewLength, FadeIn, FadeOut, PreviewVolume, 33075);

            await previewTask;

            await msv.CreatePs2Preview(previewSave);

            await audioTask;
        }
    }
}
