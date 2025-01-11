using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using static GH_Toolkit_Core.Methods.GlobalVariables;

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


    }
}
