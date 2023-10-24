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
        public static bool FlipCheck(string endian)
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
            bool flipBytes = FlipCheck(endian);
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
                
                uint extension = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
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
                uint offset = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.StartOffset = offset+header_start;
                uint filesize = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.FileSize = filesize;
                uint asset = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.AssetContext = DebugCheck(asset);
                uint fullname = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.FullName = DebugCheck(fullname);
                uint name = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.NameNoExt = DebugCheck(name);
                if (entry.FullName.StartsWith("0x"))
                {
                    entry.FullName = $"{entry.FullName}.{entry.NameNoExt}";
                }
                uint parent = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.Parent = parent;
                uint flags = BitConverter.ToUInt32(ReadAndMaybeFlipBytes(stream, UnitSize, flipBytes));
                entry.Flags = flags;
                switch (flags)
                {
                    case 0:
                        break;
                    case 0x20:
                        var skipTo = stream.Position + 160;
                        string tempString = ReadUntilNullByte(stream);
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
                    PakBytes = Compression.DecompressChunk(PakBytes);
                    stream = new MemoryStream(PakBytes);
                    TryGH3 = true;
                }
            }
            
            
            return PakList;
        }

        public static string ReadUntilNullByte(MemoryStream memoryStream)
        {
            List<byte> byteList = new List<byte>();
            int currentByte;

            // Read byte by byte
            while ((currentByte = memoryStream.ReadByte()) != -1) // -1 means end of stream
            {
                // Break if currentByte is null byte
                if (currentByte == 0)
                    break;

                byteList.Add((byte)currentByte);
            }

            // Convert byte list to string using UTF-8 encoding
            return Encoding.UTF8.GetString(byteList.ToArray());
        }
        public static byte[] ReadAndMaybeFlipBytes(MemoryStream s, int count, bool flipBytes)
        {
            byte[] buffer = new byte[count];
            s.Read(buffer, 0, count);
            if (flipBytes)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }
    }




}
