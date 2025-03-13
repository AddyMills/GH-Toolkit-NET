using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GH_Toolkit_Core.Methods.GlobalVariables;

namespace GH_Toolkit_Core.Debug
{
    public class DebugReader
    {
        public static readonly object ChecksumDbgLock = new object();
        public static Dictionary<uint, string> ChecksumDbg { get; private set; }
        public static Dictionary<string, uint> Ps2PakDbg { get; private set; }

        static DebugReader()
        {
            ChecksumDbg = ReadQBDebug();
            Ps2PakDbg = ReadPs2PakDbg();
        }
        static Dictionary<string, uint> ReadPs2PakDbg()
        {
            var funcDict = new Dictionary<string, uint>();
            var rootFolder = ExeRootFolder;
            var compressedPath = Path.Combine(rootFolder, "QBDebug", "PS2Pak.dbg");
            var dbgPath = Path.Combine(rootFolder, "QBDebug", "ps2pak.txt");
            if (Path.Exists(compressedPath))
            {
                DecompressToFile(compressedPath, dbgPath);
            }

            try
            {
                var textLines = File.ReadAllLines(dbgPath);

                foreach (var line in textLines)
                {
                    var newLine = line.TrimEnd('\n').Split(new[] { '\t' }, 2);

                    if (newLine.Length != 2) continue;

                    try
                    {
                        var key = newLine[1];//.Replace("\"", "");
                        var value = Convert.ToUInt32(newLine[0], 16);
                        funcDict[key] = value;
                    }
                    catch
                    {
                        // If an exception occurs, ignore and continue processing the next line.
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading or processing the file: {ex.Message}");
            }
            if (Path.Exists(dbgPath))
            {
                File.Delete(dbgPath);
            }
            return funcDict;
        }
        static Dictionary<uint, string> ReadQBDebug()
        {
            var funcDict = new Dictionary<uint, string>();
            var rootFolder = ExeRootFolder;

            var compressedPath = Path.Combine(rootFolder, "QBDebug", "keys.dbg");
            var dbgPath = Path.Combine(rootFolder, "QBDebug", "keys.txt");
            if (Path.Exists(compressedPath))
            {
                DecompressToFile(compressedPath, dbgPath);
            }

            try
            {
                var textLines = File.ReadAllLines(dbgPath);

                foreach (var line in textLines)
                {
                    var newLine = line.TrimEnd('\n').Split(new[] { '\t' }, 2);

                    if (newLine.Length != 2) continue;

                    try
                    {
                        var key = Convert.ToUInt32(newLine[0], 16);
                        var value = newLine[1];//.Replace("\"", "");
                        funcDict[key] = value;
                    }
                    catch
                    {
                        // If an exception occurs, ignore and continue processing the next line.
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading or processing the file: {ex.Message}");
            }
            if (Path.Exists(dbgPath))
            {
                File.Delete(dbgPath);
            }
            return funcDict;
        }
        public static string DebugCheck(Dictionary<uint, string> headers, uint check)
        {
            return headers.TryGetValue(check, out string? result) ? result : DbgString(check);
        }
        public static string DebugCheck(string checkString)
        {
            uint check = uint.Parse(checkString.Substring(2), NumberStyles.HexNumber);
            return DbgString(check);
        }
        public static string DbgString(uint toCheck)
        {
            if (ChecksumDbg.TryGetValue(toCheck, out var checksum))
            {
                return checksum;
            }
            else
            {
                return "0x" + toCheck.ToString("x8");
            }
        }
        public static string Ps2PakString(string toCheck)
        {
            if (Ps2PakDbg.TryGetValue(toCheck, out var checksum))
            {
                return "0x" + checksum.ToString("x8");
            }
            else
            {
                return toCheck;
            }
        }
        public static void DecompressToFile(string compressedFilePath, string outputFilePath)
        {
            using (FileStream compressedStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read))
            using (GZipStream decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(decompressionStream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }

        public static Dictionary<uint, string> MakeDictFromName(string name)
        {
            Dictionary<uint, string> headers;

            // Check if name contains "dlc"
            if (name.Contains("dlc"))
            {
                // Check if name starts with "adlc", "bdlc", or "cdlc" and remove the first letter if it does
                if (name.StartsWith("adlc") || name.StartsWith("bdlc") || name.StartsWith("cdlc"))
                {
                    headers = CreateCustomDict(name.Substring(1));
                }
                else
                {
                    // If name contains "dlc" but does not start with "adlc", "bdlc", or "cdlc", use name directly
                    headers = CreateCustomDict(name);
                }
            }
            // Check if name contains "dlx" where x is a number from 0 to 0xffffffff
            else if (Regex.IsMatch(name, @"dl(\d{1,10})") && long.TryParse(Regex.Match(name, @"dl(\d{1,10})").Groups[1].Value, out long number) && number <= 0xffffffff)
            {
                // Use just the number part in "name" for CreateCustomDict
                headers = CreateCustomDlcDict(number.ToString());
            }
            else
            {
                // If name does not contain "dlc", just use the original logic or handle accordingly
                headers = CreateCustomDict(name);
                if (name.StartsWith("a")) // Mainly for WTDE, may use a little more memory, but it's fine
                {
                    var otherHeaders = CreateCustomDict(name.Substring(1));
                    foreach (var header in otherHeaders)
                    {
                        headers.Add(header.Key, header.Value);
                    }
                }
            }

            return headers;
        }
        private static Dictionary<uint, string> CreateCustomDict(string name)
        {
            Dictionary<uint, string> headers = new Dictionary<uint, string>();
            if (string.IsNullOrEmpty(name))
            {
                headers.Clear();
            }
            else
            {
                headers = DebugHeaders.CreateHeaderDict(name);
            }
            return headers;
        }
        private static Dictionary<uint, string> CreateCustomDlcDict(string name)
        {
            Dictionary<uint, string> headers = new Dictionary<uint, string>();
            if (string.IsNullOrEmpty(name))
            {
                headers.Clear();
            }
            else
            {
                headers = DebugHeaders.CreateDlcDict(name);
            }
            return headers;
        }
    }
}
