using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidiTheory = Melanchall.DryWetMidi.MusicTheory;

namespace GH_Toolkit_Core.MIDI
{
    public class MidiDefs
    {

        public static Dictionary<MidiTheory.NoteName, int> Gh3Notes = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.C, 1 },
            { MidiTheory.NoteName.CSharp, 2 },
            { MidiTheory.NoteName.D, 4 },
            { MidiTheory.NoteName.DSharp, 8 },
            { MidiTheory.NoteName.E, 16 }
        };

        public static Dictionary<MidiTheory.NoteName, int> Gh4Notes = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.B, 32 },
            { MidiTheory.NoteName.C, 1 },
            { MidiTheory.NoteName.CSharp, 2 },
            { MidiTheory.NoteName.D, 4 },
            { MidiTheory.NoteName.DSharp, 8 },
            { MidiTheory.NoteName.E, 16 }
            
        };

        public static Dictionary<MidiTheory.NoteName, int> Gh4Drums = new Dictionary<MidiTheory.NoteName, int>()
        {
            { MidiTheory.NoteName.B, 64 }, // Expert +
            { MidiTheory.NoteName.C, 32 },
            { MidiTheory.NoteName.CSharp, 2 },
            { MidiTheory.NoteName.D, 4 },
            { MidiTheory.NoteName.DSharp, 8 },
            { MidiTheory.NoteName.E, 16 },
            { MidiTheory.NoteName.F, 1 }
        };

    }
}
