using Melanchall.DryWetMidi.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.MIDI
{
    public partial class Chart
    {
        private const byte SYSMOD = 0x00; // SysEx Modifier
        private const byte SYSTAP = 0x04; // SysEx Tap Note

        private const byte SYSSTART = 0xF0; // SysEx Start Byte
        private const byte SYSEND = 0xF7; // SysEx Start Byte

        private const int TAPNOTE = 6;
        private const int FACEOFFBASENOTE = 105;
        private const int STARPOWERNOTE = 116;
        private const float HopoThreshold = 65/192f; // 65/192 is the threshold for HOPOs in Chart files

        private static readonly byte[] PS_SYSEX = { (byte)'P', (byte)'S', (byte)'\0' };

        private enum DrumParseMode
        {
            RB,
            GH,
            Neither
        }

        private static Dictionary<string, int> DiffBaseNote = new Dictionary<string, int>()
        {
            { "Easy", 60 },
            { "Medium", 72 },
            { "Hard", 84 },
            { "Expert", 96 },
        };        
        private static Dictionary<string, byte> DiffSysex = new Dictionary<string, byte>()
        {
            { "Easy", 0x00 },
            { "Medium", 0x01 },
            { "Hard", 0x02 },
            { "Expert", 0x03 }
        };
        private static Dictionary<int, int> NoteModifier = new Dictionary<int, int>()
        {
            { 0, 0 }, // Green / Kick Drums
            { 1, 1 }, // Red
            { 2, 2 }, // Yellow
            { 3, 3 }, // Blue
            { 4, 4 }, // Orange / Green Drums (RB)
            { 5, 5 }, // Force Hopo / Green Drums (GH)
            { 7, -1 }, // Purple/Open
            {32, -1 } // 2x Kick Drums
        };
        private static Dictionary<int, int> AccentModifier = new Dictionary<int, int>()
        {
            { 34, 1 },
            { 35, 2 },
            { 36, 3 },
            { 37, 4 },
            { 38, 5 }
        };
        private static Dictionary<int, int> GhostModifier = new Dictionary<int, int>()
        {
            { 40, 1 },
            { 41, 2 },
            { 42, 3 },
            { 43, 4 },
            { 44, 5 }
        };
    }
}
