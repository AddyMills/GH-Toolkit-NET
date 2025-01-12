using NAudio.Wave;
using GH_Toolkit_Core.Methods;
using System.Text;
using static GH_Toolkit_Core.Methods.GlobalVariables;
using System.Runtime.InteropServices;

namespace GH_Toolkit_Core.Audio
{
    public partial class MSV
    {
        private static readonly byte[] MSVp = { (byte)'M', (byte)'S', (byte)'V', (byte)'p' };
        private static readonly int BUFFER_SIZE = 128 * 28;

        private static double _s_1 = 0.0;
        private static double _s_2 = 0.0;
        private static double pack_s_1 = 0.0;
        private static double pack_s_2 = 0.0;
        private static readonly double[][] f =
        [
            [0.0, 0.0],
            [60.0 / 64.0, 0.0],
            [115.0 / 64.0, -52.0 / 64.0],
            [98.0 / 64.0, -55.0 / 64.0],
            [122.0 / 64.0, -60.0 / 64.0]
        ];

        public MSV()
        {
        }
        public string[] SplitStereoToMono(MemoryStream audio, string toSave)
        {
            // This method assumes it's fed a stereo stream. Will cause issues if it's not.
            // This method does not check if the stream is stereo or not. It assumes it is.
            // Same with it being 16-bit PCM

            byte[] bytes = audio.ToArray();
            byte[] left = new byte[bytes.Length / 2];
            byte[] right = new byte[bytes.Length / 2];
            for (int i = 0; i < bytes.Length; i += 4)
            {
                left[i / 2] = bytes[i];
                left[i / 2 + 1] = bytes[i + 1];
                right[i / 2] = bytes[i + 2];
                right[i / 2 + 1] = bytes[i + 3];
            }
            string leftPath = toSave + "_1.wav";
            string rightPath = toSave + "_2.wav";
            File.WriteAllBytes(leftPath, left);
            File.WriteAllBytes(rightPath, right);

            return [leftPath, rightPath];
        }
        public string[]? getWavStreams(string filePath, int sampleRate = 33075)
        {

            WaveFormat targetFormat = new WaveFormat(sampleRate, 16, 2); // Target format: 33075 Hz (or what is passed in), 16 bits, Stereo

            using (var resampledStream = new MemoryStream())
            using (var wavReader = new WaveFileReader(filePath))
            {
                var waveFormat = wavReader.WaveFormat;
                var channels = waveFormat.Channels;
                string folderPath = Path.GetDirectoryName(filePath);
                string fileNoExt = Path.GetFileNameWithoutExtension(filePath);
                string fullPath = Path.Combine(folderPath, fileNoExt);
                // Assuming the stereo file has 1 or 2 channels
                if (channels != 1 && channels != 2)
                {
                    throw new Exception("Input file does not have 1 or 2 channels");
                }

                IWaveProvider waveProvider = wavReader;
                // Check if the WAV file does not meet the expected format, sample rate, and bit depth
                if (wavReader.WaveFormat.SampleRate != sampleRate || wavReader.WaveFormat.BitsPerSample != 16 || wavReader.WaveFormat.Channels != 2)
                {
                    // Resample the audio stream to the target format using MediaFoundationResampler
                    waveProvider = new MediaFoundationResampler(wavReader, targetFormat)
                    {
                        ResamplerQuality = 60 // Set the resampler quality (optional)
                    };
                }

                // Directly write from waveProvider to MemoryStream
                WaveFileWriter.WriteWavFileToStream(resampledStream, waveProvider);
                //File.WriteAllBytes(Path.Combine(folderPath, $"{fileNoExt}_test.wav"), resampledStream.ToArray());
                

                resampledStream.Position = 0; // Reset the stream position to the beginning
                using (var wav2 = new WaveFileReader(resampledStream))
                {
                    var dataPosition = resampledStream.Length - wav2.Length;
                    // Put all bytes from resampledStream starting at dataPosition until the end into a new byte array
                    byte[] data = new byte[resampledStream.Length - dataPosition];
                    resampledStream.Position = dataPosition;
                    resampledStream.Read(data, 0, data.Length);
                    // Overwrite the resampledStream with the data array
                    resampledStream.SetLength(0);
                    resampledStream.Write(data, 0, data.Length);
                    resampledStream.Position = 0;

                }
                if (resampledStream.Length % 4 != 0)
                {
                    // Pad the stream with zeroes to ensure the stream length is a multiple of 4
                    resampledStream.SetLength(resampledStream.Length + (4 - (resampledStream.Length % 4)));
                }
                string[] streams = SplitStereoToMono(resampledStream, fullPath);
                return streams;
            }
        }
        public void makeMSV(string filePath, string songName = "")
        {
            // Code converted from MaxKiller/GameZelda's MSV encoder
            var _rw = new ReadWrite("big");
            var _waveread = new ReadWrite("little");
            var msvTarget = new WaveFormat(33075, 16, 1);

            double[] dSamples = new double[28];
            int predictNr = 0;
            int shiftFactor = 0;
            short[] fourBit = new short[28];
            int flags = 0;

            string folderName = Path.GetDirectoryName(filePath);
            string fileNoExt = Path.GetFileNameWithoutExtension(filePath);

            using (var rawStream = new MemoryStream(File.ReadAllBytes(filePath)))
            using (var wavStream = new RawSourceWaveStream(rawStream, msvTarget))
            using (var msvStream = new MemoryStream())
            {
                int bytesPerSample = wavStream.WaveFormat.BitsPerSample / 8;
                // Check the above code is good (since it'll be a while since we tested this next time this is run)
                var sampleLen = (int)(wavStream.Length / bytesPerSample);
                var size = (int)(sampleLen / 28);
                if (sampleLen % 28 != 0)
                {
                    size++;
                }
                string wavSave = Path.Combine(folderName, $"{fileNoExt}_test.wav");
                // wavStream.WriteToFile(wavSave);
                string filename = songName == "" ? fileNoExt : $"{songName}_{fileNoExt}";
                byte[] msvArray;
                msvStream.Write(MSVp, 0, 4); // ID ("MSVp");
                _rw.WriteUInt32(msvStream, 0x20); // Version
                _rw.WriteUInt32(msvStream, 0x00); // Reserved

                _rw.WriteUInt32(msvStream, (uint)(16 * (size))); // Data size
                _rw.WriteUInt32(msvStream, (uint)wavStream.WaveFormat.SampleRate); // Sample rate

                for (int i = 0; i < 3; i++)
                {
                    _rw.WriteUInt32(msvStream, 0); // Reserved
                }

                if (filename.Length > 0x10)
                {
                    filename = filename.Substring(0, 0x10);
                }
                else
                {
                    filename = filename.PadRight(0x10, '\0');
                }
                byte[] fileByte = Encoding.UTF8.GetBytes(filename);
                msvStream.Write(fileByte, 0, 0x10);

                for (int i = 0; i < 4; i++)
                {
                    _rw.WriteUInt32(msvStream, 0); // Reserved
                }

                using (BinaryWriter writer = new BinaryWriter(msvStream))
                {
                    while (sampleLen > 0)
                    {
                        size = (sampleLen >= BUFFER_SIZE) ? BUFFER_SIZE : sampleLen;
                        short[] wave = new short[size];
                        for (int i = 0; i < size; i++)
                        {
                            wave[i] = _waveread.ReadInt16(rawStream);
                        }

                        // Ensure 'wave' is large enough to include padding if necessary
                        int requiredSize = size + (28 - (size % 28)) % 28; // This adds padding space only if needed
                        if (wave.Length < requiredSize)
                        {
                            Array.Resize(ref wave, requiredSize);
                        }
                        int unkA = size / 28;
                        if (size % 28 != 0)
                        {
                            for (int j = size % 28; j < 28; j++)
                            {
                                wave[28 * unkA + j] = 0;
                            }
                            unkA++;
                        }

                        for (int i = 0; i < unkA; i++)
                        {
                            // In C#, directly access the array instead of using pointers
                            short[] ptr = new short[28];
                            Array.Copy(wave, i * 28, ptr, 0, 28);
                            FindPredict(ptr, dSamples, ref predictNr, ref shiftFactor);
                            Pack(dSamples, fourBit, predictNr, shiftFactor);
                            int d = (predictNr << 4) | shiftFactor;
                            writer.Write((byte)d);
                            writer.Write((byte)flags);

                            for (int k = 0; k < 28; k += 2)
                            {
                                d = (((fourBit[k + 1] >> 8) & 0xf0) | ((fourBit[k] >> 12) & 0xf));
                                writer.Write((byte)d);
                            }

                            sampleLen -= 28;
                            if (sampleLen < 28)
                                flags = 1;
                        }
                    }
                }
                msvArray = msvStream.ToArray();
                string msvSave = Path.Combine(folderName, $"{fileNoExt}.msvs");
                File.WriteAllBytes(msvSave, msvArray);
            }
        }
        public static void FindPredict(short[] samples, double[] dSamples, ref int predictNr, ref int shiftFactor)
        {
            int i, j;
            double[,] buffer = new double[28, 5];
            double min = 1e10;
            double[] max = new double[5];
            double ds;
            int min2;
            int shiftMask;
            double s_0;
            double s_1 = _s_1;
            double s_2 = _s_2;

            for (i = 0; i < 5; i++)
            {
                max[i] = 0.0;
                s_1 = _s_1;
                s_2 = _s_2;
                for (j = 0; j < 28; j++)
                {
                    s_0 = samples[j]; // s[t-0]
                    if (s_0 > 30719.0)
                        s_0 = 30719.0;
                    else if (s_0 < -30720.0)
                        s_0 = -30720.0;
                    ds = s_0 + s_1 * f[i][0] + s_2 * f[i][1];
                    buffer[j, i] = ds;
                    if (Math.Abs(ds) > max[i])
                        max[i] = Math.Abs(ds);
                    s_2 = s_1; // new s[t-2]
                    s_1 = s_0; // new s[t-1]
                }

                if (max[i] < min)
                {
                    min = max[i];
                    predictNr = i;
                }
                if (min <= 7)
                {
                    predictNr = 0;
                    break;
                }
            }

            // Store s[t-2] and s[t-1] in static fields
            _s_1 = s_1;
            _s_2 = s_2;

            for (i = 0; i < 28; i++)
                dSamples[i] = buffer[i, predictNr];

            min2 = (int)min;
            shiftMask = 0x4000;
            shiftFactor = 0;

            while (shiftFactor < 12)
            {
                if ((shiftMask & (min2 + (shiftMask >> 3))) != 0)
                    break;
                shiftFactor++;
                shiftMask >>= 1;
            }
        }
        public static void Pack(double[] dSamples, short[] fourBit, int predictNr, int shiftFactor)
        {
            double ds;
            int di;
            double s_0;

            for (int i = 0; i < 28; i++)
            {
                s_0 = dSamples[i] + pack_s_1 * f[predictNr][0] + pack_s_2 * f[predictNr][1];
                ds = s_0 * (1 << shiftFactor);

                di = (int)(((int)ds + 0x800) & 0xfffff000);

                if (di > 32767)
                    di = 32767;
                else if (di < -32768)
                    di = -32768;

                fourBit[i] = (short)di;

                di = di >> shiftFactor;
                pack_s_2 = pack_s_1;
                pack_s_1 = di - s_0;
            }
        }
        public void CombineMSVStreams(string gtr, string rhy, string back, string output = "", bool coop = false)
        {
            var coopAdd = coop ? "_coop" : "";
            var gtr1 = Path.Combine(Path.GetDirectoryName(gtr), $"gtr{coopAdd}_1.msvs");
            var gtr2 = Path.Combine(Path.GetDirectoryName(gtr), $"gtr{coopAdd}_2.msvs");
            var rhy1 = Path.Combine(Path.GetDirectoryName(rhy), $"rhythm{coopAdd}_1.msvs");
            var rhy2 = Path.Combine(Path.GetDirectoryName(rhy), $"rhythm{coopAdd}_2.msvs");
            var back1 = Path.Combine(Path.GetDirectoryName(back), $"backing{coopAdd}_1.msvs");
            var back2 = Path.Combine(Path.GetDirectoryName(back), $"backing{coopAdd}_2.msvs");
            CombineMSVStreams(gtr1, gtr2, rhy1, rhy2, back1, back2, output);

        }
        public void CombineMSVStreams(string gtr1, string gtr2, string rhy1, string rhy2, string back1, string back2, string output = "")
        {
            // Define the chunk size
            const int chunkSize = 0x20000; // 131072 bytes

            // Set output to a default value if it's empty
            if (output == "")
            {
                output = Path.Combine(Path.GetDirectoryName(gtr1), "combined.msvs");
            }

            // Open all source files for reading
            using (var fsGtr1 = new FileStream(gtr1, FileMode.Open, FileAccess.Read))
            using (var fsGtr2 = new FileStream(gtr2, FileMode.Open, FileAccess.Read))
            using (var fsRhy1 = new FileStream(rhy1, FileMode.Open, FileAccess.Read))
            using (var fsRhy2 = new FileStream(rhy2, FileMode.Open, FileAccess.Read))
            using (var fsBack1 = new FileStream(back1, FileMode.Open, FileAccess.Read))
            using (var fsBack2 = new FileStream(back2, FileMode.Open, FileAccess.Read))
            using (var fsOutput = new FileStream(output, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[chunkSize];
                bool filesHaveData = true;

                while (filesHaveData)
                {
                    filesHaveData = false;

                    // Interleave the files with padding if necessary
                    filesHaveData |= InterleaveMSVChunk(fsGtr1, fsOutput, buffer);
                    filesHaveData |= InterleaveMSVChunk(fsGtr2, fsOutput, buffer);
                    filesHaveData |= InterleaveMSVChunk(fsBack1, fsOutput, buffer);
                    filesHaveData |= InterleaveMSVChunk(fsBack2, fsOutput, buffer);
                    filesHaveData |= InterleaveMSVChunk(fsRhy1, fsOutput, buffer);
                    filesHaveData |= InterleaveMSVChunk(fsRhy2, fsOutput, buffer);
                }
            }

            // Delete all input files
            File.Delete(gtr1);
            File.Delete(gtr2);
            File.Delete(rhy1);
            File.Delete(rhy2);
            File.Delete(back1);
            File.Delete(back2);


        }
        private bool InterleaveMSVChunk(FileStream fsInput, FileStream fsOutput, byte[] buffer)
        {
            int bytesRead = fsInput.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                if (bytesRead < buffer.Length)
                {
                    // If the chunk is not full, fill the rest with zeros
                    Array.Clear(buffer, bytesRead, buffer.Length - bytesRead);
                }
                fsOutput.Write(buffer, 0, buffer.Length); // Always write a full chunk
                return true;
            }
            return false;
        }

        public async Task makePs2Audio(string filePath, string songName = "") // Always assumes 16-bit PCM. The file should be sent to the check method first before this one.
        {
            string extension = Path.GetExtension(filePath);
            string[] streams;

            switch (extension.ToLower())
            {
                case ".wav":
                    streams = getWavStreams(filePath);
                    break;
                default:
                    throw new Exception("Invalid file type.");
            }
            makeMSV(streams[0], songName);
            makeMSV(streams[1], songName);
            File.Delete(streams[0]);
            File.Delete(streams[1]);
        }

        private void MakeAllAudioEqualLength(string[] audioFiles)
        {
            int longestWavSize = 0;
            int largestFileSize = 0;
            int largestAudioSize = 0;
            foreach (var audio in audioFiles)
            {
                var audioBytes = File.ReadAllBytes(audio);
                using (var reader = new MemoryStream(audioBytes))
                {
                    if (reader.Length > longestWavSize)
                    {
                        longestWavSize = (int)reader.Length;
                    }
                    reader.Position = 4;
                    byte[] sizeBytes = new byte[4];
                    reader.Read(sizeBytes, 0, 4);
                    var fileSize = BitConverter.ToInt32(sizeBytes, 0);
                    if (fileSize > largestFileSize)
                    {
                        largestFileSize = fileSize;
                    }
                    reader.Position = 74;
                    byte[] fileBytes = new byte[4];
                    reader.Read(fileBytes, 0, 4);
                    var audioSize = BitConverter.ToInt32(fileBytes, 0);
                    if (audioSize > largestAudioSize)
                    {
                        largestAudioSize = audioSize;
                    }
                }
            }
            foreach (var audio in audioFiles)
            {
                var audioBytes = File.ReadAllBytes(audio);
                using (var reader = new MemoryStream(audioBytes))
                {
                    if (reader.Length < longestWavSize)
                    {
                        byte[] newAudio = new byte[longestWavSize];
                        reader.Read(newAudio, 0, (int)reader.Length);
                        using (var newAudioReader = new MemoryStream(newAudio))
                        {
                            newAudioReader.Position = 4;
                            byte[] sizeBytes = BitConverter.GetBytes(largestFileSize);
                            newAudioReader.Write(sizeBytes, 0, 4);
                            newAudioReader.Position = 74;
                            byte[] audioSizeBytes = BitConverter.GetBytes(largestAudioSize);
                            newAudioReader.Write(audioSizeBytes, 0, 4);
                            File.WriteAllBytes(audio, newAudio);
                        }
                        
                    }
                }
            }
        }

        public async Task CreatePs2Msv(string gtrAudio, string rhythmAudio, string backingAudio, string gtrCoopAudio = "", string rhythmCoopAudio = "", string backingCoopAudio = "", string? output = null, int sampleRate = 33075)
        {
            var exePath = ExeRootFolder;
            if (output == null)
            {
                if (File.Exists(gtrAudio))
                {
                    output = Path.Combine(Path.GetDirectoryName(gtrAudio), "output.msv");
                }
                else if (File.Exists(rhythmAudio))
                {
                    output = Path.Combine(Path.GetDirectoryName(rhythmAudio), "output.msv");
                }
                else if (File.Exists(backingAudio))
                {
                    output = Path.Combine(Path.GetDirectoryName(backingAudio), "output.msv");
                }
                else
                {
                    throw new Exception("No valid audio files found.");
                }
            }

            var outputFolder = Path.GetDirectoryName(output);

            var gtrOut = Path.Combine(outputFolder, "gtr.wav");
            var rhythmOut = Path.Combine(outputFolder, "rhythm.wav");
            var backingOut = Path.Combine(outputFolder, "backing.wav");

            Task gtrStem = ConvertToWav(gtrAudio, gtrOut, sampleRate);
            Task rhythmStem = ConvertToWav(rhythmAudio, rhythmOut, sampleRate);
            Task backingStem = ConvertToWav(backingAudio, backingOut, sampleRate);

            var allTasks = new Task[] { gtrStem, rhythmStem, backingStem };
            var allStems = new string[] { gtrOut, rhythmOut, backingOut };

            var gtrCoopOut = Path.Combine(outputFolder, "gtr_coop.wav");
            var rhythmCoopOut = Path.Combine(outputFolder, "rhythm_coop.wav");
            var backingCoopOut = Path.Combine(outputFolder, "backing_coop.wav");
            Task? gtrCoopStem = null;
            Task? rhythmCoopStem = null;
            Task? backingCoopStem = null;
            var coopTracks = new string[] { gtrCoopAudio, rhythmCoopAudio, backingCoopAudio };
            string[]? coopStems = null;
            var coopTracksExist = AnyFileExists(coopTracks);

            if (coopTracksExist)
            {
                gtrCoopStem = ConvertToWav(gtrCoopAudio, gtrCoopOut, sampleRate);
                rhythmCoopStem = ConvertToWav(rhythmCoopAudio, rhythmCoopOut, sampleRate);
                backingCoopStem = ConvertToWav(backingCoopAudio, backingCoopOut, sampleRate);

                allTasks = [gtrStem, rhythmStem, backingStem, gtrCoopStem, rhythmCoopStem, backingCoopStem];
                coopStems = [gtrCoopOut, rhythmCoopOut, backingCoopOut];
                allStems = allStems.Concat(coopStems).ToArray();
            }

            await Task.WhenAll(allTasks);

            MakeAllAudioEqualLength(allStems);


            gtrStem = makePs2Audio(gtrOut);
            rhythmStem = makePs2Audio(rhythmOut);
            backingStem = makePs2Audio(backingOut);

            allTasks = [gtrStem, rhythmStem, backingStem];

            if (coopTracksExist)
            {
                gtrCoopStem = makePs2Audio(gtrCoopOut);
                rhythmCoopStem = makePs2Audio(rhythmCoopOut);
                backingCoopStem = makePs2Audio(backingCoopOut);

                allTasks = [gtrStem, rhythmStem, backingStem, gtrCoopStem, rhythmCoopStem, backingCoopStem];
            }

            await Task.WhenAll(allTasks);

            CombineMSVStreams(gtrOut, rhythmOut, backingOut, output, false);
            if (coopTracksExist)
            {
                var coopOutput = Path.Combine(outputFolder, "output_coop.msv");
                CombineMSVStreams(gtrCoopOut, rhythmCoopOut, backingCoopOut, coopOutput, true);
            }
        }


        public static bool AnyFileExists(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
