using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using GH_Toolkit_Core.Methods;
using GH_Toolkit_Core.Checksum;


namespace GH_Toolkit_Core.Audio
{
    public class FSB
    {
        private static string? rootFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string? audioPad48k128kbps = Path.Combine(rootFolder, "Audio\\Blank Audio\\48k128kbps.mp3");
        private ReadWrite fsb_writer = new ReadWrite("little");
        private ReadWrite dat_writer = new ReadWrite("big");

        private int FRAMEBYTESIZE = 384;
        private int FRAMESAMPLESIZE = 1152;
        private ushort FILEENTRYLEN = 80;
        private int FSBHEADERLEN = 24;

        public async Task ConvertToMp3(string inputPath, string outputPath)
        {

            /*string arg_test = FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                .WithAudioBitrate(128) // Set the bitrate to 128kbps
                .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                .WithoutMetadata() // Remove metadata
                .WithCustomArgument("-ac 2") // Force 2 Channels
            )
            .Arguments;*/
            try
            {
                await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, true, options => options
                    .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                    .WithAudioBitrate(128) // Set the bitrate to 128kbps
                    .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                    .WithoutMetadata() // Remove metadata
                    .WithCustomArgument("-ac 2 -write_xing 0 -id3v2_version 0") // Force 2 Channels
                )
                .ProcessAsynchronously();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
                throw; // Rethrow the exception to handle it further up the call stack
            }
            
        }
        public async Task MakePreview(string[] paths, string outputPath, decimal startTime = 0, decimal trimDuration = 30, decimal fadeIn = 1, decimal fadeOut = 1)
        {
            string trimFilter = $"[mixout]atrim=start={startTime}:duration={trimDuration},afade=t=in:st={startTime}:d={fadeIn},afade=t=out:st={startTime + trimDuration - 1}:d={fadeOut},volume=-7.0dB[final]";
            await MixFiles(paths, outputPath, trimFilter, "mixout");
        }
        public async Task MixFiles(string[] paths, string outputPath, string customArgument = "", string customPipe = "")
        {
            if (paths.Length < 2)
            {
                await ConvertToMp3(paths[0], outputPath); 
            }
            else
            {
                // Build the FFmpeg arguments for mixing audio files.
                // We use the amix filter to mix audio inputs into a single output.
                string audioStreams = string.Join("", paths.Select((item, index) => $"[{index}:0]"));
                string mixFilter = $"{audioStreams}amix=inputs={paths.Length}:duration=longest:dropout_transition=1:normalize=0";
                if (customArgument != "" && customPipe != "")
                {
                    mixFilter += $"[{customPipe}];";
                    mixFilter += customArgument;
                }
                else
                {
                    mixFilter += "[final]";
                }
                if (!mixFilter.EndsWith("[final]"))
                {
                    throw new Exception("Invalid custom argument. The custom argument must end with [final]");
                }

                // Assemble the full FFmpeg command
                var ffmpegArgs = $"-filter_complex \"{mixFilter}\" -map \"[final]\"";

                /*var strTest = FFMpegArguments
                        .FromFileInput(paths, true)
                        .OutputToFile(outputPath, true, options => options
                            .WithCustomArgument(ffmpegArgs)
                            .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                            .WithAudioBitrate(128) // Set the bitrate to 128kbps
                            .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                            .WithoutMetadata() // Remove metadata
                            .WithCustomArgument("-ac 2 -write_xing 0 -id3v2_version 0") // Force 2 Channels
                            ).Arguments;*/

                try
                {
                    // Execute the FFmpeg command
                    await FFMpegArguments
                        .FromFileInput(paths, true)
                        .OutputToFile(outputPath, true, options => options
                            .WithCustomArgument(ffmpegArgs)
                            .WithAudioCodec(AudioCodec.LibMp3Lame) // Set the audio codec to MP3
                            .WithAudioBitrate(128) // Set the bitrate to 128kbps
                            .WithAudioSamplingRate(48000) // Set the sample rate to 48kHz
                            .WithoutMetadata() // Remove metadata
                            .WithCustomArgument("-ac 2 -write_xing 0 -id3v2_version 0") // Force 2 Channels
                            ).ProcessAsynchronously();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during mixing: {ex.Message}");
                    throw; // Rethrow the exception to handle it further up the call stack
                }
            }
        }
        public void PadAudio(string inputPath, long paddedSize, string audioPad)
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException("The audio file does not exist.");
            }
            byte[] audio = File.ReadAllBytes(inputPath);
            int audioLen = audio.Length;
            byte[] paddingBytes = File.ReadAllBytes(audioPad);
            int paddingLen = paddingBytes.Length;
            byte[] paddedAudio = new byte[paddedSize];
            long repetitions = (paddedSize - audioLen) / paddingLen;
            Array.Copy(audio, paddedAudio, audioLen);
            for (int i = 0; i < repetitions; i++)
            {
                Array.Copy(paddingBytes, 0, paddedAudio, audioLen + (i * paddingLen), paddingLen);
            }
            /*
             * var folderPath = Path.GetDirectoryName(inputPath);
             * var testSavePath = Path.Combine(folderPath, "padded.mp3");
            */
            File.WriteAllBytes(inputPath, paddedAudio);
            //return audio;
        }
        public (string, string) CombineFSB3File(IEnumerable<string> audioFiles, string output)
        {
            long maxSize = 0;
            string fsbOut = output + ".fsb";
            string datOut = output + ".dat";
            foreach (var file in audioFiles)
            {
                // Create a new FileInfo object
                FileInfo fileInfo = new FileInfo(file);

                // Check if the file exists to avoid FileNotFoundException
                if (fileInfo.Exists)
                {
                    // Set the file size
                    maxSize = Math.Max(maxSize, fileInfo.Length);
                }
            }
            if (maxSize % 384 != 0)
            {
                throw new Exception("The input files are not CBR MP3 files.");
            }
            var filesThatExist = new List<FileInfo>();
            int totalSize = 0;
            foreach (var file in audioFiles)
            {
                // Create a new FileInfo object
                FileInfo fileInfo = new FileInfo(file);

                // Check if the file exists to avoid FileNotFoundException
                if (!fileInfo.Exists)
                {
                    continue;
                }
                // Set the file size
                maxSize = Math.Max(maxSize, fileInfo.Length);
                if (fileInfo.Length < maxSize && !fileInfo.FullName.Contains("_preview.mp3"))
                {
                    PadAudio(file, maxSize, audioPad48k128kbps);
                    fileInfo = new FileInfo(file);
                }
                filesThatExist.Add(fileInfo);
                totalSize += (int)fileInfo.Length;
            }

            using (var fsb = new FileStream(fsbOut, FileMode.Create, FileAccess.Write))
            using (var dat = new FileStream(datOut, FileMode.Create, FileAccess.Write))
            using (var fsbBytes = new MemoryStream())
            using (var mp3Bytes = new MemoryStream())
            {
                int fileCount = filesThatExist.Count();
                fsbBytes.Write(Encoding.ASCII.GetBytes("FSB3"));
                fsb_writer.WriteInt32(fsbBytes, fileCount);
                int dirLength = FILEENTRYLEN * fileCount + 8; //80 for each entry, 8 because of eight 00 bytes
                int fsbSize = dirLength + totalSize + FSBHEADERLEN;

                dat_writer.WriteInt32(dat, fileCount);
                dat_writer.WriteInt32(dat, fsbSize);

                fsb_writer.WriteInt32(fsbBytes, dirLength);
                fsb_writer.WriteInt32(fsbBytes, totalSize);
                fsb_writer.WriteInt16(fsbBytes, 1); // Unknown flag, always 1
                fsb_writer.WriteInt16(fsbBytes, 3); // FSB type flag, always 3 for FSB3
                fsb_writer.WriteInt32(fsbBytes, 0); // Flags, always 0 for FSB3?

                int fileIndex = 0;
                foreach (var file in filesThatExist)
                {
                    FsbEntry(file, fsbBytes, mp3Bytes);
                    string fileNoExt = Path.GetFileNameWithoutExtension(file.Name);
                    dat_writer.WriteUInt32(dat, CRC.QBKeyUInt(fileNoExt));
                    dat_writer.WriteInt32(dat, fileIndex);
                    dat_writer.WriteInt32(dat, 0);
                    dat_writer.WriteInt32(dat, 0);
                    dat_writer.WriteInt32(dat, 0);
                    fileIndex++;
                }
                fsb_writer.WriteInt32(fsbBytes, 0); // Unknown, always 0 for FSB3?
                fsb_writer.WriteInt32(fsbBytes, 0); // Unknown, always 0 for FSB3?
                
                fsbBytes.Write(mp3Bytes.ToArray());

                fsb.Write(EncryptDecrypt.EncryptFSB3(fsbBytes.ToArray()));
            }

            return (fsbOut, datOut);

        }
        public void FsbEntry(FileInfo file, Stream fsb, Stream audioBytes)
        {
            var fileName = file.Name;
            if (fileName.Length < 30)
            {
                fileName = fileName.PadRight(30, '\0');
            }
            else
            {
                fileName = fileName.Substring(0, 30);
            }
            int fileSize = (int)file.Length;
            int samplesLength = (int)file.Length / FRAMEBYTESIZE * FRAMESAMPLESIZE;
            int loopStart = 0;
            int loopEnd = samplesLength - 1;
            int mode = 576;
            int sampleRate = 48000;
            (ushort volume, ushort priority) = (255, 255);
            ushort pan = 128;
            ushort channels = 2;
            float minDistance = 1.0f;
            float maxDistance = 10000.0f;
            (int varFreq, ushort varVol, ushort varPan) = (0, 0, 0);

            fsb_writer.WriteUInt16(fsb, FILEENTRYLEN);
            fsb_writer.WriteNoFlipBytes(fsb, Encoding.ASCII.GetBytes(fileName));
            fsb_writer.WriteInt32(fsb, samplesLength);
            fsb_writer.WriteInt32(fsb, fileSize);
            fsb_writer.WriteInt32(fsb, loopStart);
            fsb_writer.WriteInt32(fsb, loopEnd);
            fsb_writer.WriteInt32(fsb, mode);
            fsb_writer.WriteInt32(fsb, sampleRate);
            fsb_writer.WriteUInt16(fsb, volume);
            fsb_writer.WriteUInt16(fsb, pan);
            fsb_writer.WriteUInt16(fsb, priority);
            fsb_writer.WriteUInt16(fsb, channels);
            fsb_writer.WriteFloat(fsb, minDistance);
            fsb_writer.WriteFloat(fsb, maxDistance);
            fsb_writer.WriteInt32(fsb, varFreq);
            fsb_writer.WriteUInt16(fsb, varVol);
            fsb_writer.WriteUInt16(fsb, varPan);

            byte[] audio = File.ReadAllBytes(file.FullName);
            audioBytes.Write(audio);
        }
    }
}
