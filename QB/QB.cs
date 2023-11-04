using GH_Toolkit_Core.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.PAK;
using static GH_Toolkit_Core.QB.QBConstants;

namespace GH_Toolkit_Core.QB
{
    public class QB
    {
        public static bool FlipBytes = true;
        public static Dictionary<uint, string> SongHeaders;
        public static ReadWrite Reader;

        public class QbItem
        {
            public QbItemInfo Info { get; set; }
            public QbSharedProps Props { get; set; }
            public object Data { get; set; }
            public QbItem(MemoryStream stream)
            {
                Info = new QbItemInfo(BitConverter.ToUInt32(ReadWrite.ReadNoFlip(stream, 4)));
                Props = new QbSharedProps(stream, Info.Type);
                switch (Props.Name)
                {
                    case FLOAT:
                    case INTEGER:
                    case QBKEY:
                    case QSKEY:
                    case POINTER:
                        Data = Props.DataValue;
                        break;


                }
            }
        }
        public class QbItemInfo
        {
            public byte Flags { get; set; }
            public string Type { get; set; }
            public QbItemInfo(uint info)
            {
                Flags = (byte)(info >> 8);
                Type = QbType[(byte)(info >> 16)];
            }
        }
        public class QbSharedProps
        {
            public string ID { get; set; }
            public string QbName { get; set; }
            public object DataValue { get; set; }
            /*  DataValue can be a value itself (for integers, floats, and QB Keys)
                or it can be the starting byte of the data */
            public uint NextItem { get; set; }
            public string Name { get; set; }
            public QbSharedProps(MemoryStream stream, string itemType)
            {
                ID = ReadQbKey(stream);
                QbName = ReadQbKey(stream);
                switch (itemType)
                {
                    case FLOAT:
                        DataValue = Reader.ReadFloat(stream);
                        break;
                    case QBKEY:
                    case QSKEY:
                    case POINTER:
                        DataValue = ReadQbKey(stream);
                        break;
                    default:
                        DataValue = Reader.ReadUInt32(stream);
                        break;
                }
                NextItem = Reader.ReadUInt32(stream);
            }
        }

        public class QbHeader
        {
            public uint Flags { get; set; }
            public uint FileSize { get; set; }
            public QbHeader(MemoryStream stream)
            {
                Flags = Reader.ReadUInt32(stream);
                FileSize = Reader.ReadUInt32(stream);
                stream.Seek(28, SeekOrigin.Begin);
            }

        }
        private static string ReadQbKey(MemoryStream stream)
        {
            return DebugReader.DebugCheck(SongHeaders, Reader.ReadUInt32(stream));
        }

        private static readonly Dictionary<byte, string> QbFlags = new Dictionary<byte, string>()
        {
            {0x20, "Section"},
            {0x4, "Section"},
            {0x1, "Array"},

        };

        private static readonly Dictionary<byte, string> QbType = new Dictionary<byte, string>()
        {
            {0x00, "Flag" }, // Should only be used in structs
            {0x01, "Integer" },
            {0x02, "Float" },
            {0x03, "String" },
            {0x04, "WideString" },
            {0x05, "Pair" }, // Two float values
            {0x06, "Vector" }, // Three float values
            {0x07, "Script" },
            {0x0A, "Struct"},
            {0x0C, "Array" },
            {0x0D, "QbKey" },
            {0x1A, "Pointer" },
            {0x1C, "QsKey" }, // AKA localized string
        };

        private static readonly Dictionary<byte, string> QbTypePs2Struct = new Dictionary<byte, string>()
        {
            {0x00, "Flag" },
            {0x03, "Integer" },
            {0x05, "Float" },
            {0x07, "String" },
            {0x09, "WideString" },
            {0x0B, "Pair" }, // Two float values
            {0x0D, "Vector" }, // Three float values
            {0x15, "Struct"},
            {0x19, "Array" },
            {0x1B, "QbKey" },
            {0x35, "Pointer" },
        };

        public static Dictionary<byte, string> StructTypes { get; private set; }

        public static void SetStructType(string endian)
        {
            if (endian == "big")
            {
                StructTypes = QbType;
            }
            else
            {
                StructTypes = QbTypePs2Struct;
            }

        }

        public static List<QbItem> DecompileQb(byte[] qbBytes, string endian = "big", string songName = "")
        {
            SetStructType(endian);
            Reader = new ReadWrite(endian);
            MemoryStream stream = new MemoryStream(qbBytes);
            var qbList = new List<QbItem>();
            SongHeaders = DebugReader.MakeDictFromName(songName);
            QbHeader header = new QbHeader(stream);
            while (true)
            {
                QbItem item = new QbItem(stream);
                break;
            }
            return qbList;
        }
        public static void ProcessQbFromFile(string file)
        {
            string fileName = Path.GetFileName(file);
            if (fileName.IndexOf(".qb", 0, fileName.Length, StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                throw new Exception("Invalid File");
            }
            string fileNoExt = fileName.Substring(0, fileName.IndexOf(".qb"));
            string fileExt = Path.GetExtension(file);
            Console.WriteLine($"Decompiling {fileName}");
            string folderPath = Path.GetDirectoryName(file);
            string NewFilePath = Path.Combine(folderPath, fileNoExt, $"{fileNoExt}.q");
            string songCheck = ".mid";
            string songName = "";
            if (fileName.Contains(songCheck))
            {
                songName = fileName.Substring(0, fileName.IndexOf(songCheck));
            }
            byte[] qbBytes = File.ReadAllBytes(file);
            string endian;
            if (fileExt == ".ps2")
            {
                endian = "little";
            }
            else
            {
                endian = "big";
                fileExt = ".xen";
            }
            List<QbItem> qbList = DecompileQb(qbBytes, endian, songName);
        }
    }
}
