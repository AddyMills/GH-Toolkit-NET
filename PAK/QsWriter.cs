using System.Text;
using static GH_Toolkit_Core.QB.QBConstants;

namespace GH_Toolkit_Core.PAK
{
    public static class QsWriter
    {
        public static void Write(
            Dictionary<string, string> qsList,
            string songFolder,
            string songName,
            string game,
            string consoleExt)
        {
            if (qsList.Count == 0)
                return;

            List<string> qsSaves = new();
            bool addQuotes = false;

            if (game == GAME_GHWT)
            {
                qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs{consoleExt}"));
            }
            else
            {
                qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.de{consoleExt}"));
                qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.en{consoleExt}"));
                qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.es{consoleExt}"));
                qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.fr{consoleExt}"));
                qsSaves.Add(Path.Combine(songFolder, songName + $".mid.qs.it{consoleExt}"));
                addQuotes = true;
            }

            var sortedKeys = qsList
                .OrderBy(entry => entry.Value)
                .Select(entry => entry.Key)
                .ToList();

            foreach (string qsSave in qsSaves)
            {
                using var writer = new StreamWriter(qsSave, false, Encoding.Unicode)
                {
                    NewLine = "\n"
                };

                foreach (var key in sortedKeys)
                {
                    string modifiedKey = key.Substring(2).PadLeft(8, '0');
                    string line = addQuotes
                        ? $"{modifiedKey} \"{qsList[key]}\""
                        : $"{modifiedKey} {qsList[key]}";

                    writer.WriteLine(line);
                }

                writer.WriteLine();
                writer.WriteLine();
            }
        }
    }
}
