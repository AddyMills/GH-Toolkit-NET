using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Methods;
using static GH_Toolkit_Core.QB.QBConstants;

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

        // Game constants
        private const int GH3_FLOATS = 2; // Number of float value arrays per GH3/GHA ska file
        private const int GHWT_FLOATS = 4; // Number of float value arrays per GHWT and up ska file
        private const int FLOAT_VALS = 4; // Number of float values per SKA file
        // Header data
        public string Game { get; set; }
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
        public uint Flags { get; set; }
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
        public Dictionary<byte, ushort> QuatBoneSizes { get; set; } = new Dictionary<byte, ushort>(); // Dictionary of bone sizes for quaternion frames per bone
        public Dictionary<byte, ushort> TransBoneSizes { get; set; } = new Dictionary<byte, ushort>(); // Dictionary of bone sizes for translation frames per bone
        internal int FloatPairs { get; set; } // Number of float value pairs
        internal ReadWrite _rw;
        internal bool IsTransFloatTime { get; set; }
        internal bool IsNoSepTime { get; set; }
        internal bool IsCompFlags { get; set; }
        internal bool IsPartialAnim { get; set; }
        internal bool IsBigTime { get; set; }
        internal bool IsSingleFrame { get; set; }

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
                CheckFlags();
                ReadAnimation(stream);
                ReadQuatBoneSize(new MemoryStream(skaData));
                ReadTransBoneSize(new MemoryStream(skaData));
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
                Game = GAME_GHWT;
                FileSize = gameCheck;
                ReadNewHeader(stream);
                FloatPairs = GHWT_FLOATS;
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
            Flags = _rw.ReadUInt32(stream);
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

        private void ReadBonePoint(MemoryStream stream)
        {
            stream.Seek(OffsetBonePoint, SeekOrigin.Begin);
            throw new NotImplementedException();
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
            
        }
        /// <summary>
        /// Checks the flags of the SKA file.
        /// </summary>
        private void CheckFlags()
        {
            IsTransFloatTime = (Flags & TRANS_FLOAT_TIME) != 0;
            IsNoSepTime = (Flags & NO_SEP_TIME) != 0;
            IsCompFlags = (Flags & COMP_FLAGS) != 0;
            IsPartialAnim = (Flags & PARTIAL_ANIM) != 0;
            IsBigTime = (Flags & BIG_TIME) != 0;
            IsSingleFrame = (Flags & SINGLE_FRAME) != 0;
        }
        


    }
}
