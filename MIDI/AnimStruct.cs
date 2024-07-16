using GH_Toolkit_Core.QB;
using System.Diagnostics;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBStruct;
using static GH_Toolkit_Core.MIDI.MidiDefs;
using System.Xml.Linq;
namespace GH_Toolkit_Core.MIDI
{
    [DebuggerDisplay("{NumActive} Anim Structs")]
    public class AnimStruct
    {
        public InstrumentAnim Guitar { get; set; } = new InstrumentAnim("guitar");
        public InstrumentAnim Bass { get; set; } = new InstrumentAnim("bass");
        public InstrumentAnim Drum { get; set; } = new InstrumentAnim("drum");
        public InstrumentAnim Vocals { get; set; } = new InstrumentAnim("vocals");
        public string? Gender { get; set; }
        public bool IsAlt { get; set; }
        private int NumActive
        {
            get
            {
                return (Guitar.IsActive ? 1 : 0) + (Bass.IsActive ? 1 : 0) + (Drum.IsActive ? 1 : 0) + (Vocals.IsActive ? 1 : 0);
            }
        }
        [DebuggerDisplay("{DebugName}")]
        public class InstrumentAnim
        {
            public string? Pak { get; set; }
            public string? AnimSet { get; set; }
            public string? FingerAnims { get; set; }
            public string? FretAnims { get; set; }
            public string? StrumAnims { get; set; }
            public string? FacialAnims { get; set; }
            public string Instrument { get; set; }
            public bool IsActive
            {
                get
                {
                    return Pak != null || AnimSet != null || FingerAnims != null || FretAnims != null || StrumAnims != null || FacialAnims != null;
                }
            }
            private string? DebugName
            {
                get
                {
                    return Pak == null ? AnimSet : Pak;
                }
            }
            public InstrumentAnim()
            {

            }
            public InstrumentAnim(string instrument)
            {
                Instrument = instrument;
            }
            public QBStructData MakeCharacterStruct()
            {
                var character = new QBStructData();
                if (Pak != null)
                {
                    character.AddQbKeyToStruct("pak", Pak);
                }
                if (AnimSet != null)
                {
                    character.AddQbKeyToStruct("anim_set", AnimSet);
                }
                if (FingerAnims != null)
                {
                    character.AddQbKeyToStruct("finger_anims", FingerAnims);
                }
                if (FretAnims != null)
                {
                    character.AddQbKeyToStruct("fret_anims", FretAnims);
                }
                if (StrumAnims != null)
                {
                    character.AddQbKeyToStruct("strum_anims", StrumAnims);
                }
                if (FacialAnims != null)
                {
                    character.AddQbKeyToStruct("facial_anims", FacialAnims);
                }
                return character;
            }
            public void SetDefaultValues(string gender)
            {
                if (Pak == null || !legalLoops.Any(loop => Pak.ToLower().Contains(loop)))
                {
                    Pak = DefaultStructs[gender][Instrument].Pak;
                }
                if (AnimSet == null || !legalLoops.Any(loop => AnimSet.ToLower().Contains(loop)))
                {
                    AnimSet = DefaultStructs[gender][Instrument].AnimSet;
                }
                FingerAnims = DefaultStructs[gender][Instrument].FingerAnims;
                FretAnims = DefaultStructs[gender][Instrument].FretAnims;
                StrumAnims = DefaultStructs[gender][Instrument].StrumAnims;
                FacialAnims = DefaultStructs[gender][Instrument].FacialAnims;
            }
        }
        public string GetName()
        {
            string altName = IsAlt ? "alt_anim_struct" : "anim_struct";
            string structName = $"car_{Gender}_{altName}";
            return structName;
        }
        public AnimStruct(string gender, bool isAlt)
        {
            Gender = gender;
            IsAlt = isAlt;
        }
        public AnimStruct(string gender, QBStructData animStruct, bool isAlt)
        {
            Gender = gender;
            IsAlt = isAlt;
            ParseAnimStruct(animStruct);
        }
        public QBItem MakeAnimStruct(string name)
        {
            var structName = $"{GetName()}_{name}";
            var animStruct = new QBStructData();
            foreach (var instrument in new InstrumentAnim[] { Guitar, Bass, Drum, Vocals })
            {
                if (instrument.IsActive)
                {
                    animStruct.AddStructToStruct(instrument.Instrument, instrument.MakeCharacterStruct());
                }
            }
            return new QBItem(structName, animStruct);
        }
        public void MakeAnimStructGh5()
        {
            SetDefaultAnimStruct();
        }
        public void SetDefaultAnimStruct()
        {
            Guitar.SetDefaultValues(Gender);
            Bass.SetDefaultValues(Gender);
            Drum.SetDefaultValues(Gender);
            Vocals.SetDefaultValues(Gender);
        }
        private void ParseAnimStruct(QBStruct.QBStructData animStruct)
        {
            foreach (string key in animStruct.StructDict.Keys)
            {
                switch (key)
                {
                    case "guitar":
                        ParseInstrument(animStruct.StructDict[key] as QBStruct.QBStructData, Guitar);
                        break;
                    case "bass":
                        ParseInstrument(animStruct.StructDict[key] as QBStruct.QBStructData, Bass);
                        break;
                    case "drum":
                        ParseInstrument(animStruct.StructDict[key] as QBStruct.QBStructData, Drum);
                        break;
                    case "vocals":
                        ParseInstrument(animStruct.StructDict[key] as QBStruct.QBStructData, Vocals);
                        break;

                }
            }
        }
        private void ParseInstrument(QBStruct.QBStructData instrumentStruct, InstrumentAnim instrument)
        {
            foreach (string key in instrumentStruct.StructDict.Keys)
            {
                switch (key.ToLower())
                {
                    case "pak":
                        instrument.Pak = instrumentStruct.StructDict[key].ToString();
                        break;
                    case "anim_set":
                        instrument.AnimSet = instrumentStruct.StructDict[key].ToString();
                        break;
                    case "finger_anims":
                        instrument.FingerAnims = instrumentStruct.StructDict[key].ToString();
                        break;
                    case "fret_anims":
                        instrument.FretAnims = instrumentStruct.StructDict[key].ToString();
                        break;
                    case "strum_anims":
                        instrument.StrumAnims = instrumentStruct.StructDict[key].ToString();
                        break;
                    case "facial_anims":
                        instrument.FacialAnims = instrumentStruct.StructDict[key].ToString();
                        break;
                }
            }
        }
        
    }
}
