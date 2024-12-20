using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.Methods.CreateForGame;

namespace GH_Toolkit_Core.INI
{
    public class SongIniData
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string CoverArtist { get; set; }
        public string Charter { get; set; }
        public string Genre { get; set; }
        public string Checksum { get; set; }
        public int? Year { get; set; } = 0;
        public int? CoverYear { get; set; } = 0;
        public int? BandTier { get; set; } = 1;
        public int? GuitarTier { get; set; } = 1;
        public int? BassTier { get; set; } = 1;
        public int? DrumsTier { get; set; } = 1;
        public int? VocalsTier { get; set; } = 1;
        public decimal? SustainCutoffThreshold { get; set; }
        public int? HopoFrequency { get; set; }
        public int? PreviewStartTime { get; set; }
        public int? PreviewEndTime { get; set; }
        public bool UseBeatTrack { get; set; }
        public int Low8Bars { get; set; } = 1;
        public int High8Bars { get; set; } = 150;
        public int Low16Bars { get; set; } = 1;
        public int High16Bars { get; set; } = 120;
        public string Countoff { get; set; }
        public string Drumkit { get; set; }
        public string Gender { get; set; }
        public string Vocalist { get; set; }
        public string Aerosmith { get; set; }
        public string Bassist { get; set; }
        public float GuitarVolume { get; set; } = 0.0f; // Guitar volume for GH3/A
        public float BandVolume { get; set; } = 0.0f; // Band volume for GH3/A
        public float? ScrollSpeed { get; set; }
        public int TuningCents { get; set; } = 0;
        public float Volume { get; set; } = 0.0f; // Overall volume for GHWT+
        public bool GuitarMic { get; set; }
        public bool BassMic { get; set; }
        public bool EasyOpens { get; set; }
        public string LipsyncSource { get; set; } = "GHWT";
        public string VenueSource { get; set; } = "GHWT";
        public string WtdeGameIcon { get; set; }
        public string WtdeGameCategory { get; set; }
        public string WtdeBand { get; set; }
        public string Gskeleton { get; set; }
        public string Bskeleton { get; set; }
        public string Dskeleton { get; set; }
        public string Vskeleton { get; set; }
        public bool UseNewClips { get; set; }
        public bool ModernStrobes { get; set; }
        public bool IsCover { get; set; } = false;
    }

}
