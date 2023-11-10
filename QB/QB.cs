using GH_Toolkit_Core.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.PAK.PAK;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBStruct;
using static GH_Toolkit_Core.QB.QBScript;
using System.IO;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using GH_Toolkit_Core.Methods;
using System.Xml.Linq;

namespace GH_Toolkit_Core.QB
{
    public class QB
    {
        public static bool FlipBytes = true;
        public static Dictionary<uint, string> SongHeaders;
        public static ReadWrite Reader;
        [DebuggerDisplay("{Info, nq}: {Props, nq} - {Data}")]
        public class QBItem
        {
            public QBItemInfo Info { get; set; }
            public QBSharedProps Props { get; set; }
            public object Data { get; set; }
            public QBItem(MemoryStream stream)
            {
                Info = new QBItemInfo(stream);
                Props = new QBSharedProps(stream, Info.Type);
                if (IsSimpleValue(Info.Type))
                {
                    Data = Props.DataValue;
                }
                else
                {
                    Data = ReadQBData(stream, Info.Type);
                }
            }
        }
        public class QBBase
        {
            public byte Flags { get; set; }
            public uint Info { get; set; }
            public string Type { get; set; }
            public QBBase(MemoryStream stream)
            {
                Info = ReadQBHeader(stream);
                Flags = (byte)(Info >> 8);
            }
        }
        [DebuggerDisplay("{Type}")]
        public class QBItemInfo : QBBase
        {
            public QBItemInfo(MemoryStream stream) : base(stream)
            {
                Type = QbType[(byte)(Info >> 16)];
            }
        }
        [DebuggerDisplay("{Type}")]
        public class QBStructInfo : QBBase
        {
            public QBStructInfo(MemoryStream stream) : base(stream)
            {
                byte infoByte = (byte)(Info >> 8);
                byte infoByte2 = (byte)(Info >> 16);
                if (infoByte == 0x01 && infoByte2 != 0x00)
                {
                    infoByte = infoByte2;
                }
                else if ((infoByte & FLAG_STRUCT_GH3) != 0)
                {
                    infoByte &= 0x7F;
                }
                Type = StructType[infoByte];
            }
        }
        [DebuggerDisplay("{ID}")]
        public class QBSharedProps
        {
            public string ID { get; set; }
            public string QbName { get; set; }
            public object DataValue { get; set; }
            /*  DataValue can be a value itself (for integers, floats, and QB Keys)
                or it can be the starting byte of the data */
            public uint NextItem { get; set; }
            public string Name { get; set; }
            public QBSharedProps(MemoryStream stream, string itemType)
            {
                ID = ReadQBKey(stream);
                QbName = ReadQBKey(stream);
                DataValue = ReadQBValue(stream, itemType);
                NextItem = Reader.ReadUInt32(stream);
            }
        }

        public class QBHeader
        {
            public uint Flags { get; set; }
            public uint FileSize { get; set; }
            public QBHeader(MemoryStream stream)
            {
                Flags = Reader.ReadUInt32(stream);
                FileSize = Reader.ReadUInt32(stream);
                stream.Seek(28, SeekOrigin.Begin);
            }

        }
        public static string ReadQBKey(MemoryStream stream)
        {
            return DebugReader.DebugCheck(SongHeaders, Reader.ReadUInt32(stream));
        }

        private static uint ReadQBHeader(MemoryStream stream)
        {
            return BitConverter.ToUInt32(ReadWrite.ReadNoFlip(stream, 4));
        }
        public static object ReadQBValue(MemoryStream stream, string itemType)
        {
            switch (itemType)
            {
                case FLOAT:
                    return Reader.ReadFloat(stream);
                case QBKEY:
                case QSKEY:
                case POINTER:
                    return ReadQBKey(stream);
                default:
                    return Reader.ReadUInt32(stream);
            }
        }
        public static List<float> ReadQBTuple(MemoryStream stream, uint amount, bool readHeader = true)
        {
            if (readHeader)
            {
                stream.Position += 4; // Skip the floats header
            }
            var list = new List<float>();
            for (int i = 0; i < amount; i++)
            {
                list.Add(Reader.ReadFloat(stream));
            }
            return list;
        }
        public static object ReadQBData(MemoryStream stream, string itemType)
        {
            Func<MemoryStream, object> createItem = GetItemFunction(itemType);
            object qbData = createItem(stream);
            ReadWrite.MoveToModFour(stream);
            return qbData;
        }
        public static Func<MemoryStream, object> GetItemFunction(string itemType)
        {

            switch (itemType)
            {
                case ARRAY:
                    return s => new QBArrayNode(s);
                case PAIR:
                    return s => ReadQBTuple(s, 2);
                case SCRIPT:
                    return s => new QBScriptData(s);
                case STRUCT:
                    return s => new QBStructData(s);
                case STRING:
                    return ReadWrite.ReadUntilNullByte;
                case VECTOR:
                    return s => ReadQBTuple(s, 3);
                case WIDESTRING:
                    return s => ReadWrite.ReadWideString(s);
                default:
                    throw new Exception($"{itemType} is not supported!");
            }
        }
        public static bool IsSimpleValue(string info)
        {
            switch (info)
            {
                case FLOAT:
                case INTEGER:
                case POINTER:
                case QBKEY:
                case QSKEY:
                    return true;
            }

            return false;
        }
        public static string ReadStructValue(MemoryStream stream)
        {
            // This is only needed since PS2 is special and has separate values
            return "";
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

        public static Dictionary<byte, string> StructType { get; private set; }

        public static void SetStructType(string endian)
        {
            if (endian == "big")
            {
                StructType = QbType;
            }
            else
            {
                StructType = QbTypePs2Struct;
            }

        }

        public static List<QBItem> DecompileQb(byte[] qbBytes, string endian = "big", string songName = "")
        {
            SetStructType(endian);
            Reader = new ReadWrite(endian);

            var qbList = new List<QBItem>();
            SongHeaders = DebugReader.MakeDictFromName(songName);
            using (MemoryStream stream = new MemoryStream(qbBytes))
            {
                QBHeader header = new QBHeader(stream);
                while (stream.Position < stream.Length)
                {
                    QBItem item = new QBItem(stream);
                    qbList.Add(item);
                }
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
            List<QBItem> qbList = DecompileQb(qbBytes, endian, songName);
        }

    }
}
