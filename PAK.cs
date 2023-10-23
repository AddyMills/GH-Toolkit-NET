using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core
{
    public class PAK
    {
        private static bool FlipCheck(string endian)
        {
            bool big_endian;
            bool little_arc;
            if (endian == "little")
            {
                big_endian = false;
            }
            else
            {
                big_endian = true;
            }
            if (BitConverter.IsLittleEndian)
            {
                little_arc = true;
            }
            else
            {
                little_arc = false;
            }
            return big_endian && little_arc;
        }
        [DebuggerDisplay("Entry: {FullName}")]
        public class PakEntry
        {
            public object Extension { get; set; } // This can be either uint or string
            public uint StartOffset { get; set; }
            public uint FileSize { get; set; }
            public object AssetContext { get; set; } // This can be either uint or string
            public object FullName { get; set; } // This can be either uint or string
            public object NameNoExt { get; set; } // This can be either uint or string
            public uint Parent { get; set; }
            public uint Flags { get; set; }
            public byte[] EntryData { get; set; }
        }

        public static List<PakEntry> ExtractPAK(byte[] PakBytes, string endian = "big", string songName = "")
        {
            const int ChunkSize = 32;  // Size of each read chunk for PAK Header
            const int UnitSize = 4;    // Size of each unit in one entry
            bool flip_bytes = FlipCheck(endian);
            MemoryStream stream = new MemoryStream(PakBytes);
            // stream.Write(PakBytes, 0, PakBytes.Length);
            List<PakEntry> PakList = new List<PakEntry>();
            Dictionary<uint, string> headers = DebugReader.MakeDictFromName(songName);
            byte[] ReadAndMaybeFlipBytes(MemoryStream s, int count)
            {
                byte[] buffer = new byte[count];
                s.Read(buffer, 0, count);
                if (flip_bytes)
                {
                    Array.Reverse(buffer);
                }
                return buffer;
            }

            object DebugCheck(uint check)
            {
                if (headers.TryGetValue(check, out string result))
                {
                    return result;
                }
                else
                {
                    return DebugReader.DbgCheck(check);
                }
                
            }

            while (true)
            {
                PakEntry entry = new PAK.PakEntry();
                uint header_start = (uint)stream.Position; // To keep track of which entry since the offset in the header needs to be added to the StartOffset below
                
                uint extension = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                if (extension != 0x2cb3ef3b)
                {
                    entry.Extension = DebugCheck(extension);
                }
                else
                {
                    break;
                }
                uint offset = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.StartOffset = offset+header_start;
                uint filesize = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.FileSize = filesize;
                uint asset = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.AssetContext = DebugCheck(asset);
                uint fullname = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.FullName = DebugCheck(fullname);
                uint name = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.NameNoExt = DebugCheck(name);
                uint parent = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.Parent = parent;
                uint flags = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize));
                entry.Flags = flags;
                entry.EntryData = new byte[entry.FileSize];
                Array.Copy(PakBytes, entry.StartOffset, entry.EntryData, 0, entry.FileSize);
                PakList.Add(entry);
            }
            
            
            return PakList;
        }

        public static string GetExtension()
        {
            return "test";
        }
    }

    
}
