using GH_Toolkit_Core.QB;
using System.Diagnostics;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBStruct;
using static GH_Toolkit_Core.QB.QBConstants;

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
        public List<QBStructData> Commands { get; set; } = new List<QBStructData>();
        public List<string> VenueFlags { get; set; } = new List<string>();
        public List<string> CharFlags { get; set; } = new List<string>();
        public List<string> GoalFlags { get; set; } = new List<string>();
        public int StartFrame
        {
            get
            {
                if (!CharIsActive) return 0;
                var minVals = new List<int>();
                if (Guitarist.IsActive) minVals.Add(Guitarist.StartFrame);
                if (Bassist.IsActive) minVals.Add(Bassist.StartFrame);
                if (Vocalist.IsActive) minVals.Add(Vocalist.StartFrame);
                if (Drummer.IsActive) minVals.Add(Drummer.StartFrame);
                return minVals.Min();
            }
            set
            {
                Guitarist.StartFrame = value;
                Bassist.StartFrame = value;
                Vocalist.StartFrame = value;
                Drummer.StartFrame = value;
            }
        }
        public int EndFrame
        {
            get
            {
                if (CharIsActive)
                {
                    var maxVals = new List<int>();
                    if (Guitarist.IsActive) maxVals.Add(Guitarist.EndFrame);
                    if (Bassist.IsActive) maxVals.Add(Bassist.EndFrame);
                    if (Vocalist.IsActive) maxVals.Add(Vocalist.EndFrame);
                    if (Drummer.IsActive) maxVals.Add(Drummer.EndFrame);
                    return maxVals.Max();
                }
                var maxCamera = Cameras.Max(c => c.GetLength());
                return maxCamera;
            }
            set
            {
                Guitarist.EndFrame = value;
                Bassist.EndFrame = value;
                Vocalist.EndFrame = value;
                Drummer.EndFrame = value;
            }
        }
        public int Length
        {
            get
            {
                var length = (int)Math.Round((EndFrame - StartFrame) / 30f / TimeFactor * 1000);

                return RoundClipLen(length);
            }
        }
        public int DebugLength
        {
            get
            {
                return (int)Math.Round((EndFrame - StartFrame) / 30f / TimeFactor * 1000);
            }
        }
        public float TimeFactor
        {
            get
            {
                if (!CharIsActive) return 1f;
                var timeFactors = new List<float>();
                if (Guitarist.IsActive) timeFactors.Add(Guitarist.TimeFactor);
                if (Bassist.IsActive) timeFactors.Add(Bassist.TimeFactor);
                if (Vocalist.IsActive) timeFactors.Add(Vocalist.TimeFactor);
                if (Drummer.IsActive) timeFactors.Add(Drummer.TimeFactor);
                return timeFactors.Min();
            }
            set
            {
                Guitarist.TimeFactor = value;
                Bassist.TimeFactor = value;
                Vocalist.TimeFactor = value;
                Drummer.TimeFactor = value;
            }

        }
        private bool CharIsActive
        {
            get
            {
                return Guitarist.IsActive || Bassist.IsActive || Vocalist.IsActive || Drummer.IsActive;
            }
        }
        private bool CameraIsActive
        {
            get
            {
                return Cameras.Count > 0;
            }
        }
        public SongClip(string name, QBStructData clipStruct)
        {
            Name = name;
            ParseClipStruct(clipStruct);
        }
        private int RoundClipLen(int len)
        {
            int lastTwoDigits = len % 100;
            if (lastTwoDigits == 0 || lastTwoDigits == 33 || lastTwoDigits == 67)
                return len;

            int baseLen = (len / 100) * 100;

            if (lastTwoDigits < 33)
                return baseLen + 33;
            if (lastTwoDigits < 67)
                return baseLen + 67;

            return baseLen + 100;
        }

        public QBItem MakeClip()
        {
            var clipStruct = new QBStructData();
            var clip = new QBItem(Name, clipStruct);
            if (VenueFlags.Count > 0)
            {
                clipStruct.AddArrayToStruct("venueflags", MakeFlags(VenueFlags));
            }
            if (CharFlags.Count > 0)
            {
                clipStruct.AddArrayToStruct("charflags", MakeFlags(CharFlags));
            }
            if (CharIsActive)
            {
                var charArray = MakeCharacterArray();
                clipStruct.AddArrayToStruct("characters", charArray);
            }
            if (CameraIsActive)
            {
                var cameraArray = MakeCameraArray();
                clipStruct.AddArrayToStruct("cameras", cameraArray);
            }

            clipStruct.AddArrayToStruct("commands", MakeCommandsArray());
            return clip;
        }

        private QBArrayNode MakeCharacterArray()
        {
            var characters = new QBArrayNode();
            foreach (ClipCharacter character in new ClipCharacter[] { Guitarist, Bassist, Vocalist, Drummer })
            {
                if (character.IsActive)
                {
                    characters.AddStructToArray(MakeCharacterStruct(character));
                }
            }

            return characters;
        }
        private QBArrayNode MakeFlags(List<string> flags)
        {

            var flagArray = new QBArrayNode();
            foreach (string flag in flags)
            {
                flagArray.AddQbkeyToArray(flag);
            }

            return flagArray;
        }
        private QBStructData MakeCharacterStruct(ClipCharacter clipChar)
        {
            var character = new QBStructData();
            character.AddQbKeyToStruct("name", clipChar.Name);
            character.AddStringToStruct("startnode", clipChar.Startnode);
            character.AddQbKeyToStruct("anim", clipChar.Anim);
            character.AddIntToStruct("startframe", clipChar.StartFrame);
            character.AddIntToStruct("endframe", clipChar.EndFrame);
            bool tfIsInteger = clipChar.TimeFactor == Math.Floor(clipChar.TimeFactor);
            if (tfIsInteger)
            {
                character.AddIntToStruct("timefactor", (int)clipChar.TimeFactor);
            }
            else
            {
                character.AddFloatToStruct("timefactor", clipChar.TimeFactor);
            }
            character.AddQbKeyToStruct("ik_targetl", clipChar.Arms.IKTargetL);
            character.AddQbKeyToStruct("ik_targetr", clipChar.Arms.IKTargetR);
            character.AddQbKeyToStruct("strum", clipChar.Arms.Strum.ToString());
            character.AddQbKeyToStruct("fret", clipChar.Arms.Fret.ToString());
            character.AddQbKeyToStruct("chord", clipChar.Arms.Chord.ToString());

            return character;
        }
        private QBArrayNode MakeCameraArray()
        {
            var cameras = new QBArrayNode();
            foreach (ClipCamera camera in Cameras)
            {
                var cameraStruct = new QBStructData();
                cameraStruct.AddIntToStruct("slot", camera.GetSlot());
                cameraStruct.AddStringToStruct("name", camera.Name);
                cameraStruct.AddQbKeyToStruct("anim", camera.Anim);
                cameras.AddStructToArray(cameraStruct);
            }

            return cameras;
        }
        private QBArrayNode MakeCommandsArray()
        {
            var commands = new QBArrayNode();

            foreach (QBStructData command in Commands)
            {
                commands.AddStructToArray(command);
            }

            return commands;
        }
        private void ParseClipStruct(QBStruct.QBStructData clipStruct)
        {
            foreach (string key in clipStruct.StructDict.Keys)
            {
                switch (key.ToLower())
                {
                    case "characters":
                        ParseCharacters(clipStruct.StructDict[key] as QBArray.QBArrayNode);
                        break;
                    case "cameras":
                    case "vocalist_cameras":
                    case "bassist_cameras":
                    case "guitarist_cameras":
                    case "secondary_cameras":
                        ParseCameras(clipStruct.StructDict[key] as QBArrayNode, key);
                        break;
                    case "startnodes":
                        ParseStartnode(clipStruct.StructDict[key] as QBStructData);
                        break;
                    case "anims":
                        ParseAnims(clipStruct.StructDict[key] as QBStructData);
                        break;
                    case "arms":
                        ParseArms(clipStruct.StructDict[key] as QBStructData);
                        break;
                    case "events":
                        ParseEvents(clipStruct.StructDict[key] as QBArrayNode, true);
                        break;
                    case "commands":
                        ParseEvents(clipStruct.StructDict[key] as QBArrayNode);
                        break;
                    case "venueflags":
                        VenueFlags = ParseFlags(clipStruct.StructDict[key] as QBArrayNode);
                        break;
                    case "charflags":
                        CharFlags = ParseFlags(clipStruct.StructDict[key] as QBArrayNode);
                        break;
                    case "anim": // Old data, maybe? Only seems to be in Jimi DLC
                    case "measures": // I think this is unused
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
                                clipChar.TimeFactor = Convert.ToSingle(data.StructDict[key]);
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
                        switch (key.ToLower())
                        {
                            case "name":
                                break;
                            case "anim":
                                clipCam.Anim = data.StructDict[key].ToString();
                                break;
                            case "slot":
                                clipCam.SetSlot(Convert.ToInt32(data.StructDict[key]));
                                break;
                            case "venues":
                            case "weight":
                                // I don't know what these do right now
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
        private void ParseEvents(QBArrayNode events, bool ghwt = false)
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
        private List<string> ParseFlags(QBArrayNode flags)
        {
            var flagList = new List<string>();
            foreach (string? flag in flags.Items)
            {
                if (flag != null)
                {
                    flagList.Add(flag);
                }
            }
            return flagList;
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
        public void UpdateFromParams(QBStruct.QBStructData clipParams)
        {
            foreach (string key in clipParams.StructDict.Keys)
            {
                switch (key)
                {
                    case "startframe":
                        int startFrame = Convert.ToInt32(clipParams.StructDict[key]);
                        Guitarist.StartFrame = startFrame;
                        Bassist.StartFrame = startFrame;
                        Vocalist.StartFrame = startFrame;
                        Drummer.StartFrame = startFrame;
                        break;
                    case "endframe":
                        int endFrame = Convert.ToInt32(clipParams.StructDict[key]);
                        Guitarist.EndFrame = endFrame;
                        Bassist.EndFrame = endFrame;
                        Vocalist.EndFrame = endFrame;
                        Drummer.EndFrame = endFrame;
                        break;
                    case "timefactor":
                        float timeFactor = Convert.ToSingle(clipParams.StructDict[key]);
                        Guitarist.TimeFactor = timeFactor;
                        Bassist.TimeFactor = timeFactor;
                        Vocalist.TimeFactor = timeFactor;
                        Drummer.TimeFactor = timeFactor;
                        break;
                }
            }
        }
        /// <summary>
        /// This method will check the start and end frames to ensure they are within the bounds of the animation.
        /// It will also check to make sure the start frame is less than the end frame.
        /// Then it will check the animation files to make sure they exist.
        /// Finally, it will check to make sure the animation ends within 100ms of a camera cut.
        /// If it doesn't line up and there is a camera cut within 100ms of the end frame, it will adjust the end frame to match the camera cut.
        /// </summary>
        public void UpdateFromSkaFile(string skaPath, int startChange, int endChange, int closeStart, int closeEnd, out int msChange, Dictionary<string, string> skaQbKeys)
        {
            List<ClipCharacter> charList = new List<ClipCharacter>() { Guitarist, Bassist, Vocalist, Drummer };
            List<string> dontExist = new List<string>();
            msChange = 0;
            int frameChange = 0;
            foreach (ClipCharacter character in charList)
            {
                if (!character.IsActive)
                {
                    continue;
                }
                if (skaQbKeys.ContainsKey(character.Anim))
                {
                    character.Anim = skaQbKeys[character.Anim];
                }
                string animPath = GetSkaPath(skaPath, character.Anim);
                if (string.IsNullOrEmpty(animPath))
                {
                    dontExist.Add($"{Name}:{character.Anim}");
                    continue;
                }

                float skaLength = GetSkaLength(animPath);
                int skaLengthFrames = (int)Math.Round(skaLength * 30);

                if (character.StartFrame < 0)
                {
                    character.StartFrame = 0;
                }
                if (character.EndFrame <= 0)
                {
                    character.EndFrame = skaLengthFrames;
                }
                if (character.StartFrame > character.EndFrame)
                {
                    character.StartFrame = character.EndFrame;
                }

                AdjustFrames(character, startChange, endChange, skaLengthFrames, out frameChange);

                // Ensure the EndFrame is never larger than skaLengthFrames
                if (character.EndFrame > skaLengthFrames)
                {
                    character.EndFrame = skaLengthFrames;
                }

                msChange = (int)Math.Round(frameChange / 30f * 1000);
            }
            // Check if cameras exist in ska path
            foreach (ClipCamera camera in Cameras)
            {
                if (skaQbKeys.ContainsKey(camera.Anim))
                {
                    camera.Anim = skaQbKeys[camera.Anim];
                }
                string animPath = GetSkaPath(skaPath, camera.Anim);
                if (string.IsNullOrEmpty(animPath))
                {
                    dontExist.Add($"{Name}:{camera.Anim}");
                    continue;
                }
                float skaLength = GetSkaLength(animPath);
                int skaLengthFrames = (int)Math.Round(skaLength * 30);
                camera.Length = skaLengthFrames;

            }
            if (dontExist.Count > 0)
            {
                throw new Exception($"{string.Join(",", dontExist)}");
            }
        }

        private void AdjustFrames(ClipCharacter character, int startChange, int endChange, int skaLengthFrames, out int frameChange)
        {
            frameChange = 0;

            if (startChange != 0)
            {
                character.StartFrame -= startChange;
            }

            if (character.StartFrame < 0)
            {
                frameChange = -character.StartFrame;
                character.StartFrame = 0;
            }

            if (endChange != 0)
            {
                character.EndFrame -= endChange;
            }

            if (character.EndFrame > skaLengthFrames)
            {
                frameChange -= (skaLengthFrames - character.EndFrame);
                character.EndFrame = skaLengthFrames;
            }

            if (character.EndFrame < skaLengthFrames && frameChange > 0)
            {
                while (character.EndFrame < skaLengthFrames && frameChange > 0)
                {
                    frameChange--; character.EndFrame++;
                }
            }

            if (character.StartFrame > 0 && frameChange > 0)
            {
                while (character.StartFrame > 0 && frameChange > 0)
                {
                    frameChange--; character.StartFrame--;
                }
            }
        }
        private string GetSkaPath(string skaPath, string anim)
        {
            List<string> extensions = new List<string>() { ".ska.xen", ".ska", ".ska.ps3" };
            foreach (string ext in extensions)
            {
                string animPath = Path.Combine(skaPath, $"{anim}{ext}");
                if (File.Exists(animPath))
                {
                    return animPath;
                }
            }
            return string.Empty;
        }
        private float GetSkaLength(string skaPath)
        {
            // Read the file bytes
            byte[] fileBytes = File.ReadAllBytes(skaPath);
            // Ensure the file has enough bytes
            if (fileBytes.Length < 44)
            {
                Console.WriteLine($"{Path.GetFileName(skaPath)} does not contain enough bytes.");
                throw new Exception($"Invalid SKA file. {Path.GetFileName(skaPath)} does not contain enough bytes.");
            }

            // Extract bytes 40-44
            byte[] byteArray = new byte[4];
            Array.Copy(fileBytes, 40, byteArray, 0, 4);

            // Convert from big-endian to little-endian if system is little-endian
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }

            // Convert to float
            float result = BitConverter.ToSingle(byteArray, 0);

            return result;
        }
        [DebuggerDisplay("{Name}: {DebugAnim}")]
        public class ClipCharacter
        {
            public string? Name { get; set; }
            public string? Startnode { get; set; }
            public string? Anim { get; set; }
            public int StartFrame { get; set; }
            public int EndFrame { get; set; }
            public float TimeFactor { get; set; } = 1f;
            public ClipArms Arms { get; set; }
            public bool IsActive
            {
                get
                {
                    return !string.IsNullOrEmpty(Anim);
                }
            }
            private string DebugAnim
            {
                get
                {
                    return IsActive ? Anim : "Inactive";
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
                    bool isVocalist = name.ToLower() == "vocalist";

                    IKTargetL = ikType;
                    IKTargetR = ikType;
                    Strum = !isVocalist;
                    Fret = !isVocalist;
                    Chord = !isVocalist;
                }
            }
        }
        [DebuggerDisplay("Slot {Slot}: {Anim}")]
        public class ClipCamera
        {
            public string? Name { get; set; }
            public string? Anim { get; set; }
            private int Slot { get; set; } = -1;
            public int Length { get; set; }
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
            public int GetLength()
            {
                return Length;
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
