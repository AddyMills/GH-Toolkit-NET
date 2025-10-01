using GH_Toolkit_Core.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * CRC
 * Contains all functions related to CRC generation for Neversoft games
 * The QBKeyGen function allows you to generate a QB key indefinitely until you type in -1
 * 
 * 
 * Author: AddyMills
 * 
 */

namespace GH_Toolkit_Core.Checksum
{
    public class CRC
    {
        public static Dictionary<uint, string> NewKeys { get; private set; } = new Dictionary<uint, string>();
        private static readonly uint[] CRC32Table = {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,
            0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
            0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
            0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
            0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
            0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
            0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
            0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
            0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
            0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
            0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
            0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,
            0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,
            0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
            0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
            0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
            0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
            0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
            0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
            0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
            0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
            0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
            0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
            0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
            0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
            0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
            0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
            0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
            0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
            0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
            0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
            0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
            0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,
            0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
            0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
            0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
            0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
            0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
            0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
            0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
            0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
            0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
            0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
            0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
        };

        public static string QBKeyQs(string text, bool addToDict = true)
        {
            if (text.StartsWith("0x") && text.Length <= 10)
            {
                return text;
            }
            byte[] textBytes = Encoding.Unicode.GetBytes(text);

            string hexString = GenQBKey(textBytes, out uint checksumInt);
            if (hexString == "0xffffffff")
            {
                // Special case. Although an empty string would result in -1
                // Neversoft had it set to 0
                hexString = "0x00000000";
                checksumInt = 0;
            }
            // Use a lock to ensure thread safety when accessing the dictionary
            lock (DebugReader.QsDbgLock) // Add a static object in DebugReader for locking
            {
                if (addToDict && !DebugReader.QsDbg.ContainsKey(checksumInt))
                {
                    DebugReader.QsDbg.Add(checksumInt, text);
                    DebugReader.AddQsKeyToUser(checksumInt, text);
                }
            }

            return hexString;
        }
        private static byte[] QBKeyBytes(string text)
        {
            text = text.ToLower().Replace("/", "\\");
            return Encoding.UTF8.GetBytes(text);
        }
        public static string QBKey(string text, bool addToDict = true)
        {
            if (text.StartsWith("0x") && text.Length <= 10)
            {
                return text;
            }
            byte[] textBytes = QBKeyBytes(text);

            string hexString = GenQBKey(textBytes, out uint checksumInt);

            
            // Use a lock to ensure thread safety when accessing the dictionary
            lock (DebugReader.ChecksumDbgLock) // Add a static object in DebugReader for locking
            {
                if (addToDict && !DebugReader.ChecksumDbg.ContainsKey(checksumInt))
                {
                    DebugReader.ChecksumDbg.Add(checksumInt, text);
                    DebugReader.AddQbKeyToUser(checksumInt, text);
                }
            }

            return hexString;
        }
        public static byte[] MakeHexBytes(string hexString)
        {
            // Remove all whitespace, and 
            hexString = hexString.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            // split the string into a byte array
            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
        public static string QBKeyFromHexString(string hexString)
        {
            byte[] bytes = MakeHexBytes(hexString);
            return GenQBKey(bytes, out uint checksumInt);
        }
        /// <summary>
        /// Attempts to brute-force by removing bytes from the input until GenQBKey produces the target checksum.
        /// </summary>
        /// <param name="original">Original input bytes.</param>
        /// <param name="targetChecksum">Checksum string you want to match.</param>
        /// <param name="genQBKey">Function that computes checksum string from a byte[] (your GenQBKey wrapper).</param>
        /// <param name="maxRemovals">Maximum number of bytes to remove in combination (1..maxRemovals will be tried).</param>
        /// <param name="maxAttempts">Optional hard limit on how many candidate arrays to test (avoid infinite/huge runs).</param>
        /// <param name="progressCallback">Optional callback invoked periodically with number of attempts completed.</param>
        /// <returns>
        /// Tuple: (ResultingBytes, RemovedIndices). If no solution found, ResultingBytes is null.
        /// RemovedIndices is the indices in the original array that were removed (ascending).
        /// </returns>
        public static (byte[] Result, int[] RemovedIndices) BruteForceRemoveBytes(
            byte[] original,
            string targetChecksum,
            Func<byte[], string> genQBKey,
            int maxRemovals = 4,
            long maxAttempts = long.MaxValue,
            Action<long> progressCallback = null)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (genQBKey == null) throw new ArgumentNullException(nameof(genQBKey));
            if (maxRemovals < 1) throw new ArgumentOutOfRangeException(nameof(maxRemovals));

            int n = original.Length;

            // Quick direct check
            string originalChecksum = genQBKey(original);
            if (string.Equals(originalChecksum, targetChecksum, StringComparison.Ordinal))
                return (original, Array.Empty<int>());

            long attempts = 0;
            // For k = number of bytes to remove
            for (int k = 1; k <= Math.Min(maxRemovals, n); k++)
            {
                // initialize the first combination: [0,1,2,...,k-1] (these are indices to remove)
                int[] comb = new int[k];
                for (int i = 0; i < k; i++) comb[i] = i;

                bool done = false;
                while (!done)
                {
                    // Build candidate by skipping indices in comb (comb is sorted ascending)
                    byte[] candidate = new byte[n - k];
                    int dest = 0;
                    int nextRemovePos = 0;
                    int nextRemoveIdx = (k > 0) ? comb[0] : -1;

                    for (int src = 0; src < n; src++)
                    {
                        if (src == nextRemoveIdx)
                        {
                            // skip this byte
                            nextRemovePos++;
                            nextRemoveIdx = (nextRemovePos < k) ? comb[nextRemovePos] : -1;
                        }
                        else
                        {
                            candidate[dest++] = original[src];
                        }
                    }

                    // call the checksum function
                    attempts++;
                    if (attempts % 1000 == 0)
                    {
                        progressCallback?.Invoke(attempts);
                    }

                    string cs = genQBKey(candidate);
                    if (string.Equals(cs, targetChecksum, StringComparison.Ordinal))
                    {
                        // Found it — return candidate and removed indices
                        return (candidate, (int[])comb.Clone());
                    }

                    if (attempts >= maxAttempts)
                    {
                        return (null, null); // hit the attempt limit
                    }

                    // generate next combination (lexicographic)
                    int iPos = k - 1;
                    while (iPos >= 0 && comb[iPos] == iPos + n - k) iPos--;
                    if (iPos < 0)
                    {
                        done = true; // finished all combinations of this k
                    }
                    else
                    {
                        comb[iPos]++;
                        for (int j = iPos + 1; j < k; j++)
                            comb[j] = comb[j - 1] + 1;
                    }
                } // while combinations for k
            } // for k

            // nothing found
            return (null, null);
        }

        public static (byte[] Result, int[] RemovedIndices) BruteForce(
            byte[] original,
            string targetChecksum,
            int maxRemovals = 4,
            long maxAttempts = long.MaxValue,
            Action<long> progressCallback = null)
        {
            return BruteForceRemoveBytes(original, targetChecksum, GenQBKey, maxRemovals, maxAttempts, progressCallback);
        }
        public static string GenQBKey(byte[] textBytes)
        {
            uint checksumInt = GenQBKeyUInt(textBytes);
            string result = checksumInt.ToString("x8");

            // Pad to 8 characters
            result = result.PadLeft(8, '0');
            result = "0x" + result;
            return result;
        }
        public static string GenQBKey(byte[] textBytes, out uint checksumInt)
        {
            checksumInt = GenQBKeyUInt(textBytes);
            string result = checksumInt.ToString("x8");

            // Pad to 8 characters
            result = result.PadLeft(8, '0');
            result = "0x" + result;
            return result;
        }
        
        public static uint GenQBKeyUInt(byte[] textBytes)
        {
            uint crc = 0xffffffff;

            foreach (var b in textBytes)
            {
                uint numA = (crc ^ b) & 0xFF;
                crc = CRC32Table[numA] ^ crc >> 8 & 0x00ffffff;
            }

            uint finalCRC = ~crc;
            long value = -finalCRC - 1;
            return (uint)value & 0xffffffff;
        }

        public static uint QBKeyUInt(string textBytes)
        {
            if (textBytes.StartsWith("0x") && textBytes.Length <= 10)
            {
                return ConvertHexToUInt(textBytes);
            }
            return GenQBKeyUInt(QBKeyBytes(textBytes));
        }
        public static uint QSKeyUInt(string textBytes)
        {
            if (textBytes == string.Empty)
            {
                return 0;
            }
            return GenQBKeyUInt(Encoding.Unicode.GetBytes(textBytes));
        }
        private static uint ConvertHexToUInt(string hexString)
        {
            // Ensure the string starts with '0x' and is not longer than 10 characters.
            if (hexString.StartsWith("0x") && hexString.Length <= 10)
            {
                return Convert.ToUInt32(hexString, 16);
            }
            throw new ArgumentException("Invalid hex string: " + hexString);
        }

        internal static void DebugTest()
        {
            string[] lines = File.ReadAllLines("D:\\Visual Studio\\Repos\\GH-Toolkit\\debug.txt");

            foreach (string line in lines)
            {
                string[] pairs = line.Split(" ");
                string dbg_hex = pairs[0];
                string orig_string = pairs[1].Replace("\"", "");

                string new_string = QBKey(orig_string);

                int old_hex = Convert.ToInt32(dbg_hex.Substring(2), 16);
                int new_hex = Convert.ToInt32(new_string, 16);

                if (old_hex != new_hex)
                {
                    Console.WriteLine(string.Format("{0} checksum {1} does not match {2}", orig_string, dbg_hex, new_string));
                }
            }
        }

        public static void QBKeyGen()
        {
            while (true)
            {

                Console.Write("Please enter a string: ");
                string userInput = Console.ReadLine();
                string qbKey = QBKey(userInput);
                if (userInput == "-1")
                {
                    break;
                }
                Console.WriteLine("The QBKey of your string is: " + qbKey);
            }
        }
    }
}
