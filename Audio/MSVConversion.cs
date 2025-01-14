using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using static GH_Toolkit_Core.Methods.GlobalVariables;
using static GH_Toolkit_Core.Audio.FSB;
using static GH_Toolkit_Core.Audio.AudioConstants;

namespace GH_Toolkit_Core.Audio
{
    public partial class MSV
    {
        public async Task ConvertToWav(string inputPath, string outputPath, int sampleRate = 33075)
        {
            if (!File.Exists(inputPath))
            {
                inputPath = BlankWav;
            }
                
            var settings = FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, true, options => options
                    .WithAudioCodec(AudioCodec.PcmS16le)
                    .WithAudioSamplingRate(sampleRate) // Set the sample rate to variable
                    .WithCustomArgument("-ac 2") // Force 2 Channels (Stereo)
                    .WithoutMetadata()// Remove metadata
                );
            await settings.ProcessAsynchronously();
        }

        public async Task MakePreviewPs2(string[] paths, string outputPath, decimal startTime = 0, decimal trimDuration = 30, decimal fadeIn = 1, decimal fadeOut = 1, decimal volume = -7, int sampleRate = 33075)
        {
            var fsb = new FSB();
            string trimFilter = GetMixFilter(startTime, trimDuration, fadeIn, fadeOut, volume);
            await fsb.MixFiles(paths, outputPath, trimFilter, "mixout", WAV, sampleRate);
        }

    }
}
