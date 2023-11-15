using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Debug
{
    public class DebugReader
    {
        public static Dictionary<uint, string> ChecksumDbg { get; private set; }

        static DebugReader()
        {
            ChecksumDbg = ReadQBDebug();
        }
        static Dictionary<uint, string> ReadQBDebug()
        {
            var funcDict = new Dictionary<uint, string>();
            var rootFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            var compressedPath = Path.Combine(rootFolder, "QBDebug", "keys.dbg");
            var dbgPath = Path.Combine(rootFolder,"QBDebug", "keys.txt");
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
    }
}
