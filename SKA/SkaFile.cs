using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Methods;

namespace GH_Toolkit_Core.SKA
{
    internal class SkaFile
    {
        public string Game { get; set; }
        public int OffsetUnknownA { get; set; } // In GH3/GHA ska files
        public int FileSize { get; set; }
        public int OffsetAnim { get; set; }
        public int Version { get; set; } // 0 for GH3/GHA/GHWT, 410 for GHM/GHSH/GHVH, 500 for GH5/GHWoR
        public int OffsetBonePoint { get; set; } // Offset to the bone pointer data
        public int OffsetUnknownB { get; set; }
        public int OffsetUnknownC { get; set; }
        public int OffsetPartialAnim { get; set; } // Offset to the partial animation data
        public int OffsetUnknownD { get; set; }
        internal ReadWrite rw;
        public SkaFile(string filePath, string endian)
        {
            rw = new ReadWrite(endian);
            byte[] skaData = File.ReadAllBytes(filePath);
            using (MemoryStream stream = new MemoryStream(skaData))
            {
                int gameCheck = rw.ReadInt32(stream);
                if (gameCheck == 0)
                {
                    ReadOldHeader(stream);
                }
                else 
                {
                    FileSize = gameCheck;
                    ReadNewHeader(stream);
                }
            }
        }
        private void ReadNewHeader(MemoryStream stream)
        {
            OffsetAnim = rw.ReadInt32(stream);
            Version = rw.ReadInt32(stream);
            OffsetBonePoint = rw.ReadInt32(stream);
            OffsetUnknownB = rw.ReadInt32(stream);
            OffsetUnknownC = rw.ReadInt32(stream);
            OffsetPartialAnim = rw.ReadInt32(stream);
            OffsetUnknownD = rw.ReadInt32(stream);
        }
        private void ReadOldHeader(MemoryStream stream)
        {
            OffsetUnknownA = rw.ReadInt32(stream);
            FileSize = rw.ReadInt32(stream);
            OffsetAnim = rw.ReadInt32(stream);
            Version = rw.ReadInt32(stream);
            OffsetBonePoint = rw.ReadInt32(stream);
            OffsetPartialAnim = rw.ReadInt32(stream);
            OffsetUnknownD = rw.ReadInt32(stream);
        }
    }
}
