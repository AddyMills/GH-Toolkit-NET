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
        public static readonly object ChecksumDbgLock = new object();
        public static readonly object QsDbgLock = new object();
        public static Dictionary<uint, string> ChecksumDbg { get; private set; }
        public static Dictionary<uint, string> QsDbg { get; private set; }
        public static Dictionary<uint, string> QsDbgF { get; private set; }
        public static Dictionary<uint, string> QsDbgG { get; private set; }
        public static Dictionary<uint, string> QsDbgI { get; private set; }
        public static Dictionary<uint, string> QsDbgS { get; private set; }
        public static Dictionary<string, uint> Ps2PakDbg { get; private set; }

        private static StreamWriter _qbUserWriter;
        private static StreamWriter _qsUserWriter;

        public enum QsDict
        {
            En = 0,
            Fr = 1,
            De = 2,
            It = 3,
            Es = 4
        }

        static DebugReader()
        {
            ChecksumDbg = ReadQBDebug();
            QsDbg = ReadQSDebug();
            QsDbgF = ReadQSDebug("_f");
            QsDbgG = ReadQSDebug("_g");
            QsDbgI = ReadQSDebug("_i");
            QsDbgS = ReadQSDebug("_s");
            Ps2PakDbg = ReadPs2PakDbg();
            InitializeWriters();
        }

        private static void InitializeWriters()
        {
            var rootFolder = ExeRootFolder;
            var qbDebugFolder = Path.Combine(rootFolder, "QBDebug");
            Directory.CreateDirectory(qbDebugFolder); // Ensures directory exists

            var userDbgPath = Path.Combine(qbDebugFolder, "keys_user.txt");
            var qsUserDbgPath = Path.Combine(qbDebugFolder, "keys_qs_user.txt");

            // Initialize StreamWriters with append mode and AutoFlush
            _qbUserWriter = new StreamWriter(userDbgPath, true, Encoding.UTF8) { AutoFlush = true };
            _qsUserWriter = new StreamWriter(qsUserDbgPath, true, Encoding.UTF8) { AutoFlush = true };
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

            //var compressedPath = Path.Combine(rootFolder, "QBDebug", "keys.dbg");
            var dbgPath = Path.Combine(rootFolder, "QBDebug", "keys.txt");
            var userDbgPath = Path.Combine(rootFolder, "QBDebug", "keys_user.txt");
            /*if (Path.Exists(compressedPath))
            {
                DecompressToFile(compressedPath, dbgPath);
            }*/

            try
            {
                var textLines = File.ReadAllLines(dbgPath);
                if (Path.Exists(userDbgPath))
                {
                    var userTextLines = File.ReadAllLines(userDbgPath);
                    textLines = textLines.Concat(userTextLines).ToArray();
                }
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

            return funcDict;
        }
        static void AddQbKeyToUser(string key, string value)
        {
            lock (ChecksumDbgLock)
            {
                _qbUserWriter.WriteLine($"{key}\t{value}");
            }
        }
        public static void AddQbKeyToUser(uint key, string value)
        {
            AddQbKeyToUser(key.ToString("x8"), value);
        }

        static void AddQsKeyToUser(string key, string value)
        {
            lock (QsDbgLock)
            {
                _qsUserWriter.WriteLine($"{key}\t{value}");
            }
        }
        public static void AddQsKeyToUser(uint key, string value)
        {
            AddQsKeyToUser(key.ToString("x8"), value);
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
        static Dictionary<uint, string> ReadQSDebug(string language = "")
        {
            var funcDict = new Dictionary<uint, string>();
            var rootFolder = ExeRootFolder;

            //var compressedPath = Path.Combine(rootFolder, "QBDebug", "keys_qs.dbg");
            var dbgPath = Path.Combine(rootFolder, "QBDebug", $"keys_qs{language}.txt");
            var userDbgPath = Path.Combine(rootFolder, "QBDebug", $"keys_qs_user{language}.txt");
            /*if (Path.Exists(compressedPath))
            {
                DecompressQsToFile(compressedPath, dbgPath);
            }*/
            try
            {
                var textLines = File.ReadAllLines(dbgPath, Encoding.BigEndianUnicode);
                var textTest = File.ReadAllText(dbgPath, Encoding.BigEndianUnicode);
                if (Path.Exists(userDbgPath))
                {
                    var userTextLines = File.ReadAllLines(userDbgPath);
                    textLines = textLines.Concat(userTextLines).ToArray();
                }

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
        public static string QsDbgString(uint toCheck)
        {
            if (QsDbg.TryGetValue(toCheck, out var checksum))
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

        public static void DecompressQsToFile(string compressedFilePath, string outputFilePath)
        {
            using (FileStream compressedStream = new FileStream(compressedFilePath, FileMode.Open, FileAccess.Read))
            using (GZipStream decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(decompressionStream, Encoding.Unicode))
            using (StreamWriter writer = new StreamWriter(outputFilePath, false, Encoding.BigEndianUnicode))
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

        // Dispose pattern to clean up resources
        public static void Dispose()
        {
            _qbUserWriter?.Dispose();
            _qsUserWriter?.Dispose();
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
