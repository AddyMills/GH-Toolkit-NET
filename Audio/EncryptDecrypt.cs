using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.Checksum.CRC;

namespace GH_Toolkit_Core.Audio
{
    public class EncryptDecrypt
    {
        // All of the following keys are used for the XOR operation with GH3 and GHA files.
        private static byte[] Fsb3Key = System.Text.Encoding.UTF8.GetBytes("5atu6w4zaw");
        private static readonly byte[] FSB3 = { (byte)'F', (byte)'S', (byte)'B', (byte)'3' };
        private static readonly byte[] FSB4 = { (byte)'F', (byte)'S', (byte)'B', (byte)'4' };

        /// <summary>
        /// Removes the first character from the file name if it starts with "adlc".
        /// </summary>
        /// <param name="fileName">The file name to be renamed.</param>
        /// <returns>The renamed file name.</returns>
        public static string FileRenamer(string fileName)
        {
            if (fileName.StartsWith("adlc"))
            {
                // Remove the first character
                fileName = fileName.Substring(1);
            }
            return fileName;
        }

        /// <summary>
        /// Flips the bits of the given audio.
        /// </summary>
        /// <param name="audio">The audio to flip the bits of.</param>
        /// <returns>The audio with flipped bits.</returns>
        public static byte[] FlipBits(byte[] audio)
        {
            byte[] result = new byte[audio.Length];
            for (int i = 0; i < audio.Length; i++)
            {
                result[i] = BinaryLookup.binaryReverse[audio[i]];
            }
            return result;
        }

        /// <summary>
        /// Performs the XOR operation between the audio and the key.
        /// </summary>
        /// <param name="audio">The audio to perform the XOR operation on.</param>
        /// <param name="key">The key to use for the XOR operation.</param>
        /// <returns>The result of the XOR operation.</returns>
        public static byte[] XorProcess(byte[] audio, byte[] key)
        {
            // Calculate the number of repetitions needed to match or exceed the length of the audio.
            int repetitions = 1 + (audio.Length / key.Length);

            // Create an array to hold the extended key.
            byte[] extendedKey = new byte[audio.Length];

            // Fill the extendedKey array with repeated copies of the key.
            for (int i = 0; i < repetitions; i++)
            {
                Array.Copy(key, 0, extendedKey, i * key.Length, Math.Min(key.Length, extendedKey.Length - (i * key.Length)));
            }

            // Create an array to hold the result of the XOR operation.
            byte[] result = new byte[audio.Length];

            // Perform the XOR operation between each byte of the audio and the extended key.
            for (int i = 0; i < audio.Length; i++)
            {
                result[i] = (byte)(audio[i] ^ extendedKey[i]);
            }

            return result;
        }

        /// <summary>
        /// Generates an FSB encryption key based on the provided string.
        /// </summary>
        /// <param name="toGen">The string used to generate the key.</param>
        /// <returns>The generated FSB key.</returns>
        public static byte[] GenerateFsbKey(string toGen)
        {
            uint xor = 0xffffffff;
            string encStr = "";
            const int cycle = 32;
            List<byte> key = new List<byte>();

            for (int i = 0; i < cycle; i++)
            {
                char ch = toGen[i % toGen.Length];
                uint crc = QBKeyUInt(new string(ch, 1));
                xor ^= crc;

                int index = (int)(xor % toGen.Length);
                encStr += toGen[index];
            }

            for (int i = 0; i < cycle - 1; i++)
            {
                char ch = encStr[i];
                uint crc = QBKeyUInt(new string(ch, 1));
                xor ^= crc;

                int c = i & 0x03;
                xor >>= c;

                uint z = 0; // Set to 0
                for (int x = 0; x < 32 - c; x++)
                {
                    z += (uint)(1 << x);
                }

                xor &= z;

                byte checkByte = (byte)(xor & 0xFF); // Equivalent to Python's hex(xor)[-2:],16

                if (checkByte == 0)
                {
                    break;
                }

                key.Add(checkByte);
            }

            return key.ToArray();
        }

        /// <summary>
        /// Decrypts the given audio using the provided key.
        /// </summary>
        /// <param name="audio">The audio to decrypt.</param>
        /// <param name="key">The key to use for decryption.</param>
        /// <returns>The decrypted audio.</returns>
        public static byte[] DecryptFsb4(byte[] audio, byte[] key)
        {
            var decrypted = FlipBits(audio);
            decrypted = XorProcess(decrypted, key);

            return decrypted;
        }

        /// <summary>
        /// Decrypts the given audio using the default Fsb3 key.
        /// </summary>
        /// <param name="audio">The audio to decrypt.</param>
        /// <returns>The decrypted audio.</returns>
        public static byte[] DecryptFsb3(byte[] audio)
        {
            var decrypted = XorProcess(audio, Fsb3Key);
            decrypted = FlipBits(decrypted);
            return decrypted;
        }

        /// <summary>
        /// Decrypts the given audio file.
        /// </summary>
        /// <param name="audio">The audio file to decrypt.</param>
        /// <param name="filename">The name of the audio file.</param>
        /// <returns>The decrypted audio.</returns>
        /// <exception cref="NotImplementedException">Thrown when the file type is not supported.</exception>
        public static byte[] DecryptFile(byte[] audio, string filename = "")
        {
            byte[] crypted = DecryptFsb3(audio[0..4]);
            if (crypted.SequenceEqual(FSB3))
            {
                crypted = DecryptFsb3(audio);
            }
            else
            {
                // Remove the extension and convert to lowercase. Sometimes there are two extensions which this hopefully covers.
                string noExt = FileRenamer(Path.GetFileNameWithoutExtension(filename).ToLower()).Replace(".fsb", "", StringComparison.CurrentCultureIgnoreCase);
                byte[] key = GenerateFsbKey(noExt);
                crypted = DecryptFsb4(audio[0..4], key);
                if (crypted.SequenceEqual(FSB4))
                {
                    crypted = DecryptFsb4(audio, key);
                }
                else
                {
                    /*crypted = DecryptWor(audio);
                    if (crypted == null)
                    {
                        Console.WriteLine($"Could not decrypt {filename}. Skipping...");
                        // Or throw an exception, or handle the error in some other way
                    }*/
                    throw new NotImplementedException("This file type is not supported yet.");
                }
            }
            return crypted;
        }

        /// <summary>
        /// Decrypts the audio file from the specified file path.
        /// </summary>
        /// <param name="filePath">The path of the audio file.</param>
        /// <returns>The decrypted audio.</returns>
        public static byte[] DecryptFromFilePath(string filePath)
        {
            var audio = File.ReadAllBytes(filePath);
            return DecryptFile(audio, filePath);
        }
    }
}
