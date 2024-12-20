using GH_Toolkit_Core.INI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.INI.FileAssignment;
using static GH_Toolkit_Core.INI.SongIniData;


namespace GH_Toolkit_Core.MIDI
{
    public class SongData
    {
        private FileAssignment? filePathInfo = new FileAssignment();
        private SongIniData? songInfo = new SongIniData();

        public void SetSongInfo(SongIniData songInfo)
        {
            this.songInfo = songInfo;
        }
        public SongIniData GetSongInfo() {
            return songInfo;
        }
        public void SetFilePathInfo(FileAssignment filePathInfo)
        {
            this.filePathInfo = filePathInfo;
        }
        public FileAssignment GetFilePathInfo()
        {
            return filePathInfo;
        }

    }
}
