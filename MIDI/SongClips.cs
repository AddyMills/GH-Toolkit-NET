using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.QB;

namespace GH_Toolkit_Core.MIDI
{
    [DebuggerDisplay("Clip: {Name}")]
    public class SongClip
    {
        public string? Name { get; set; }
        public ClipCharacter Guitarist { get; set; } = new ClipCharacter("Guitarist");
        public ClipCharacter Bassist { get; set; } = new ClipCharacter("Bassist");
        public ClipCharacter Vocalist { get; set; } = new ClipCharacter("Vocalist");
        public ClipCharacter Drummer { get; set; } = new ClipCharacter("Drummer");
        public List<ClipCamera> Cameras { get; set; } = new List<ClipCamera>();
        public List<QBStruct.QBStructData> Commands { get; set; } = new List<QBStruct.QBStructData>();
        public SongClip(string name, QBStruct.QBStructData clipStruct)
        {
            Name = name;
            ParseClipStruct(clipStruct);
        }
        private void ParseClipStruct(QBStruct.QBStructData clipStruct)
        {
            foreach (string key in clipStruct.StructDict.Keys)
            {
                switch (key)
                {
                    case "characters":
                        ParseCharacters(clipStruct.StructDict[key] as QBArray.QBArrayNode);
                        break;
                    case "cameras":
                    case "vocalist_cameras":
                    case "bassist_cameras":
                    case "guitarist_cameras":
                    case "secondary_cameras":
                        ParseCameras(clipStruct.StructDict[key] as QBArray.QBArrayNode, key);
                        break;
                    case "startnodes":
                        ParseStartnode(clipStruct.StructDict[key] as QBStruct.QBStructData);
                        break;
                    case "anims":
                        ParseAnims(clipStruct.StructDict[key] as QBStruct.QBStructData);
                        break;
                    case "arms":
                        ParseArms(clipStruct.StructDict[key] as QBStruct.QBStructData);
                        break;
                    case "events":
                        ParseEvents(clipStruct.StructDict[key] as QBArray.QBArrayNode, true);
                        break;
                    case "commands":
                        ParseEvents(clipStruct.StructDict[key] as QBArray.QBArrayNode);
                        break;
                    case "anim":
                        // Old data, maybe? Only seems to be in Jimi DLC
                        break;
                    default:
                        break;
                }
            }
        }
        private void ParseCharacters(QBArray.QBArrayNode characters)
        {
            if (characters.FirstItem.Type != "Struct")
            {
                return;
            }
            foreach (QBStruct.QBStructData data in characters.Items)
            {
                if (data.StructDict.TryGetValue("name", out object? character))
                {
                    var clipChar = GetCharacter(character.ToString());
                    foreach (string key in data.StructDict.Keys)
                    {
                        switch (key)
                        {
                            case "name":
                                break;
                            case "startnode":
                                clipChar.Startnode = data.StructDict[key].ToString();
                                break;
                            case "anim":
                                clipChar.Anim = data.StructDict[key].ToString();
                                break;
                            case "startframe":
                                clipChar.StartFrame = Convert.ToInt32(data.StructDict[key]);
                                break;
                            case "endframe":
                                clipChar.EndFrame = Convert.ToInt32(data.StructDict[key]);
                                break;
                            case "timefactor":
                                clipChar.TimeFactor = Convert.ToInt32(data.StructDict[key]);
                                break;
                            case "ik_targetl":
                                clipChar.Arms.IKTargetL = data.StructDict[key].ToString();
                                break;
                            case "ik_targetr":
                                clipChar.Arms.IKTargetR = data.StructDict[key].ToString();
                                break;
                            case "strum":
                                clipChar.Arms.Strum = Convert.ToBoolean(data.StructDict[key]);
                                break;
                            case "fret":
                                clipChar.Arms.Fret = Convert.ToBoolean(data.StructDict[key]);
                                break;
                            case "chord":
                                clipChar.Arms.Chord = Convert.ToBoolean(data.StructDict[key]);
                                break;
                            default:
                                throw new Exception($"Unknown character key: {key}");
                        }
                    }
                }
                else
                {
                    throw new Exception("No characters found in clip");
                }
            }
        }
        private void ParseCameras(QBArray.QBArrayNode cameras, string cameraType = "cameras")
        {
            if (cameras.FirstItem.Type != "Struct")
            {
                return;
            }
            var cameraAdd = new Dictionary<string, int>()
            {
                { "cameras", 0 },
                { "vocalist_cameras", 0 },
                { "bassist_cameras", 3 },
                { "guitarist_cameras", 6 },
                { "secondary_cameras", 9 }
            };

            int slots = cameraAdd[cameraType];

            foreach (QBStruct.QBStructData data in cameras.Items)
            {
                if (data.StructDict.TryGetValue("name", out object? camera))
                {
                    var clipCam = new ClipCamera(camera.ToString());
                    foreach (string key in data.StructDict.Keys)
                    {
                        switch (key)
                        {
                            case "name":
                                break;
                            case "anim":
                                clipCam.Anim = data.StructDict[key].ToString();
                                break;
                            case "slot":
                                clipCam.SetSlot(Convert.ToInt32(data.StructDict[key]));
                                break;
                            default:
                                throw new Exception($"Unknown camera key: {key}");
                        }
                    }
                    if (clipCam.GetSlot() == -1)
                    {
                        clipCam.SetSlot(slots);
                        slots++;
                    }
                    Cameras.Add(clipCam);
                }
                else
                {
                    throw new Exception("No cameras found in clip");
                }
            }
        }
        private void ParseStartnode(QBStruct.QBStructData nodeData)
        {
            foreach (string key in nodeData.StructDict.Keys)
            {
                var character = GetCharacter(key);
                character.Startnode = nodeData.StructDict[key].ToString();
            }
        }
        private void ParseAnims(QBStruct.QBStructData nodeData)
        {
            foreach (string key in nodeData.StructDict.Keys)
            {
                string anim = nodeData.StructDict[key].ToString();
                if (anim.ToLower().Equals("none"))
                {
                    continue;
                }
                var character = GetCharacter(key);
                character.Anim = anim;
            }
        }
        private bool OldArmsBoolean(string value)
        {
            return value.ToLower().Equals("on");
        }

        private void ParseArms(QBStruct.QBStructData nodeData)
        {
            foreach (string key in nodeData.StructDict.Keys)
            {
                var character = GetCharacter(key);
                var armsData = nodeData.StructDict[key] as QBStruct.QBStructData;
                foreach (string armKey in armsData.StructDict.Keys)
                {
                    switch (armKey)
                    {
                        case "ik_targetl":
                            character.Arms.IKTargetL = armsData.StructDict[armKey].ToString();
                            break;
                        case "ik_targetr":
                            character.Arms.IKTargetR = armsData.StructDict[armKey].ToString();
                            break;
                        case "strum":
                            character.Arms.Strum = OldArmsBoolean(armsData.StructDict[armKey].ToString());
                            break;
                        case "fret":
                            character.Arms.Fret = OldArmsBoolean(armsData.StructDict[armKey].ToString());
                            break;
                        case "chord":
                            character.Arms.Chord = OldArmsBoolean(armsData.StructDict[armKey].ToString());
                            break;
                        default:
                            throw new Exception($"Unknown arm key: {armKey}");
                    }
                }
            }
        }
        private void ParseEvents(QBArray.QBArrayNode events, bool ghwt = false)
        {
            if (events.FirstItem.Type != "Struct")
            {
                return;
            }
            foreach (QBStruct.QBStructData data in events.Items)
            {
                if (data.StructDict.TryGetValue("time", out object? time))
                {
                    if (ghwt)
                    {
                        int timeRounded = (int)Math.Round((Convert.ToSingle(time) * 1000f));
                        data["time"] = timeRounded;
                    }
                    Commands.Add(data);
                }
            }
        }
        private ClipCharacter GetCharacter(string character)
        {
            switch (character)
            {
                case "guitarist":
                    return Guitarist;
                case "bassist":
                    return Bassist;
                case "vocalist":
                    return Vocalist;
                case "drummer":
                    return Drummer;
                default:
                    throw new Exception($"Invalid character: {character}");
            }
        }
        [DebuggerDisplay("{Name}: {Anim}")]
        public class ClipCharacter
        {
            public string? Name { get; set; }
            public string? Anim { get; set; }
            public string? Startnode { get; set; }
            public int TimeFactor { get; set; } = 1;
            public int StartFrame { get; set; }
            public int EndFrame { get; set; }
            public ClipArms Arms { get; set; }
            public bool IsActive { get
                {
                    return !string.IsNullOrEmpty(Anim);
                } 
            }
            public ClipCharacter(string name)
            {
                Name = name;
                Startnode = $"{Name.ToLower()}_start";
                Arms = new ClipArms(name);
            }
            public class ClipArms
            {
                public string IKTargetL { get; set; }
                public string IKTargetR { get; set; }
                public bool Strum { get; set; }
                public bool Fret { get; set; }
                public bool Chord { get; set; }
                public ClipArms(string name)
                {
                    string ikType = name.ToLower() switch
                    {
                        "vocalist" => "slave",
                        _ => "guitar"
                    };
                    bool isVocalist = name.ToLower() != "vocalist";

                    IKTargetL = ikType;
                    IKTargetR = ikType;
                    Strum = isVocalist;
                    Fret = isVocalist;
                    Chord = isVocalist;
                }
            }
        }
        [DebuggerDisplay("Slot {Slot}: {Anim}")]
        public class ClipCamera
        {
            public string? Name { get; set; }
            public string? Anim { get; set; }
            private int Slot { get; set; } = -1;
            public ClipCamera(string name)
            {
                string lowName = name.ToLower();
                if (lowName.StartsWith("trg"))
                {
                    Name = name;
                }
                else if (IsForInstrument(lowName))
                {
                    Name = $"TRG_Geo_Camera_Performance_{name.ToUpper()}";
                }
                else
                {
                    throw new Exception($"Invalid camera name: {name}");
                }
            }
            public void SetSlot(int slot)
            {
                Slot = slot;
            }
            public int GetSlot()
            {
                return Slot;
            }
            private bool IsForInstrument(string camera)
            {
                return camera.Contains("guit") ||
                    camera.Contains("bass") ||
                    camera.Contains("sing") ||
                    camera.Contains("drum");
            }
        }
    }
}
