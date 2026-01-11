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
    public class DebugReader : IDisposable
    {
        private static readonly Lazy<DebugData> _data = new(() => new DebugData());

        public static readonly object ChecksumDbgLock = new();
        public static readonly object QsDbgLock = new();

        public static Dictionary<uint, string> ChecksumDbg => _data.Value.ChecksumDbg;
        public static Dictionary<uint, string> QsDbg => _data.Value.QsDbg;
        public static Dictionary<uint, string> QsDbgF => _data.Value.QsDbgF;
        public static Dictionary<uint, string> QsDbgG => _data.Value.QsDbgG;
        public static Dictionary<uint, string> QsDbgI => _data.Value.QsDbgI;
        public static Dictionary<uint, string> QsDbgS => _data.Value.QsDbgS;
        public static Dictionary<string, uint> Ps2PakDbg => _data.Value.Ps2PakDbg;

        public enum QsDict
        {
            En = 0,
            Fr = 1,
            De = 2,
            It = 3,
            Es = 4
        }
        private class DebugData : IDisposable
        {
            public Dictionary<uint, string> ChecksumDbg { get; }
            public Dictionary<uint, string> QsDbg { get; }
            public Dictionary<uint, string> QsDbgF { get; }
            public Dictionary<uint, string> QsDbgG { get; }
            public Dictionary<uint, string> QsDbgI { get; }
            public Dictionary<uint, string> QsDbgS { get; }
            public Dictionary<string, uint> Ps2PakDbg { get; }

            public StreamWriter QbUserWriter { get; }
            public StreamWriter QsUserWriter { get; }

            public DebugData()
            {
                // Load dictionaries
                ChecksumDbg = ReadQBDebug();
                QsDbg = ReadQSDebug();
                QsDbgF = ReadQSDebug("_f");
                QsDbgG = ReadQSDebug("_g");
                QsDbgI = ReadQSDebug("_i");
                QsDbgS = ReadQSDebug("_s");
                Ps2PakDbg = ReadPs2PakDbg();

                // Prepare user writers
                var folder = Path.Combine(ExeRootFolder, "QBDebug");
                Directory.CreateDirectory(folder);

                QbUserWriter = new StreamWriter(Path.Combine(folder, "keys_user.txt"), true, Encoding.UTF8) { AutoFlush = true };
                QsUserWriter = new StreamWriter(Path.Combine(folder, "keys_qs_user.txt"), true, Encoding.UTF8) { AutoFlush = true };
            }

            public void Dispose()
            {
                QbUserWriter?.Dispose();
                QsUserWriter?.Dispose();
            }
        }

        // --------------------------
        // File Readers
        // --------------------------

        private static Dictionary<uint, string> ReadQBDebug()
        {
            try
            {
                string folder = Path.Combine(ExeRootFolder, "QBDebug");
                string dbg = Path.Combine(folder, "keys.txt");
                string user = Path.Combine(folder, "keys_user.txt");

                var lines = File.Exists(dbg)
                    ? File.ReadAllLines(dbg)
                    : Array.Empty<string>();

                if (File.Exists(user))
                    lines = lines.Concat(File.ReadAllLines(user)).ToArray();

                var dict = new Dictionary<uint, string>();
                foreach (var line in lines)
                {
                    var parts = line.Split('\t');
                    if (parts.Length != 2) continue;

                    if (uint.TryParse(parts[0].Replace("0x", string.Empty), NumberStyles.HexNumber, null, out var key))
                        dict[key] = parts[1];
                }

                return dict;
            }
            catch
            {
                return new Dictionary<uint, string>();
            }
        }

        private static Dictionary<uint, string> ReadQSDebug(string suffix = "")
        {
            try
            {
                string folder = Path.Combine(ExeRootFolder, "QBDebug");
                string dbg = Path.Combine(folder, $"keys_qs{suffix}.txt");
                string user = Path.Combine(folder, $"keys_qs_user{suffix}.txt");

                var lines = File.Exists(dbg)
                    ? File.ReadAllLines(dbg, Encoding.BigEndianUnicode)
                    : Array.Empty<string>();

                if (File.Exists(user))
                    lines = lines.Concat(File.ReadAllLines(user)).ToArray();

                var dict = new Dictionary<uint, string>();
                foreach (var line in lines)
                {
                    var parts = line.Split('\t');
                    if (parts.Length != 2) continue;

                    if (uint.TryParse(parts[0].Replace("0x", string.Empty), NumberStyles.HexNumber, null, out var key))
                        dict[key] = parts[1];
                }

                return dict;
            }
            catch
            {
                return new Dictionary<uint, string>();
            }
        }

        private static Dictionary<string, uint> ReadPs2PakDbg()
        {
            var dict = new Dictionary<string, uint>();
            string folder = Path.Combine(ExeRootFolder, "QBDebug");

            string compressed = Path.Combine(folder, "PS2Pak.dbg");
            string output = Path.Combine(folder, "ps2pak.txt");

            try
            {
                if (File.Exists(compressed))
                    DecompressToFile(compressed, output);

                if (!File.Exists(output))
                    return dict;

                foreach (var line in File.ReadAllLines(output))
                {
                    var parts = line.Split('\t');
                    if (parts.Length != 2) continue;

                    if (uint.TryParse(parts[0].Replace("0x", string.Empty), NumberStyles.HexNumber, null, out var val))
                        dict[parts[1]] = val;
                }
            }
            catch { }

            try { if (File.Exists(output)) File.Delete(output); } catch { }

            return dict;
        }

        // --------------------------
        // Write helpers
        // --------------------------

        private static void AddQbKeyToUser(string key, string value)
        {
            lock (ChecksumDbgLock)
                _data.Value.QbUserWriter.WriteLine($"{key}\t{value}");
        }

        public static void AddQbKeyToUser(uint key, string value) =>
            AddQbKeyToUser(key.ToString("x8"), value);

        private static void AddQsKeyToUser(string key, string value)
        {
            lock (QsDbgLock)
                _data.Value.QsUserWriter.WriteLine($"{key}\t{value}");
        }

        public static void AddQsKeyToUser(uint key, string value) =>
            AddQsKeyToUser(key.ToString("x8"), value);

        // --------------------------
        // Lookup methods
        // --------------------------

        public static string DbgString(uint toCheck) =>
            ChecksumDbg.TryGetValue(toCheck, out var v) ? v : "0x" + toCheck.ToString("x8");

        public static string QsDbgString(uint toCheck) =>
            QsDbg.TryGetValue(toCheck, out var v) ? v : "0x" + toCheck.ToString("x8");

        public static string Ps2PakString(string input) =>
            Ps2PakDbg.TryGetValue(input, out var v) ? "0x" + v.ToString("x8") : input;

        // --------------------------
        // Decompression helpers
        // --------------------------

        public static void DecompressToFile(string compressedFilePath, string outputFilePath)
        {
            using var input = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8);

            writer.Write(reader.ReadToEnd());
        }

        // --------------------------
        // Misc
        // --------------------------

        public static void Dispose()
        {
            if (_data.IsValueCreated)
                _data.Value.Dispose();
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }


        public static void AddToQsDictTemp(string filePath, QsDict dictMod)
        {
            try
            {
                var textLines = File.ReadAllLines(filePath, Encoding.BigEndianUnicode);
                Dictionary<uint, string> funcDict;
                switch (dictMod)
                {
                    case QsDict.En:
                        funcDict = QsDbg;
                        break;
                    case QsDict.Fr:
                        funcDict = QsDbgF;
                        break;
                    case QsDict.De:
                        funcDict = QsDbgG;
                        break;
                    case QsDict.It:
                        funcDict = QsDbgI;
                        break;
                    case QsDict.Es:
                        funcDict = QsDbgS;
                        break;
                    default:
                        throw new ArgumentException("Invalid dictMod value. Must be between 0 and 4.");
                }
                foreach (var line in textLines)
                {
                    var newLine = line.TrimEnd('\n').Split(new[] { ' ' }, 2);

                    if (newLine.Length != 2) continue;

                    try
                    {
                        var key = Convert.ToUInt32(newLine[0], 16);
                        var value = newLine[1].Replace("\"", "");
                        funcDict[key] = value;
                    }
                    catch
                    {
                        // If an exception occurs, ignore and continue processing the next line.
                    }
                }
            }
            catch
            {

            }
        }
        public static string? GetQsKeyFromDict(uint key, QsDict dictMod = QsDict.En)
        {
            Dictionary<uint, string> funcDict;
            switch (dictMod)
            {
                case QsDict.En:
                    funcDict = QsDbg;
                    break;
                case QsDict.Fr:
                    funcDict = QsDbgF;
                    break;
                case QsDict.De:
                    funcDict = QsDbgG;
                    break;
                case QsDict.It:
                    funcDict = QsDbgI;
                    break;
                case QsDict.Es:
                    funcDict = QsDbgS;
                    break;
                default:
                    throw new ArgumentException("Invalid dictMod value. Must be between 0 and 4.");
            }
            if (funcDict.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }
        public static Dictionary<uint, string> TranslateDictionary(Dictionary<uint, string> originalDict, QsDict dictMod = QsDict.En)
        {
            var newDict = new Dictionary<uint, string>();
            foreach (var kvp in originalDict)
            {
                var translatedValue = GetQsKeyFromDict(kvp.Key, dictMod);
                if (translatedValue != null)
                {
                    newDict[kvp.Key] = translatedValue;
                }
                else
                {
                    newDict[kvp.Key] = kvp.Value; // Fallback to original value if translation not found
                }
            }
            return newDict;
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
