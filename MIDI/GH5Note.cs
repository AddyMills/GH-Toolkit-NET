using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.MIDI
{
    public class GH5Note
    {
        // Define possible values
        private static string[] instruments = { "drums", "bass", "guitar", "vocal" };
        private static string[] difficulties = { "easy", "medium", "hard", "expert" };
        private static string[] modifiers = { "instrument", "starpower", "tapping" };
        public static void ReadNoteFileFromBytes(byte[] notes)
        {

        }
        //public static void ParseGh5StarPower()
        public static (string instrument, string difficulty, string modifier) GetNoteType(string input)
        {
            // Attempt to parse the string
            foreach (var instrument in instruments)
            {
                if (input.StartsWith(instrument))
                {
                    foreach (var difficulty in difficulties)
                    {
                        if (input.Contains(difficulty))
                        {
                            string remaining = input.Substring(instrument.Length + difficulty.Length);
                            foreach (var modifier in modifiers)
                            {
                                if (remaining == modifier)
                                {
                                    return (instrument, difficulty, modifier);
                                }
                            }

                            return (instrument, difficulty, "none");
                        }
                    }

                    return (instrument, "none", "none");
                }
            }

            return ("none", "none", "none");
        }
    }
}
