using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.Methods.GlobalVariables;

/*
 * * This file is intended to be a collection of custom methods to read and create sections to be used in custom songs
 * * 
 * */

namespace GH_Toolkit_Core.MIDI
{
    public class SectionNames
    {
        public static Dictionary<string, string> SectionNamesDict { get; private set; }

        static SectionNames()
        {
            SectionNamesDict = GetSectionsFromText();
        }
        static Dictionary<string, string> GetSectionsFromText()
        {
            var funcDict = new Dictionary<string, string>();
            var rootFolder = Path.GetDirectoryName(ExeRootFolder);
            var sectionPath = Path.Combine(rootFolder, "MIDI", "Sections.txt");
            var customSectionsPath = Path.Combine(rootFolder, "MIDI", "CustomSections.txt");

            if (Path.Exists(sectionPath))
            {
                LoadSections(sectionPath, funcDict);
            }
            if (Path.Exists(customSectionsPath))
            {
                LoadSections(customSectionsPath, funcDict);
            }

            return funcDict;
        }

        private static void LoadSections(string filename, Dictionary<string, string> funcDict)
        {
            try
            {
                var textLines = File.ReadAllLines(filename);

                foreach (var line in textLines)
                {
                    if (line.StartsWith("#")) continue;

                    var newLine = line.TrimEnd('\n').Split(new[] { '\t' }, 2);

                    if (newLine.Length != 2) continue;

                    try
                    {
                        var key = newLine[0];
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
        }
    }
}
