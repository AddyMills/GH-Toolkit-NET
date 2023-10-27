using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Debug;

namespace GH_Toolkit_Core
{
    public class PAK
    {
        [DebuggerDisplay("Entry: {FullName}")]
        public class PakEntry
        {
            public string Extension { get; set; } // This can be either uint or string
            public uint StartOffset { get; set; }
            public uint FileSize { get; set; }
            public string AssetContext { get; set; } // This can be either uint or string
            public string FullName { get; set; } // This can be either uint or string
            public string NameNoExt { get; set; } // This can be either uint or string
            public uint Parent { get; set; }
            public uint Flags { get; set; }
            public byte[] EntryData { get; set; }
        }

        public static List<PakEntry> ExtractPAK(byte[] PakBytes, string endian = "big", string songName = "")
        {
            const int ChunkSize = 32;  // Size of each read chunk for PAK Header
            const int UnitSize = 4;    // Size of each unit in one entry
            bool flipBytes = Readers.FlipCheck(endian);
            if (Compression.isCompressed(PakBytes))
            {
                PakBytes = Compression.DecompressWTPak(PakBytes);
            }
            MemoryStream stream = new MemoryStream(PakBytes);
            List<PakEntry> PakList = new List<PakEntry>();
            Dictionary<uint, string> headers = DebugReader.MakeDictFromName(songName);

            string DebugCheck(uint check)
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
            bool TryGH3 = false;
            while (true)
            {
                PakEntry entry = new PAK.PakEntry();
                uint header_start = (uint)stream.Position; // To keep track of which entry since the offset in the header needs to be added to the StartOffset below

                uint extension = Readers.ReadUInt32(stream, flipBytes);
                if (extension != 0x2cb3ef3b && extension != 0xb524565f)
                {
                    entry.Extension = DebugCheck(extension);
                }
                else
                {
                    break;
                }
                if (!entry.Extension.StartsWith("."))
                {
                    entry.Extension = "." + entry.Extension;
                }
                uint offset = Readers.ReadUInt32(stream, flipBytes);
                entry.StartOffset = offset + header_start;
                uint filesize = Readers.ReadUInt32(stream, flipBytes);
                entry.FileSize = filesize;
                uint asset = Readers.ReadUInt32(stream, flipBytes);
                entry.AssetContext = DebugCheck(asset);
                uint fullname = Readers.ReadUInt32(stream, flipBytes);
                entry.FullName = DebugCheck(fullname);
                uint name = Readers.ReadUInt32(stream, flipBytes);
                entry.NameNoExt = DebugCheck(name);
                if (entry.FullName.StartsWith("0x"))
                {
                    entry.FullName = $"{entry.FullName}.{entry.NameNoExt}";
                }
                uint parent = Readers.ReadUInt32(stream, flipBytes);
                entry.Parent = parent;
                uint flags = Readers.ReadUInt32(stream, flipBytes);
                entry.Flags = flags;
                switch (flags)
                {
                    case 0:
                        break;
                    case 0x20:
                        var skipTo = stream.Position + 160;
                        string tempString = Readers.ReadUntilNullByte(stream);
                        switch (tempString)
                        {
                            case string s when s.StartsWith("ones\\"):
                                tempString = "z" + tempString;
                                break;
                            case string s when s.StartsWith("cripts\\"):
                                tempString = "s" + tempString;
                                break;
                            case string s when s.StartsWith("kies\\"):
                                tempString = "s" + tempString;
                                break;
                            case string s when s.StartsWith("ongs\\"):
                                tempString = "s" + tempString;
                                break;
                            case string s when s.StartsWith("odels\\"):
                                tempString = "m" + tempString;
                                break;
                        }
                        entry.FullName = tempString;
                        stream.Position = skipTo;
                        break;
                }
                try
                {
                    entry.EntryData = new byte[entry.FileSize];
                    Array.Copy(PakBytes, entry.StartOffset, entry.EntryData, 0, entry.FileSize);
                    PakList.Add(entry);
                }
                catch (Exception ex)
                {
                    if (TryGH3 == true)
                    {
                        Console.WriteLine(ex.Message);
                        throw new Exception("Could not extract PAK file.");
                    }
                    Console.WriteLine("Could not find last entry. Trying Guitar Hero 3 Compression.");
                    PakList.Clear();
                    PakBytes = Compression.DecompressData(PakBytes);
                    stream = new MemoryStream(PakBytes);
                    TryGH3 = true;
                }
            }

            Console.WriteLine("Success!");


            return PakList;
        }
    }
}
