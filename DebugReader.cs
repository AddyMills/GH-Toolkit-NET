using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core
{
    public class DebugReader
    {
        public static Dictionary<uint, string> ChecksumDbg { get; private set; }

        static DebugReader()
        {
            ChecksumDbg = ReadDebug();
        }
        static Dictionary<uint, string> ReadDebug()
        {
            var funcDict = new Dictionary<uint, string>();
            var rootFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var dbgPath = Path.Combine(rootFolder, "debug.txt");

            try
            {
                var textLines = File.ReadAllLines(dbgPath);

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading or processing the file: {ex.Message}");
                // Handle or log the exception as needed.
            }

            return funcDict;
        }

        public static object DbgCheck(uint toCheck)
        {
            if (DebugReader.ChecksumDbg.TryGetValue(toCheck, out var checksum))
            {
                return checksum;
            }
            else
            {
                return "0x"+toCheck.ToString("X");
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
