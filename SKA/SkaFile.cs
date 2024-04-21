using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Methods;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.SKA.BoneTables;

namespace GH_Toolkit_Core.SKA
{
    public class SkaFile
    {
        // Flag constants
        private const int TRANS_FLOAT_TIME = 1 << 31; // If set, translation values are in floats and overall simpler to parse
        private const int NO_SEP_TIME = 1 << 28; // If set, only 2 bytes for time, else 4
        private const int COMP_FLAGS = 1 << 23; // If set, compression flags exist in the time values
        private const int PARTIAL_ANIM = 1 << 19; // If set, the ska file has a partial anim footer
        private const int BIG_TIME = 1 << 8; // Time goes above 2047 if set
        private const int SINGLE_FRAME = 1 << 6; // Single anim frames.

        private const ushort NEG_W = 1 << 15;
        private const ushort COMP_FLAG = 1 << 14;
        private const ushort COMP_X = 1 << 13;
        private const ushort COMP_Y = 1 << 12;
        private const ushort COMP_Z = 1 << 11;

        private const ushort FULL_TIME_MASK = 0b11111111111;
        private const ushort DEADDEAD = 0xDEAD;
        private const byte TRANS_FLAGTIME_MAX = 0x40;
        private const byte POINTER_BLOCK_AMOUNT = 20;
        private const int VERSION_PS2 = 0x28;
        private const int VERSION_OLD = 0x48;
        private const int VERSION_NEW = 0x68;
        private const int SKA_BOUNDARY = 0x80;
        private const int ANIM_BOUNDARY = 0x4;
        private const int ANIM_START = 0x20;

        // Game constants
        private const int GH3_FLOATS = 2; // Number of float value arrays per GH3/GHA ska file
        private const int GHWT_FLOATS = 4; // Number of float value arrays per GHWT and up ska file
        private const int FLOAT_VALS = 4; // Number of float values per SKA file
        private const int BONE_INTS = 3; // Number of ints per skeleton

        // Header data
        public string Game { get; set; }
        public string SkeletonType { get; set; }
        public int OffsetUnknownA { get; set; } // In GH3/GHA ska files
        public int FileSize { get; set; }
        public int OffsetAnim { get; set; }
        public int UnknownA { get; set; } // 0 for GH3/GHA/GHWT, 410 for GHM/GHSH/GHVH, 500 for GH5/GHWoR
        public int OffsetBonePoint { get; set; } // Offset to the bone pointer data
        public int OffsetUnknownB { get; set; }
        public int OffsetUnknownC { get; set; }
        public int OffsetPartialAnim { get; set; } // Offset to the partial animation data
        public int OffsetUnknownD { get; set; }
        // Animation data
        public int Version { get; set; }
        public int Flags { get; set; }
        public float Duration { get; set; }
        public int DurationFrames { get { return (int)(Duration * 60); } }
        public ushort NumBones { get; set; }
        public ushort QuatFrames { get; set; } // Number of quaternion frames
        public float[][] FloatValues { get; set; } // Array of float values
        public ushort TransFrames { get; set; } // Number of translation frames
        public ushort CustomKeyCount { get; set; } // Number of custom keys
        public int OffsetCustomKeys { get; set; } // Offset to the custom keys
        public int OffsetQuatFrames { get; set; } // Offset to the quaternion frames
        public int OffsetTransFrames { get; set; } // Offset to the translation frames
        public int OffsetQuatBoneSize { get; set; } // Offset to the quaternion bone size array
        public int OffsetTransBoneSize { get; set; } // Offset to the translation bone size array
        public Dictionary<int, ushort> QuatBoneSizes { get; set; } = new Dictionary<int, ushort>(); // Dictionary of bone sizes for quaternion frames per bone
        public Dictionary<int, ushort> TransBoneSizes { get; set; } = new Dictionary<int, ushort>(); // Dictionary of bone sizes for translation frames per bone
        public Dictionary<int, List<BoneFrameQuat>> QuatData { get; set; } = new Dictionary<int, List<BoneFrameQuat>>(); // Dictionary of quaternion data per bone
        public Dictionary<int, List<BoneFrameTrans>> TransData { get; set; } = new Dictionary<int, List<BoneFrameTrans>>(); // Dictionary of translation data per bone
        public Dictionary<int, List<(ushort frame, int position)>> BonePointerData { get; set; } = new Dictionary<int, List<(ushort frame, int position)>>(); // Dictionary of bone pointer data per bone
        public List<CustomKey> CustomKeys { get; set; } = new List<CustomKey>(); // List of custom keys
        public List<int> AnimFlags { get; set; } = new List<int>(); // List of bones that have animation data
        internal int FloatPairs { get; set; } // Number of float value pairs
        internal ReadWrite _rw;
        internal ReadWrite _rwPs2 = new ReadWrite("little");
        internal ushort TimeMask { get; set; }
        internal bool LargeTime { get; set; }
        internal bool HasTransFloatTime { get; set; }
        internal bool HasNoSepTime { get; set; }
        internal bool HasCompFlags { get; set; }
        internal bool HasPartialAnim { get; set; }
        internal bool HasBigTime { get; set; }
        internal bool IsSingleFrame { get; set; }
        internal float UnknownFloatA { get; set; }
        internal float UnknownFloatB { get; set; }
        internal float UnknownFloatC { get; set; }
        internal int NewBones { get; set; }
        internal uint NewFlags { get; set; }
        internal Dictionary<int, Dictionary<ushort, int>> NewBonePointers { get; set; } = new Dictionary<int, Dictionary<ushort, int>>();

        public SkaFile()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkaFile"/> class.
        /// </summary>
        /// <param name="filePath">The path of the SKA file.</param>
        /// <param name="endian">The endian format.</param>
        public SkaFile(string filePath, string endian)
        {
            _rw = new ReadWrite(endian);
            if (File.Exists(filePath))
            {
                ParseSkaFile(filePath);
            }
            else
            {
                throw new Exception("File does not exist.");
            }
        }
        [DebuggerDisplay("Frame {FrameTime}: X: {QuatX} Y: {QuatY} Z: {QuatZ}")]
        public class BoneFrameQuat
        {
            public ushort FrameTime { get; set; }
            public float RealTime { get { return FrameTime / 60; } }
            public short QuatX { get; set; }
            public short QuatY { get; set; }
            public short QuatZ { get; set; }
            public BoneFrameQuat(ushort frameTime, short quatX, short quatY, short quatZ)
            {
                FrameTime = frameTime;
                QuatX = quatX;
                QuatY = quatY;
                QuatZ = quatZ;
            }
        }
        [DebuggerDisplay("Frame {FrameTime}: X: {TransX} Y: {TransY} Z: {TransZ}")]
        public class BoneFrameTrans
        {
            public ushort FrameTime { get; set; }
            public float RealTime { get { return FrameTime / 60; } }
            public float TransX { get; set; }
            public float TransY { get; set; }
            public float TransZ { get; set; }
            public BoneFrameTrans(ushort frameTime, float transX, float transY, float transZ)
            {
                FrameTime = frameTime;
                TransX = transX;
                TransY = transY;
                TransZ = transZ;
            }
        }
        [DebuggerDisplay("Frame {FrameTime}: X: {TransX} Y: {TransY} Z: {TransZ}")]
        public class BoneFrameTransCompressed
        {
            public ushort FrameTime { get; set; }
            public float RealTime { get { return FrameTime / 60; } }
            public short TransX { get; set; }
            public short TransY { get; set; }
            public short TransZ { get; set; }
            public BoneFrameTransCompressed(ushort frameTime, short transX, short transY, short transZ)
            {
                FrameTime = frameTime;
                TransX = transX;
                TransY = transY;
                TransZ = transZ;
            }
        }
        [DebuggerDisplay("Time: {KeyTime} Type: {KeyType} Value: {KeyValue} Modifier: {KeyMod}")]
        public class CustomKey
        {
            public float KeyTime { get; set; }
            public int KeyType { get; set; }
            public int KeyValue { get; set; }
            public float KeyMod { get; set; }
            public CustomKey(float keyTime, int keyType, int keyValue, float keyMod)
            {
                KeyTime = keyTime;
                KeyType = keyType;
                KeyValue = keyValue;
                KeyMod = keyMod;
            }
        }
        /// <summary>
        /// Parses the SKA file.
        /// </summary>
        /// <param name="filePath">The path of the SKA file to parse.</param>
        private void ParseSkaFile(string filePath)
        {
            byte[] skaData = File.ReadAllBytes(filePath);
            using (var stream = new MemoryStream(skaData))
            {
                ReadHeader(stream);
                ReadAnimation(stream);
                SetSkeletonType();
                CheckFlags();
                ReadQuatBoneSize(stream);
                ReadTransBoneSize(stream);
                if (HasPartialAnim)
                {
                    ReadPartialAnim(stream);
                }
                else if (SkeletonType == SKELETON_CAMERA)
                {
                    for (int i = 0; i < NumBones; i++)
                    {
                        AnimFlags.Add(i);
                    }
                }
                else
                {
                    throw new NotImplementedException("Partial anim data not found.");
                }
                ReadQuatData(stream);
                ReadTransData(stream);
                if (OffsetCustomKeys != -1)
                {
                    ProcessCustomKeys(stream);
                }
                if (OffsetBonePoint != -1)
                {
                    ReadBonePointers(stream);
                }
            }
        }

        /// <summary>
        /// Reads the header of the SKA file.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        private void ReadHeader(MemoryStream stream)
        {
            int gameCheck = _rw.ReadInt32(stream);
            if (gameCheck == 0)
            {
                Game = GAME_GH3;
                ReadOldHeader(stream);
                FloatPairs = GH3_FLOATS;
            }
            else
            {
                FileSize = gameCheck;
                ReadNewHeader(stream);
                FloatPairs = GHWT_FLOATS;
                if (UnknownA != 0)
                {
                    Game = GAME_GHWOR; // Also GHM, GH5, GHSH, GHVH
                }
                else
                {
                    Game = GAME_GHWT;
                }
            }
        }
        private void SetSkeletonType()
        {
            switch (NumBones)
            {
                case 2:
                case 3:
                    SkeletonType = SKELETON_CAMERA;
                    break;
                case 115:
                    SkeletonType = SKELETON_DMC_SINGER;
                    break;
                case 118:
                    SkeletonType = SKELETON_GHA_SINGER;
                    break;
                case 121:
                    SkeletonType = SKELETON_GH3_SINGER;
                    break;
                case 125:
                    SkeletonType = SKELETON_GH3_GUITARIST;
                    break;
                case 128:
                    if (Version == VERSION_OLD)
                    {
                        SkeletonType = SKELETON_STEVE;
                    }
                    else
                    {
                        SkeletonType = SKELETON_WT_ROCKER;
                    }
                    break;
                default:
                    SkeletonType = SKELETON_OTHER;
                    break;
            }
        }
        /// <summary>
        /// Reads the animation data from the SKA file.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        private void ReadAnimation(MemoryStream stream)
        {
            stream.Seek(OffsetAnim, SeekOrigin.Begin);
            Version = _rw.ReadInt32(stream);
            Flags = _rw.ReadInt32(stream);
            Duration = _rw.ReadFloat(stream);
            // Read Bone data
            NumBones = _rw.ReadUInt16(stream);
            QuatFrames = _rw.ReadUInt16(stream);
            FloatValues = new float[FloatPairs][];
            for (int i = 0; i < FloatPairs; i++)
            {
                FloatValues[i] = new float[FLOAT_VALS];
                for (int j = 0; j < FLOAT_VALS; j++)
                {
                    FloatValues[i][j] = _rw.ReadFloat(stream);
                }
            }
            TransFrames = _rw.ReadUInt16(stream);
            CustomKeyCount = _rw.ReadUInt16(stream);
            OffsetCustomKeys = _rw.ReadInt32(stream);
            OffsetQuatFrames = _rw.ReadInt32(stream);
            OffsetTransFrames = _rw.ReadInt32(stream);
            OffsetQuatBoneSize = _rw.ReadInt32(stream);
            OffsetTransBoneSize = _rw.ReadInt32(stream);
        }

        /// <summary>
        /// Reads the header of the "new" style SKA file.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        private void ReadNewHeader(MemoryStream stream)
        {
            OffsetAnim = _rw.ReadInt32(stream);
            UnknownA = _rw.ReadInt32(stream);
            OffsetBonePoint = _rw.ReadInt32(stream);
            OffsetUnknownB = _rw.ReadInt32(stream);
            OffsetUnknownC = _rw.ReadInt32(stream);
            OffsetPartialAnim = _rw.ReadInt32(stream);
            OffsetUnknownD = _rw.ReadInt32(stream);
        }

        /// <summary>
        /// Reads the header of the "old" style SKA file.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        private void ReadOldHeader(MemoryStream stream)
        {
            OffsetUnknownA = _rw.ReadInt32(stream);
            FileSize = _rw.ReadInt32(stream);
            OffsetAnim = _rw.ReadInt32(stream);
            UnknownA = _rw.ReadInt32(stream);
            OffsetBonePoint = _rw.ReadInt32(stream);
            OffsetPartialAnim = _rw.ReadInt32(stream);
            OffsetUnknownD = _rw.ReadInt32(stream);
        }

        private void ReadBonePointers(MemoryStream stream)
        {
            stream.Seek(OffsetBonePoint, SeekOrigin.Begin);
            long pointerBlockStart = stream.Position;
            List<int> pointerBlockOffsets = new List<int>();
            for (int i = 0; i < POINTER_BLOCK_AMOUNT; i++)
            {
                pointerBlockOffsets.Add(_rw.ReadInt32(stream) + (int)pointerBlockStart);
            }
            foreach (var offset in pointerBlockOffsets)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                for (int i = 0; i < NumBones; i++)
                {
                    int bonePointer = _rw.ReadInt32(stream);
                    if (bonePointer != -1)
                    {
                        if (!BonePointerData.ContainsKey(i))
                        {
                            BonePointerData.Add(i, new List<(ushort frame, int position)>());
                        }
                        int bonePos = bonePointer + OffsetQuatFrames;
                        long streamPosition = stream.Position;
                        stream.Seek(bonePos, SeekOrigin.Begin); // Go to the position in the file and read the frame time
                        ushort quatTime = GetBoneTimeFromPointer(stream, bonePos);
                        stream.Seek(streamPosition, SeekOrigin.Begin);
                        BonePointerData[i].Add((quatTime, bonePos));
                    }
                }
            }
        }
        private ushort GetBoneTimeFromPointer(MemoryStream stream, int bonePos)
        {
            ushort quatTime;
            if (HasCompFlags)
            {
                quatTime = ReadQuatTime(stream, TimeMask, LargeTime).quatTime;
            }
            else
            {
                quatTime = _rw.ReadUInt16(stream);
            }
            return quatTime;
        }
        private void ReadQuatBoneSize(MemoryStream stream)
        {
            stream.Seek(OffsetQuatBoneSize, SeekOrigin.Begin);
            for (byte i = 0; i < NumBones; i++)
            {
                QuatBoneSizes.Add(i, _rw.ReadUInt16(stream));
            }
        }

        private void ReadTransBoneSize(MemoryStream stream)
        {
            stream.Seek(OffsetTransBoneSize, SeekOrigin.Begin);
            for (byte i = 0; i < NumBones; i++)
            {
                TransBoneSizes.Add(i, _rw.ReadUInt16(stream));
            }
        }

        private void ReadPartialAnim(MemoryStream stream)
        {
            stream.Seek(OffsetPartialAnim, SeekOrigin.Begin);
            uint partialAnimFlags = _rw.ReadUInt32(stream);
            string boneFlags = "";
            for (int i = 0; i <= BONE_INTS; i++)
            {
                string boneFlag = Convert.ToString(_rw.ReadUInt32(stream), 2).PadLeft(32, '0');
                char[] boneFlagChars = boneFlag.ToArray();
                Array.Reverse(boneFlagChars);
                for (int j = 0; j < boneFlagChars.Length; j++)
                {
                    int boneIndex = (i * 32) + j;
                    if (boneFlagChars[j] == '1' && boneIndex < NumBones)
                    {
                        AnimFlags.Add(boneIndex);
                    }
                }
            }
        }
        /// <summary>
        /// Reads the quaternion data from the SKA file.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        private void ReadQuatData(MemoryStream stream)
        {
            stream.Seek(OffsetQuatFrames, SeekOrigin.Begin);
            (TimeMask, LargeTime) = CalculateTimeMask();
            for (int i = 0; i < NumBones; i++)
            {
                if (QuatBoneSizes.ContainsKey(i) && QuatBoneSizes[i] != 0)
                {
                    long boneStart = stream.Position;
                    long boneEnd = boneStart + QuatBoneSizes[i];
                    QuatData.Add(i, new List<BoneFrameQuat>());
                    while (stream.Position < boneEnd)
                    {
                        ushort quatTime;
                        short quatX;
                        short quatY;
                        short quatZ;
                        if (HasCompFlags)
                        {
                            (quatTime, ushort compFlags) = ReadQuatTime(stream, TimeMask, LargeTime);
                            (bool negW, bool compFlag, bool compX, bool compY, bool compZ) = ParseCompFlags(compFlags);
                            if (compFlag)
                            {
                                if (!(compX || compY || compZ))
                                {
                                    quatX = _rw.ReadUInt8(stream);
                                    quatY = _rw.ReadUInt8(stream);
                                    quatZ = _rw.ReadUInt8(stream);
                                    ReadWrite.MoveToModX(stream, 2);
                                }
                                else
                                {
                                    (quatX, quatY, quatZ) = ReadQuatCompXYZ(stream, compX, compY, compZ);
                                }
                            }
                            else
                            {
                                (quatX, quatY, quatZ) = ReadQuatUncompXYZ(stream);
                            }
                        }
                        else
                        {
                            quatTime = _rw.ReadUInt16(stream);
                            (quatX, quatY, quatZ) = ReadQuatUncompXYZ(stream);
                            if (quatTime == DEADDEAD)
                            {
                                boneEnd += 8; // For all bytes just read
                                continue;
                            }
                        }
                        QuatData[i].Add(new BoneFrameQuat((ushort)quatTime, quatX, quatY, quatZ));
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the time mask and determines if the time format is large.
        /// The time mask is used to extract time data from a field, and the format is considered large 
        /// if specific conditions are met (all flags are true).
        /// </summary>
        /// <returns>A tuple consisting of the time mask and a boolean indicating if the time format is large.</returns>
        private (ushort timeMask, bool largeTime) CalculateTimeMask()
        {
            bool largeTime;
            if (UnknownA == -1 || UnknownA == 0)
            {
                largeTime = HasCompFlags && HasBigTime;
            }
            else
            {
                largeTime = false;
            }
            ushort timeMask;

            if (largeTime)
            {
                timeMask = 0;
            }
            else if (HasCompFlags && (HasNoSepTime || !HasBigTime))
            {
                timeMask = FULL_TIME_MASK;
            }
            else
            {
                timeMask = 0;
            }

            return (timeMask, largeTime);
        }

        /// <summary>
        /// Reads the quaternion time and compression flags from the SKA file.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        /// <param name="timeMask">The time mask used to extract time data from a field.</param>
        /// <param name="largeTime">A boolean indicating if the time format is large.</param>
        /// <returns>A tuple consisting of the quaternion time and the compression flags.</returns>
        private (ushort quatTime, ushort compFlags) ReadQuatTime(MemoryStream stream, ushort timeMask, bool largeTime)
        {
            ushort quatTime;
            ushort compFlags;
            if (largeTime)
            {
                quatTime = _rw.ReadUInt16(stream);
                compFlags = _rw.ReadUInt16(stream);
            }
            else
            {
                compFlags = _rw.ReadUInt16(stream);
                quatTime = (ushort)(compFlags & FULL_TIME_MASK);
                compFlags -= quatTime;
            }
            return (quatTime, compFlags);
        }

        /// <summary>
        /// Parses the compression flags for quaternion data.
        /// </summary>
        /// <param name="compFlags">The compression flags.</param>
        /// <returns>A tuple consisting of the negW flag, compFlag, compX, compY, and compZ flags.</returns>
        private (bool negW, bool compFlag, bool compX, bool compY, bool compZ) ParseCompFlags(int compFlags)
        {
            bool negW = (compFlags & NEG_W) != 0;
            bool compFlag = (compFlags & COMP_FLAG) != 0;
            bool compX = (compFlags & COMP_X) != 0;
            bool compY = (compFlags & COMP_Y) != 0;
            bool compZ = (compFlags & COMP_Z) != 0;
            return (negW, compFlag, compX, compY, compZ);
        }

        /// <summary>
        /// Reads the compressed XYZ values for quaternion data.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        /// <param name="compX">A boolean indicating if the X component is compressed.</param>
        /// <param name="compY">A boolean indicating if the Y component is compressed.</param>
        /// <param name="compZ">A boolean indicating if the Z component is compressed.</param>
        /// <returns>A tuple consisting of the compressed X, Y, and Z values.</returns>
        private (short quatX, short quatY, short quatZ) ReadQuatCompXYZ(MemoryStream stream, bool compX, bool compY, bool compZ)
        {
            short quatX = ReadCompressedQuat(stream, compX);
            short quatY = ReadCompressedQuat(stream, compY);
            short quatZ = ReadCompressedQuat(stream, compZ);
            ReadWrite.MoveToModX(stream, 2);
            return (quatX, quatY, quatZ);
        }

        /// <summary>
        /// Reads the uncompressed XYZ values for quaternion data.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        /// <returns>A tuple consisting of the uncompressed X, Y, and Z values.</returns>
        private (short quatX, short quatY, short quatZ) ReadQuatUncompXYZ(MemoryStream stream)
        {
            short quatX = _rw.ReadInt16(stream);
            short quatY = _rw.ReadInt16(stream);
            short quatZ = _rw.ReadInt16(stream);

            return (quatX, quatY, quatZ);
        }

        /// <summary>
        /// Reads a compressed quaternion value.
        /// </summary>
        /// <param name="stream">The memory stream containing the SKA file data.</param>
        /// <param name="quatComp">A boolean indicating if the quaternion value is compressed.</param>
        /// <returns>The compressed quaternion value.</returns>
        private short ReadCompressedQuat(MemoryStream stream, bool quatComp)
        {
            short quat;
            if (quatComp)
            {
                quat = _rw.ReadUInt8(stream);
            }
            else
            {
                ReadWrite.MoveToModX(stream, 2);
                quat = _rw.ReadInt16(stream);
            }
            return quat;
        }

        private void ReadTransData(MemoryStream stream)
        {
            stream.Seek(OffsetTransFrames, SeekOrigin.Begin);
            bool firstBone = true;
            for (int i = 0; i < NumBones; i++)
            {
                if (TransBoneSizes.ContainsKey(i) && TransBoneSizes[i] != 0)
                {
                    long boneStart = stream.Position;
                    long boneEnd = boneStart + TransBoneSizes[i];
                    TransData.Add(i, new List<BoneFrameTrans>());
                    while (stream.Position < boneEnd)
                    {
                        ushort transTime;
                        float transX;
                        float transY;
                        float transZ;
                        if (HasTransFloatTime)
                        {
                            (transTime, transX, transY, transZ) = ReadUncompTransData(stream);
                        }
                        else
                        {
                            (transTime, transX, transY, transZ) = ReadCompTransData(stream, ref firstBone);
                        }
                        TransData[i].Add(new BoneFrameTrans(transTime, transX, transY, transZ));
                    }
                }
            }
        }

        private (ushort transTime, float transX, float transY, float transZ) ReadUncompTransData(MemoryStream stream)
        {
            float realTime = _rw.ReadFloat(stream);
            float transX = _rw.ReadFloat(stream);
            float transY = _rw.ReadFloat(stream);
            float transZ = _rw.ReadFloat(stream);

            // Check if realTime is within ushort range and is an integer value.
            if (realTime < 0 || realTime > ushort.MaxValue || realTime != (float)(ushort)realTime)
            {
                throw new Exception("realTime is either out of ushort range or not an integer value.");
            }

            ushort transTime = (ushort)realTime;

            return (transTime, transX, transY, transZ);
        }

        private (ushort transTime, float transX, float transY, float transZ) ReadCompTransData(MemoryStream stream, ref bool firstBone)
        {
            byte flagTime = _rw.ReadUInt8(stream);
            ushort longTime = _rw.ReadUInt16(stream);
            byte zero = _rw.ReadUInt8(stream);

            ushort realTime;
            if (flagTime == 0)
            {
                realTime = longTime;
            }
            else
            {
                realTime = (byte)(flagTime - TRANS_FLAGTIME_MAX);
            }

            if (firstBone)
            {
                firstBone = false;
                UnknownFloatA = _rw.ReadFloat(stream);
                UnknownFloatB = _rw.ReadFloat(stream);
                UnknownFloatC = _rw.ReadFloat(stream);
                if (UnknownFloatA != 0 || UnknownFloatB != 0 || UnknownFloatC != 0)
                {
                    throw new Exception("Unknown floats are not 0. This is unexpected.");
                }
            }

            float transX = _rw.ReadFloat(stream);
            float transY = _rw.ReadFloat(stream);
            float transZ = _rw.ReadFloat(stream);

            return (realTime, transX, transY, transZ);
        }
        public void ProcessCustomKeys(MemoryStream stream)
        {
            stream.Seek(OffsetCustomKeys, SeekOrigin.Begin);
            for (int i = 0; i < CustomKeyCount; i++)
            {
                float keyTime = _rw.ReadFloat(stream);
                int keyType = _rw.ReadInt32(stream);
                int keyValue = _rw.ReadInt32(stream);
                float keyMod;
                if (keyType == 1)
                {
                    keyMod = _rw.ReadFloat(stream);
                }
                else
                {
                    keyMod = 0;
                    throw new NotImplementedException($"Camera key type {keyType} not yet supported.");
                }
                CustomKeys.Add(new CustomKey(keyTime, keyType, keyValue, keyMod));
            }
        }
        /// <summary>
        /// Create the Custom Keys for the SKA file and writes them to a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void MakeCustomKeys(MemoryStream stream)
        {
            foreach (var key in CustomKeys)
            {
                _rw.WriteFloat(stream, key.KeyTime);
                _rw.WriteInt32(stream, key.KeyType);
                _rw.WriteInt32(stream, key.KeyValue);
                if (key.KeyType == 1)
                {
                    _rw.WriteFloat(stream, key.KeyMod);
                }
                else
                {
                    throw new NotImplementedException($"Camera key type {key.KeyType} not yet supported.");
                }
            }
        }

        private void WritePointerBlock(MemoryStream stream, int headerOffset)
        {
            int baseOffset = 80;
            List<int> pointerBlockOffsets = new List<int>();
            List<int> pointerData = new List<int>();
            for (int i = 0; i < POINTER_BLOCK_AMOUNT; i++)
            {
                pointerBlockOffsets.Add(i * NumBones * 4 + baseOffset);
                for (int j = 0; j < NewBones; j++)
                {
                    var oldPointerBone = BonePointerData[j][i].frame;
                    var newPointerValue = NewBonePointers[j][oldPointerBone];
                    pointerData.Add(newPointerValue);
                }
            }
            pointerBlockOffsets.AddRange(pointerData);
            foreach (var offset in pointerBlockOffsets)
            {
                _rw.WriteInt32(stream, offset);
            }

        }
        /// <summary>
        /// Checks the flags of the SKA file.
        /// </summary>
        private void CheckFlags()
        {
            HasTransFloatTime = (Flags & TRANS_FLOAT_TIME) != 0;
            HasNoSepTime = (Flags & NO_SEP_TIME) != 0;
            HasCompFlags = (Flags & COMP_FLAGS) != 0;
            HasPartialAnim = (Flags & PARTIAL_ANIM) != 0;
            HasBigTime = (Flags & BIG_TIME) != 0;
            IsSingleFrame = (Flags & SINGLE_FRAME) != 0;
        }

        static Dictionary<int, ushort> CreateBlankBoneDict(int x, int max = -1)
        {
            Dictionary<int, ushort> dictionary = new Dictionary<int, ushort>();
            if (max == -1)
            {
                max = x;
            }
            for (int i = 0; i < x; i++)
            {
                dictionary[i] = 0;
            }

            if (x != max)
            {
                for (int i = x; i < max; i++)
                {
                    dictionary[i] = 1;
                }
            }

            return dictionary;
        }
        private List<BoneFrameQuat> WriteNewQuatData(List<BoneFrameQuat> oldData, float multiplier)
        {
            List<BoneFrameQuat> newData = new List<BoneFrameQuat>();
            BoneFrameQuat previousFrame = null;
            BoneFrameQuat previousAdded = null;
            int count = 1;

            foreach (var frame in oldData)
            {
                ushort frameTime = frame.FrameTime;
                short quatX = (short)Math.Round(frame.QuatX * multiplier);
                short quatY = (short)Math.Round(frame.QuatY * multiplier);
                short quatZ = (short)Math.Round(frame.QuatZ * multiplier);

                BoneFrameQuat currentFrame = new BoneFrameQuat(frameTime, quatX, quatY, quatZ);
                bool sameAsPrev = previousFrame != null &&
                                  quatX == previousFrame.QuatX &&
                                  quatY == previousFrame.QuatY &&
                                  quatZ == previousFrame.QuatZ;

                if (!sameAsPrev || count < 2)
                {
                    if (frameTime != 0 && previousAdded.FrameTime < previousFrame.FrameTime)
                    {
                        newData.Add(previousFrame);
                    }
                    newData.Add(currentFrame);
                    previousAdded = currentFrame;
                }

                if (sameAsPrev || frameTime == 0)
                {
                    count++;
                }
                else
                {
                    count = 1;
                }
                previousFrame = currentFrame;
            }
            if (previousAdded.FrameTime < previousFrame.FrameTime)
            {
                newData.Add(previousFrame);
            }
            return newData;
        }
        /*
        private List<BoneFrameQuat> WriteNewQuatData(List<BoneFrameQuat> oldData, float multiplier)
        {
            List<BoneFrameQuat> newData = new List<BoneFrameQuat>();
            ushort prevTime = 0;
            short prevX = 0;
            short prevY = 0;
            short prevZ = 0;
            foreach (var frame in oldData)
            {
                ushort frameTime = frame.FrameTime;
                short quatX = (short)Math.Round(frame.QuatX * multiplier);
                short quatY = (short)Math.Round(frame.QuatY * multiplier);
                short quatZ = (short)Math.Round(frame.QuatZ * multiplier);
                bool sameAsPrev = quatX == prevX && quatY == prevY && quatZ == prevZ;

                newData.Add(new BoneFrameQuat(frameTime, quatX, quatY, quatZ));
            }
            return newData;
        }
        */
        private List<BoneFrameTrans> WriteNewTransData(List<BoneFrameTrans> oldData)
        {
            List<BoneFrameTrans> newData = new List<BoneFrameTrans>();
            BoneFrameTrans previousFrame = null;
            BoneFrameTrans previousAdded = null;
            int count = 1;

            foreach (var frame in oldData)
            {
                ushort frameTime = frame.FrameTime;

                float transX = frame.TransX;
                float transY = frame.TransY;
                float transZ = frame.TransZ;

                BoneFrameTrans currentFrame = new BoneFrameTrans(frameTime, transX, transY, transZ);
                bool sameAsPrev = previousFrame != null &&
                                  Math.Abs(transX - previousFrame.TransX) < float.Epsilon &&
                                  Math.Abs(transY - previousFrame.TransY) < float.Epsilon &&
                                  Math.Abs(transZ - previousFrame.TransZ) < float.Epsilon;

                if (!sameAsPrev || count < 2)
                {
                    newData.Add(currentFrame);
                    previousAdded = currentFrame;
                }

                if (sameAsPrev || frameTime == 0)
                {
                    count++;
                }
                else
                {
                    count = 1;
                }
                previousFrame = currentFrame;
            }
            if (previousAdded.FrameTime < previousFrame.FrameTime)
            {
                newData.Add(previousFrame);
            }
            return newData;
        }
        private short GetPs2TransData(float transValue)
        {
            if (transValue < 0)
            {
                return (short)Math.Ceiling(transValue * 256);
            }
            else
            {
                return (short)Math.Floor(transValue * 256);
            }
        }
        private List<BoneFrameTransCompressed> WriteCompressedTransDataPs2(List<BoneFrameTrans> oldData)
        {
            List<BoneFrameTransCompressed> newData = new List<BoneFrameTransCompressed>();
            BoneFrameTransCompressed previousFrame = null;
            int count = 0;

            foreach (var frame in oldData)
            {
                ushort frameTime = frame.FrameTime;

                short transX = GetPs2TransData(frame.TransX);
                short transY = GetPs2TransData(frame.TransY);
                short transZ = GetPs2TransData(frame.TransZ);

                BoneFrameTransCompressed currentFrame = new BoneFrameTransCompressed(frameTime, transX, transY, transZ);

                newData.Add(currentFrame);
            }
            return newData;
        }
        private void WriteCompressedTrans(MemoryStream stream, BoneFrameTrans frame, ref bool firstFrame)
        {
            ushort frameTime = frame.FrameTime;
            float transX = frame.TransX;
            float transY = frame.TransY;
            float transZ = frame.TransZ;

            byte[] compTime;
            byte[] realTime;
            byte[] zero = [0];

            if (frameTime < TRANS_FLAGTIME_MAX)
            {
                compTime = [(byte)(frameTime + TRANS_FLAGTIME_MAX)];
                realTime = [0, 0];
            }
            else
            {
                compTime = [0];
                realTime = BitConverter.GetBytes(frameTime);
            }
            _rw.WriteAndMaybeFlipBytes(stream, compTime);
            _rw.WriteAndMaybeFlipBytes(stream, realTime);
            _rw.WriteAndMaybeFlipBytes(stream, zero);

            if (firstFrame)
            {
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(UnknownFloatA));
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(UnknownFloatB));
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(UnknownFloatC));
                firstFrame = false;
            }

            _rw.WriteFloat(stream, transX);
            _rw.WriteFloat(stream, transY);
            _rw.WriteFloat(stream, transZ);
        }
        private void WriteCompressedTransPs2(MemoryStream stream, BoneFrameTransCompressed frame)
        {
            ushort frameTime = frame.FrameTime;
            short transX = frame.TransX;
            short transY = frame.TransY;
            short transZ = frame.TransZ;

            byte[] compTime;
            byte[] realTime;
            byte[] zero = [0];

            if (frameTime < TRANS_FLAGTIME_MAX)
            {
                compTime = [(byte)(frameTime + TRANS_FLAGTIME_MAX)];
                _rwPs2.WriteAndMaybeFlipBytes(stream, compTime);
            }
            else
            {
                compTime = [0];
                realTime = BitConverter.GetBytes(frameTime);
                _rwPs2.WriteAndMaybeFlipBytes(stream, compTime);
                _rwPs2.WriteAndMaybeFlipBytes(stream, realTime);
            }

            _rwPs2.WriteAndMaybeFlipBytes(stream, _rwPs2.ValueHex(transX));
            _rwPs2.WriteAndMaybeFlipBytes(stream, _rwPs2.ValueHex(transY));
            _rwPs2.WriteAndMaybeFlipBytes(stream, _rwPs2.ValueHex(transZ));
        }
        private void WriteCompressedQuat(MemoryStream stream, BoneFrameQuat frame)
        {
            ushort frameTime = frame.FrameTime;
            ushort compFlags = 0;
            short quatX = frame.QuatX;
            short quatY = frame.QuatY;
            short quatZ = frame.QuatZ;

            byte[] xBytes;
            byte[] yBytes;
            byte[] zBytes;

            xBytes = CompressComponent(quatX, ref compFlags, COMP_X);
            yBytes = CompressComponent(quatY, ref compFlags, COMP_Y);
            zBytes = CompressComponent(quatZ, ref compFlags, COMP_Z);

            if (quatX == 0 && quatY == 0 && quatZ == 0)
            {
                compFlags = COMP_FLAG;
            }
            else if (compFlags != 0)
            {
                compFlags += COMP_FLAG;
            }
            if (HasBigTime)
            {
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(frameTime));
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(compFlags));
            }
            else
            {
                frameTime += compFlags;
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(frameTime));
            }
            _rw.PadStreamTo(stream, 2);
            _rw.WriteAndMaybeFlipBytes(stream, xBytes);

            if (yBytes.Length != 1)
            {
                _rw.PadStreamTo(stream, 2);
            }
            _rw.WriteAndMaybeFlipBytes(stream, yBytes);

            if (zBytes.Length != 1)
            {
                _rw.PadStreamTo(stream, 2);
            }
            _rw.WriteAndMaybeFlipBytes(stream, zBytes);

            _rw.PadStreamTo(stream, 2);
        }
        private void WritePs2Quat(MemoryStream stream, BoneFrameQuat frame)
        {
            ushort frameTime = frame.FrameTime;
            ushort compFlags = 0;
            short quatX;
            short quatY;
            short quatZ;

            quatX = frame.QuatX;
            quatY = frame.QuatY;
            quatZ = frame.QuatZ;

            byte[] xBytes;
            byte[] yBytes;
            byte[] zBytes;

            xBytes = CompressComponent(quatX, ref compFlags, COMP_X);
            yBytes = CompressComponent(quatY, ref compFlags, COMP_Y);
            zBytes = CompressComponent(quatZ, ref compFlags, COMP_Z);

            bool allZero = quatX == 0 && quatY == 0 && quatZ == 0;

            if (allZero)
            {
                compFlags = COMP_FLAG;
            }
            else if (compFlags != 0)
            {
                compFlags += COMP_FLAG;
            }
            if (HasBigTime)
            {
                _rwPs2.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(frameTime));
                _rwPs2.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(compFlags));
            }
            else
            {
                frameTime += compFlags;
                _rwPs2.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(frameTime));
            }

            if (allZero)
            {
                _rwPs2.WriteAndMaybeFlipBytes(stream, xBytes); // Not specifically x, but if all three flags are 0, there is just one 0 value
            }
            else
            {
                _rwPs2.WriteAndMaybeFlipBytes(stream, xBytes);
                _rwPs2.WriteAndMaybeFlipBytes(stream, yBytes);
                _rwPs2.WriteAndMaybeFlipBytes(stream, zBytes);
            }

        }
        private byte[] CompressComponent(short value, ref ushort compFlags, ushort flag)
        {
            if (value >= 0 && value <= 0xff)
            {
                compFlags += flag;
                return [(byte)value];
            }
            else
            {
                return BitConverter.GetBytes(value);
            }
        }
        private void QuatDataToBytesPs2(MemoryStream quatStream, MemoryStream boneCountStream, Dictionary<int, List<BoneFrameQuat>> quatData, List<int> newAnimBones)
        {
            var newQuatSizes = CreateBlankBoneDict(NewBones);

            foreach (var bone in newAnimBones)
            {
                long boneStart = quatStream.Length;
                foreach (var frame in quatData[bone])
                {
                    WritePs2Quat(quatStream, frame);
                }
                long boneLength = quatStream.Length - boneStart;
                if (boneLength > ushort.MaxValue)
                {
                    throw new Exception("Bone length is greater than ushort max value.");
                }
                newQuatSizes[bone] = (ushort)boneLength;
            }
            for (int i = 0; i < NewBones; i++)
            {
                _rwPs2.WriteAndMaybeFlipBytes(boneCountStream, BitConverter.GetBytes(newQuatSizes[i]));
            }

            // Check if length of quatStream equals sum of the values of newQuatSizes
            if (newQuatSizes.Values.Sum(x => Convert.ToInt32(x)) != quatStream.Length)
            {
                throw new Exception("Length of Quaternion Stream does not equal sum of the values of newQuatSizes.");
            }
        }
        private void QuatDataToBytes(MemoryStream quatStream, MemoryStream boneCountStream, Dictionary<int, List<BoneFrameQuat>> quatData, List<int> newAnimBones, bool compressedData = true)
        {
            var newQuatSizes = CreateBlankBoneDict(NewBones);

            foreach (var bone in newAnimBones)
            {
                NewBonePointers[bone] = new Dictionary<ushort, int>();
                var boneEntry = NewBonePointers[bone];
                
                long boneStart = quatStream.Length;
                foreach (var frame in quatData[bone])
                {
                    boneEntry[frame.FrameTime] = (int)quatStream.Length;
                    if (compressedData)
                    {
                        WriteCompressedQuat(quatStream, frame);
                    }
                    else
                    {
                        //WriteUncompressedQuat(quatStream, frame);
                    }
                }
                long boneLength = quatStream.Length - boneStart;
                if (boneLength > ushort.MaxValue)
                {
                    throw new Exception("Bone length is greater than ushort max value.");
                }
                newQuatSizes[bone] = (ushort)boneLength;
            }
            for (int i = 0; i < NewBones; i++)
            {
                _rw.WriteAndMaybeFlipBytes(boneCountStream, BitConverter.GetBytes(newQuatSizes[i]));
            }

            // Check if length of quatStream equals sum of the values of newQuatSizes
            if (newQuatSizes.Values.Sum(x => Convert.ToInt32(x)) != quatStream.Length)
            {
                throw new Exception("Length of Quaternion Stream does not equal sum of the values of newQuatSizes.");
            }

            _rw.PadStreamTo(quatStream, SKA_BOUNDARY);
            _rw.PadStreamTo(boneCountStream, SKA_BOUNDARY);
        }
        private void TransDataToBytesPs2(MemoryStream transStream, MemoryStream boneCountStream, Dictionary<int, List<BoneFrameTransCompressed>> transData, List<int> newAnimBones)
        {
            var newTransSizes = CreateBlankBoneDict(NewBones);

            foreach (var bone in newAnimBones)
            {
                long boneStart = transStream.Length;
                foreach (var frame in transData[bone])
                {
                    WriteCompressedTransPs2(transStream, frame);
                }
                long boneLength = transStream.Length - boneStart;
                if (boneLength > ushort.MaxValue)
                {
                    throw new Exception("Bone length is greater than ushort max value.");
                }
                newTransSizes[bone] = (ushort)boneLength;
            }
            for (int i = 0; i < NewBones; i++)
            {
                _rwPs2.WriteAndMaybeFlipBytes(boneCountStream, BitConverter.GetBytes(newTransSizes[i]));
            }

            if (newTransSizes.Values.Sum(x => Convert.ToInt32(x)) != transStream.Length)
            {
                throw new Exception("Length of Quaternion Stream does not equal sum of the values of newTransSizes.");
            }

            _rwPs2.PadStreamTo(transStream, ANIM_BOUNDARY);
        }
        private void TransDataToBytes(MemoryStream transStream, MemoryStream boneCountStream, Dictionary<int, List<BoneFrameTrans>> transData, List<int> newAnimBones, bool compressedData)
        {
            var newTransSizes = CreateBlankBoneDict(NewBones);
            bool firstFrame = true;
            foreach (var bone in newAnimBones)
            {
                long boneStart = transStream.Length;
                foreach (var frame in transData[bone])
                {
                    if (compressedData)
                    {
                        WriteCompressedTrans(transStream, frame, ref firstFrame);
                    }
                    else
                    {
                        //WriteUncompressedQuat(quatStream, frame);
                    }
                }
                long boneLength = transStream.Length - boneStart;
                if (boneLength > ushort.MaxValue)
                {
                    throw new Exception("Bone length is greater than ushort max value.");
                }
                newTransSizes[bone] = (ushort)boneLength;
            }
            for (int i = 0; i < NewBones; i++)
            {
                _rw.WriteAndMaybeFlipBytes(boneCountStream, BitConverter.GetBytes(newTransSizes[i]));
            }

            if (newTransSizes.Values.Sum(x => Convert.ToInt32(x)) != transStream.Length)
            {
                throw new Exception("Length of Quaternion Stream does not equal sum of the values of newTransSizes.");
            }

            _rw.PadStreamTo(transStream, SKA_BOUNDARY);
            _rw.PadStreamTo(boneCountStream, ANIM_BOUNDARY);
        }
        public void MakePartialAnimPs2(MemoryStream stream, List<int> newAnimBones)
        {
            var newPartialAnim = CreateBlankBoneDict(NewBones, 96);
            _rwPs2.WriteNoFlipBytes(stream, _rwPs2.ValueHex(NewBones));
            foreach (var bone in newAnimBones)
            {
                newPartialAnim[bone] = 1;
            }
            for (int i = 0; i < BONE_INTS; i++)
            {
                int boneFlag = 0;
                for (int j = 0; j < 32; j++)
                {
                    int boneIndex = (i * 32) + j;
                    boneFlag += newPartialAnim[boneIndex] << j;
                }
                _rwPs2.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(boneFlag));
            }
        }
        public void MakePartialAnim(MemoryStream stream, List<int> newAnimBones)
        {
            var newPartialAnim = CreateBlankBoneDict(NewBones, 128);
            _rw.WriteNoFlipBytes(stream, _rw.ValueHex(NewBones));
            foreach (var bone in newAnimBones)
            {
                newPartialAnim[bone] = 1;
            }
            for (int i = 0; i <= BONE_INTS; i++)
            {
                int boneFlag = 0;
                for (int j = 0; j < 32; j++)
                {
                    int boneIndex = (i * 32) + j;
                    boneFlag += newPartialAnim[boneIndex] << j;
                }
                _rw.WriteAndMaybeFlipBytes(stream, BitConverter.GetBytes(boneFlag));
            }
        }
        public byte[]? WritePs2StyleSka(float quatMultiplier = 1) // For GH3 PS2
        {
            var oldBones = ALL_DATA[SkeletonType];
            var newBones = ALL_DATA[SKELETON_GH3_SINGER_PS2];
            NewBones = 67;

            var newAnimFlags = new List<int>();
            var newQuatData = new Dictionary<int, List<BoneFrameQuat>>();
            var newTransData = new Dictionary<int, List<BoneFrameTransCompressed>>();
            foreach (var bone in AnimFlags)
            {
                string boneName = oldBones.bonesNum[bone];
                int newBone;
                try
                {
                    newBone = newBones.bonesName[boneName];
                }
                catch
                {
                    continue;
                }
                newAnimFlags.Add(newBone);
                newQuatData[newBone] = WriteNewQuatData(QuatData[bone], quatMultiplier);
                newTransData[newBone] = WriteCompressedTransDataPs2(WriteNewTransData(TransData[bone]));
            }
            var quatFrames = newQuatData.Values.Sum(x => Convert.ToInt32(x.Count));
            var transFrames = newTransData.Values.Sum(x => Convert.ToInt32(x.Count));
            var customKeys = CustomKeys.Count;

            NewFlags = 0x068B5000;
            if (Duration > 34.11 || HasBigTime)
            {
                HasBigTime = true;
                NewFlags += BIG_TIME;
            }

            using (var totalFile = new MemoryStream())
            using (var quatBytes = new MemoryStream())
            using (var quatBoneCountBytes = new MemoryStream())
            using (var transBytes = new MemoryStream())
            using (var transBoneCountBytes = new MemoryStream())
            using (var partialAnimBytes = new MemoryStream()) // No camera support yet, so it's always assumed to have one
            {
                QuatDataToBytesPs2(quatBytes, quatBoneCountBytes, newQuatData, newAnimFlags);
                TransDataToBytesPs2(transBytes, transBoneCountBytes, newTransData, newAnimFlags);
                MakePartialAnimPs2(partialAnimBytes, newAnimFlags);
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex(VERSION_PS2)); // Should be int
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex(NewFlags));
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex(Duration));
                byte[] boneCountBytes = { 0x00, (byte)NewBones };
                _rwPs2.WriteNoFlipBytes(totalFile, boneCountBytes);
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex((ushort)quatFrames));
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex((ushort)transFrames));
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex((ushort)customKeys));
                byte[] allNegOne = { 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 5; i++)
                {
                    _rwPs2.WriteNoFlipBytes(totalFile, allNegOne);
                }
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex((int)quatBytes.Length));
                _rwPs2.WriteNoFlipBytes(totalFile, _rwPs2.ValueHex((int)transBytes.Length));
                ReadWrite.AppendStream(totalFile, quatBoneCountBytes);
                ReadWrite.AppendStream(totalFile, transBoneCountBytes);
                ReadWrite.AppendStream(totalFile, partialAnimBytes);
                ReadWrite.AppendStream(totalFile, quatBytes);
                ReadWrite.AppendStream(totalFile, transBytes);
                return totalFile.ToArray();
            }


        }
        private (Dictionary<int, List<BoneFrameQuat>> newQuatData, Dictionary<int, List<BoneFrameTrans>> newTransData, List<int> newAnimFlags, int newBones, int quatFrames, int transFrames, int customKeys) InitializeAnimationData(string convertTo, float quatMultiplier = 1)
        {
            var oldBones = ALL_DATA[SkeletonType];
            var newBones = ALL_DATA[convertTo];
            if (SkeletonType == SKELETON_CAMERA)
            {
                newBones = ALL_DATA[SKELETON_CAMERA];
            }
            
            int newBonesCount = newBones.bonesNum.Count;

            var newAnimFlags = new List<int>();
            var newQuatData = new Dictionary<int, List<BoneFrameQuat>>();
            var newTransData = new Dictionary<int, List<BoneFrameTrans>>();
            foreach (var bone in AnimFlags)
            {
                string boneName = oldBones.bonesNum[bone];
                int newBone = newBones.bonesName[boneName];
                newAnimFlags.Add(newBone);
                newQuatData[newBone] = WriteNewQuatData(QuatData[bone], quatMultiplier);
                newTransData[newBone] = WriteNewTransData(TransData[bone]);
            }

            var quatFrames = newQuatData.Values.Sum(x => x.Count);
            var transFrames = newTransData.Values.Sum(x => x.Count);
            var customKeys = CustomKeys.Count;

            return (newQuatData, newTransData, newAnimFlags, newBonesCount, quatFrames, transFrames, customKeys);
        }
        public byte[]? WriteGh3StyleSka(string convertTo, float quatMultiplier = 1) // For GH3 and GHA
        {
            if (SkeletonType == SKELETON_CAMERA)
            {
                return [];
            }
            var (newQuatData, newTransData, newAnimFlags, newBones, quatFrames, transFrames, customKeys) = InitializeAnimationData(convertTo, quatMultiplier);

            NewBones = newBones;

            // Create an array of arrays of floats where the first float is 1.0 and the rest are 0.0
            var floatValues = new float[GH3_FLOATS][];
            for (int i = 0; i < GH3_FLOATS; i++)
            {
                floatValues[i] = new float[FLOAT_VALS];
                floatValues[i][0] = 1.0f;
            }


            NewFlags = 0x068B5000;
            if (Duration > 34.11 || HasBigTime)
            {
                HasBigTime = true;
                NewFlags += BIG_TIME;
            }

            using (var totalFile = new MemoryStream())
            using (var quatBytes = new MemoryStream())
            using (var quatBoneCountBytes = new MemoryStream())
            using (var transBytes = new MemoryStream())
            using (var transBoneCountBytes = new MemoryStream())
            using (var partialAnimBytes = new MemoryStream()) // No camera support yet, so it's always assumed to have one
            {
                QuatDataToBytes(quatBytes, quatBoneCountBytes, newQuatData, newAnimFlags, true);
                TransDataToBytes(transBytes, transBoneCountBytes, newTransData, newAnimFlags, true);
                MakePartialAnim(partialAnimBytes, newAnimFlags);
                int totalFileSize = SKA_BOUNDARY + (int)quatBytes.Length + (int)transBytes.Length + (int)quatBoneCountBytes.Length + (int)transBoneCountBytes.Length + (int)partialAnimBytes.Length;
                int partialPos = totalFileSize - (int)partialAnimBytes.Length;
                int transSizePos = partialPos - (int)transBoneCountBytes.Length;
                int quatSizePos = transSizePos - (int)quatBoneCountBytes.Length;
                int transDataPos = quatSizePos - (int)transBytes.Length;
                int quatDataPos = transDataPos - (int)quatBytes.Length;
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(0)); // Should be int
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Unknown number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(totalFileSize));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(ANIM_START));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Unknown number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Bone pointers, is always 0 for old SKA files
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(partialPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Unknown number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(VERSION_OLD));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(NewFlags));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(Duration));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)NewBones));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)quatFrames));
                foreach (var floatPair in floatValues)
                {
                    foreach (var floatValue in floatPair)
                    {
                        _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(floatValue));
                    }
                }
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)transFrames));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)customKeys));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Custom Keys, not supported yet
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(quatDataPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(transDataPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(quatSizePos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(transSizePos));

                _rw.PadStreamTo(totalFile, SKA_BOUNDARY);

                ReadWrite.AppendStream(totalFile, quatBytes);
                ReadWrite.AppendStream(totalFile, transBytes);
                ReadWrite.AppendStream(totalFile, quatBoneCountBytes);
                ReadWrite.AppendStream(totalFile, transBoneCountBytes);
                ReadWrite.AppendStream(totalFile, partialAnimBytes);

                return totalFile.ToArray();
            }
        }
        public byte[]? WriteModernStyleSka(string convertTo, string game = GAME_GHWT, float quatMultiplier = 1) // For GHWT+
        {
            var (newQuatData, newTransData, newAnimFlags, newBones, quatFrames, transFrames, customKeys) = InitializeAnimationData(convertTo, quatMultiplier);

            NewBones = newBones;


            float[][] floatValues;
            
            // Use the float values provided, or use the default values if they are not provided
            if (FloatValues.Length == 4)
            {
                floatValues = FloatValues;
            }
            else
            {
                floatValues =
                [
                    [0f, 0f, 0f, 1f],
                    [-0.5f, -0.5f, -0.5f, 0.5f],
                    [0f, 0f, 0f, 1f],
                    [-0.5f, -0.5f, -0.5f, 0.5f]
                ];
            }

            if (game == GAME_GHWT)
            {
                NewFlags = (uint)(SkeletonType == SKELETON_CAMERA ? 0x06811000 : 0x068B5000);
            }
            else
            {
                NewFlags = SkeletonType == SKELETON_CAMERA ? 0x96011000 : 0x960B5000;
            }

            if (Duration > 34.11 || HasBigTime)
            {
                HasBigTime = true;
                NewFlags += BIG_TIME;
            }

            using (var totalFile = new MemoryStream())
            using (var quatBytes = new MemoryStream())
            using (var quatBoneCountBytes = new MemoryStream())
            using (var transBytes = new MemoryStream())
            using (var transBoneCountBytes = new MemoryStream())
            using (var customKeyBytes = new MemoryStream())
            using (var bonePointerBytes = new MemoryStream())
            using (var partialAnimBytes = new MemoryStream()) // No camera support yet, so it's always assumed to have one
            {
                int headerLen = SKA_BOUNDARY * 2;
                bool compressedData = game == GAME_GHWT;
                QuatDataToBytes(quatBytes, quatBoneCountBytes, newQuatData, newAnimFlags, compressedData);
                TransDataToBytes(transBytes, transBoneCountBytes, newTransData, newAnimFlags, compressedData);
                if (HasPartialAnim)
                {
                    MakePartialAnim(partialAnimBytes, newAnimFlags);
                }
                MakeCustomKeys(customKeyBytes);
                if (BonePointerData.Count > 0)
                {
                    WritePointerBlock(bonePointerBytes, headerLen);
                }

                int quatLen = (int)quatBytes.Length;
                int transLen = (int)transBytes.Length;
                int quatBoneCountLen = (int)quatBoneCountBytes.Length;
                int transBoneCountLen = (int)transBoneCountBytes.Length;
                int partialAnimLen = (int)partialAnimBytes.Length;
                int customKeyLen = (int)customKeyBytes.Length;
                int bonePointerLen = (int)bonePointerBytes.Length;

                int totalFileSize = headerLen + quatLen + transLen + quatBoneCountLen + transBoneCountLen + partialAnimLen + customKeyLen + bonePointerLen;

                int currPos = totalFileSize;

                currPos -= bonePointerLen;

                int bonePointPos = currPos;
                if (bonePointerLen == 0)
                {
                    bonePointPos = -1;
                }

                currPos -= partialAnimLen;

                int partialPos = currPos;
                if (partialAnimLen == 0)
                {
                    partialPos = -1;
                }

                currPos -= customKeyLen;

                int customKeysPos = currPos;
                if (customKeys == 0)
                {
                    customKeysPos = -1;
                }

                currPos -= transBoneCountLen;
                int transSizePos = currPos;
                int quatSizePos = transSizePos - (int)quatBoneCountBytes.Length;
                int transDataPos = quatSizePos - (int)transBytes.Length;
                int quatDataPos = transDataPos - (int)quatBytes.Length;
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(totalFileSize));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(ANIM_START));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(0)); // Should be int
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(bonePointPos)); // BonePointer number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Unk_b number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Unk_c number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(partialPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(-1)); // Unk_d number
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(VERSION_NEW));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(NewFlags));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(Duration));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)NewBones));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)quatFrames));
                foreach (var floatPair in floatValues)
                {
                    foreach (var floatValue in floatPair)
                    {
                        _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(floatValue));
                    }
                }
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)transFrames));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex((ushort)customKeys));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(customKeysPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(quatDataPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(transDataPos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(quatSizePos));
                _rw.WriteNoFlipBytes(totalFile, _rw.ValueHex(transSizePos));

                _rw.PadStreamTo(totalFile, SKA_BOUNDARY);

                ReadWrite.AppendStream(totalFile, quatBytes);
                ReadWrite.AppendStream(totalFile, transBytes);
                ReadWrite.AppendStream(totalFile, quatBoneCountBytes);
                ReadWrite.AppendStream(totalFile, transBoneCountBytes);
                ReadWrite.AppendStream(totalFile, customKeyBytes);
                ReadWrite.AppendStream(totalFile, partialAnimBytes);
                ReadWrite.AppendStream(totalFile, bonePointerBytes);

                return totalFile.ToArray();
            }            
        }
    }
}
