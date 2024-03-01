using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;

namespace GH_Toolkit_Core.Audio
{
    public class FSB
    {
        public async Task ConvertToMp3(string inputPath, string outputPath)
        {

            string arg_test = FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                .WithAudioBitrate(128) // Set the bitrate to 128kbps
                .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                .WithoutMetadata() // Remove metadata
                .WithCustomArgument("-ac 2") // Force 2 Channels
            )
            .Arguments;
            
            await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                .WithAudioBitrate(128) // Set the bitrate to 128kbps
                .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                .WithoutMetadata() // Remove metadata
                .WithCustomArgument("-ac 2") // Force 2 Channels
            )
            .ProcessAsynchronously();
        }
        public void MixFiles(string[] paths, string outputPath)
        {
            if (paths.Length < 2)
            {
              
            }

            // Build the FFmpeg arguments for mixing audio files.
            // We use the amix filter to mix audio inputs into a single output.
            var filterComplex = $"-filter_complex \"amix=inputs={paths.Length}:duration=longest\"";
            var inputs = string.Join(" ", paths.Select(p => $"-i \"{p}\""));

            // Assemble the full FFmpeg command
            var ffmpegArgs = $"{filterComplex}";

            try
            {
                // Execute the FFmpeg command
                string arg_test = FFMpegArguments
                    .FromFileInput(paths, true)
                    .OutputToFile(outputPath, false, options => options
                        .WithCustomArgument(ffmpegArgs)
                        .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                        .WithAudioBitrate(128) // Set the bitrate to 128kbps
                        .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                        .WithoutMetadata() // Remove metadata
                        .WithCustomArgument("-ac 2") // Force 2 Channels
                        ).Arguments;
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during mixing: {ex.Message}");
                throw; // Rethrow the exception to handle it further up the call stack
            }
        }
    }
}
