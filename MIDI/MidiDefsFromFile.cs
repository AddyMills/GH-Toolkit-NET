using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.MIDI
{
    public partial class MidiDefs
    {

        public class AnimLoopsCache
        {
            public static List<string> AnimLoops { get; private set; }
            public static List<string> AnimLoopsCams { get; private set; }
            private static string filesLocation = Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Other");

            static AnimLoopsCache()
            {
                AnimLoops = LoadAnimLoops("animskas");
                AnimLoopsCams = LoadAnimLoops("cameraskas");
            }
            static List<string> LoadAnimLoops(string filename) 
            { 
                var filepaths = Path.Combine(filesLocation, $"{filename}.txt");
                var animLoops = new List<string>();
                try
                {
                    var textLines = File.ReadAllLines(filepaths);
                    foreach (var line in textLines)
                    {
                        animLoops.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading or processing the file: {ex.Message}");
                }
                return animLoops;
            }
        }
    }
}
