using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.MIDI;

namespace GH_Toolkit_Core.Other
{
    public class MiscMethods
    {
        public static void DuplicateChecker(string folderPath)
        {
            string[]? filePaths = null;
            if (Directory.Exists(folderPath))
            {
                filePaths = Directory.GetFiles(folderPath, "*_song.pak*", SearchOption.AllDirectories);
            }
            else if (File.Exists(folderPath))
            {
                filePaths = [folderPath];
            }
            else
            {
                Console.WriteLine("Invalid path.");
                return;
            }
            foreach (string file in filePaths) 
            {
                var songData = SongQbFile.TokenizePak(file);
                songData.Drums.CheckForDuplicates();
                Console.WriteLine(songData.GetErrorListAsString());
            }

        }
    }
}
