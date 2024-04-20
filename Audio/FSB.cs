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
using System.Globalization;


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
            catch (FileNotFoundException ex)
            {
                var outNoExt = Path.GetFileNameWithoutExtension(outputPath);
                var textAfterUnderscore = outNoExt.Split('_').Last();
                var properTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(textAfterUnderscore);
                Console.WriteLine($"{properTitle} track not found. Using blank audio.");
                File.Copy(audioPad48k128kbps, outputPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion:\n{ex.Message}");
                throw; // Rethrow the exception to handle it further up the call stack
            }
            
        }
        public async Task MakePreview(string[] paths, string outputPath, decimal startTime = 0, decimal trimDuration = 30, decimal fadeIn = 1, decimal fadeOut = 1, decimal volume = -7)
        {
            CultureInfo culture = new CultureInfo("en-US");
            string startTimeStr = startTime.ToString(culture);
            string trimDurationStr = trimDuration.ToString(culture);
            string fadeInStr = fadeIn.ToString(culture);
            var fadeOutCalc = startTime + trimDuration - 1;
            string fadeOutCalcStr = fadeOutCalc.ToString(culture);
            string fadeOutStr = fadeOut.ToString(culture);
            string volumeStr = volume.ToString(culture);

            string trimFilter = $"[mixout]atrim=start={startTimeStr}:duration={trimDurationStr},afade=t=in:st={startTimeStr}:d={fadeInStr},afade=t=out:st={fadeOutCalcStr}:d={fadeOutStr},volume={volumeStr}dB[final]";
            await MixFiles(paths, outputPath, trimFilter, "mixout");
        }
        public async Task MixFiles(string[] paths, string outputPath, string customArgument = "", string customPipe = "")
        {
            if (paths.Length == 0)
            {
                File.Copy(audioPad48k128kbps, outputPath, true);
            }
            else if (paths.Length == 1)
            {
                await ConvertToMp3(paths[0], outputPath); 
            }
            else
            {
                List<string> files = new List<string>();
                foreach (string path in paths)
                {
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Length > 384)
                    {
                        files.Add(path);
                    }
                }
                paths = files.ToArray();
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
        public (int, List<FileInfo>) GetPaddedAudio(IEnumerable<string> audioFiles)
        {
            long maxSize = 0;
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
            return (totalSize, filesThatExist);
        }
        public (string, string) CombineFSB3File(IEnumerable<string> audioFiles, string output)
        {

            string fsbOut = output + ".fsb";
            string datOut = output + ".dat";

            var (totalSize, filesThatExist) = GetPaddedAudio(audioFiles);

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
        public string[] CombineFSB4File(IEnumerable<string> drumFiles, IEnumerable<string> otherFiles, IEnumerable<string> backingFiles, IEnumerable<string> previewFile, string output, bool encrypt = false)
        {
            string drumOut = output + "_1";
            string otherOut = output + "_2";
            string backingOut = output + "_3";
            string previewOut = output + "_preview";

            var outList = new List<string>() { drumOut, otherOut, backingOut, previewOut };

            var fsbDict = new Dictionary<string, IEnumerable<string>>()
            {
                { drumOut, drumFiles },
                { otherOut, otherFiles },
                { backingOut, backingFiles },
                { previewOut, previewFile }
            };

            var allFiles = drumFiles.Concat(otherFiles).Concat(backingFiles);
            GetPaddedAudio(allFiles);

            List<string> processed = new List<string>();

            foreach (string outFile in outList)
            {
                string interleavedFile = outFile + "_interleaved.mp3";
                string fsbOut = outFile + ".fsb";
                var toInterleave = fsbDict[outFile].ToArray();
                InterleaveMp3Files(toInterleave, interleavedFile);
                FileInfo fileInfo = new FileInfo(interleavedFile);
                using (var fsb = new FileStream(fsbOut, FileMode.Create, FileAccess.Write))
                using (var fsbBytes = new MemoryStream())
                using (var mp3Bytes = new MemoryStream())
                {
                    int fileCount = 1;
                    fsbBytes.Write(Encoding.ASCII.GetBytes("FSB4"));
                    int dirLength = FILEENTRYLEN;
                    int fsbSize = (int)fileInfo.Length;

                    fsb_writer.WriteInt32(fsbBytes, fileCount);
                    fsb_writer.WriteInt32(fsbBytes, dirLength);

                    fsb_writer.WriteInt32(fsbBytes, fsbSize);
                    fsb_writer.WriteInt16(fsbBytes, 0); // Unknown flag, always 0
                    fsb_writer.WriteInt16(fsbBytes, 4); // FSB type flag, always 4 for FSB4
                    fsb_writer.WriteInt32(fsbBytes, 0); // Flags, always 0 for FSB4?
                    fsb_writer.WriteInt32(fsbBytes, 0); // NullA, always 0
                    fsb_writer.WriteInt32(fsbBytes, 0); // NullB, always 0

                    fsbBytes.Write(Encoding.ASCII.GetBytes("--Made By Addy--"));

                    FsbEntry(fileInfo, fsbBytes, mp3Bytes, true, (ushort)(toInterleave.Length * 2));
                    
                    fsbBytes.Write(mp3Bytes.ToArray());

                    var fsbData = fsbBytes.ToArray();

                    if (encrypt)
                    {
                        // Encrypt the FSB4 file
                    }
                    fsb.Write(fsbData);
                }
                processed.Add(fsbOut);
                File.Delete(interleavedFile);
            }
            return processed.ToArray();
        }
        public static void InterleaveMp3Files(string[] filePaths, string outputFileName)
        {
            const int frameSize = 384; // For now. I may add support for other frame sizes later.
            FileStream[] fileStreams = filePaths.Select(path => new FileStream(path, FileMode.Open, FileAccess.Read)).ToArray();
            byte[] buffer = new byte[frameSize];
            bool completed = false;

            try
            {
                using (FileStream output = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
                {
                    while (!completed)
                    {
                        completed = true;
                        foreach (var stream in fileStreams)
                        {
                            if (stream.Read(buffer, 0, frameSize) == frameSize)
                            {
                                output.Write(buffer, 0, frameSize);
                                completed = false;
                            }
                        }
                    }
                }
            }
            finally
            {
                foreach (var stream in fileStreams)
                {
                    stream.Close();
                }
            }
        }
        public void FsbEntry(FileInfo file, Stream fsb, Stream audioBytes, bool fsb4 = false, ushort channels = 2)
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
            int samplesLength = (int)file.Length / FRAMEBYTESIZE * FRAMESAMPLESIZE / (channels / 2);
            int loopStart = 0;
            int loopEnd = fsb4 ? samplesLength - 878 : samplesLength - 1;
            int mode = fsb4 ? 67109376 : 576;
            int sampleRate = 48000;
            (ushort volume, ushort priority) = (255, 128);
            ushort pan = 128;
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
