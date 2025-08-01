﻿using GH_Toolkit_Core.Checksum;
using GH_Toolkit_Core.Debug;
using GH_Toolkit_Core.Methods;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.QB.QBScript;
using static GH_Toolkit_Core.QB.QBStruct;
using static GH_Toolkit_Core.Methods.Exceptions;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using static GH_Toolkit_Core.PAK.PAK;
using Melanchall.DryWetMidi.Composing;
using System.Runtime.CompilerServices;

/*
 * * This file is intended to be a collection of methods to read and create QB files
 * * This includes reading and parsing a text file to turn into a QB
 * */

namespace GH_Toolkit_Core.QB
{
    public class QB
    {
        public static bool FlipBytes = true;
        public static Dictionary<uint, string> SongHeaders;
        public static ReadWrite Reader;
        private static string QbKeyPattern = @"^[a-z0-9_]+$";
        private static string FloatPattern = @"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$";
        public static Regex QbKeyRegex = new Regex(QbKeyPattern, RegexOptions.IgnoreCase);
        private static bool WideStringSwap = false;
        private static CultureInfo enUs = new CultureInfo("en-US");
        public Dictionary<uint, string> QsList { get; set; } = new Dictionary<uint, string>();
        public List<QBItem> Children { get; set; }
        public bool QsSwap { get; set; } = false; // This is used to determine if strings should be swapped to QS keys
        public QB(bool qsSwap)
        {
            Children = new List<QBItem>();
            QsSwap = qsSwap;
        }
        private void AddQbChild(QBItem item)
        {
            Children.Add(item);
        }
        /// <summary>
        /// Add item to a QS List
        /// </summary>
        public void AddToQsList(string item, bool forceString = false)
        {
            var qsString = CRC.QSKeyUInt(item);

            if (!QsList.ContainsKey(qsString))
            {
                QsList.Add(qsString, item);
            }
        }
        [DebuggerDisplay("{Info, nq}: {Name, nq} - {Data}")]
        public class QBItem
        {
            public QBItemInfo Info { get; set; }
            public QBSharedProps Props { get; set; }
            public object Data { get; set; }
            public string Name { get; set; }
            public object? Children { get; set; }
            // Blank QB Item to build one like Lego!
            public QBItem() { }
            // QB Item where Info is inferred from the data
            public QBItem(string name, object data)
            {
                SetName(name);
                SetData(data);
                GetInfoFromData(data);
            }
            // QB Item from MemoryStream
            public QBItem(MemoryStream stream)
            {
                Info = new QBItemInfo(stream);
                if (Info.isGhtcp)
                {
                    Data = new QBStructItem(stream);
                    var dataStruct = Data as QBStructItem;
                    var dataProps = dataStruct.Props;
                    Props = new QBSharedProps(dataProps.ID, dataProps.ID, 0, 0);
                    Data = dataStruct.Data;
                }
                else
                {
                    Props = new QBSharedProps(stream, Info.Type);
                    if (ReadWrite.IsSimpleValue(Info.Type))
                    {
                        Data = Props.DataValue;
                    }
                    else
                    {
                        Data = ReadQBData(stream, Info.Type);
                    }
                }

                Name = Props.ID;
            }
            public void SetName(string name)
            {
                Name = name;
            }
            public string GetName()
            {
                return Name;
            }
            public void SetData(object data)
            {
                Data = data;
            }
            public void AddDataAndSort(QBArrayNode data)
            {
                if (data.Items.Count == 0)
                {
                    return;
                }
                if (Data is QBArrayNode arrayNode)
                {
                    arrayNode.Items.AddRange(data.Items);

                }
                else
                {
                    throw new ArgumentException("Data is not an array or struct");
                }
            }
            public void AddDataAndSort(QBStructData data)
            {
                if (Data is QBStructData structData)
                {
                    structData.Items.AddRange(data.Items);
                }
                else
                {
                    throw new ArgumentException("Data is not an array or struct");
                }
            }
            public void SetInfo(string type)
            {
                if (type == MULTIFLOAT)
                {
                    Info = new QBItemInfo(MultiFloatType());
                }
                else
                {
                    Info = new QBItemInfo(type);
                }
            }
            public void MakeEmpty()
            {
                QBArrayNode emptyData = new QBArrayNode();
                emptyData.MakeEmpty();
                SetData(emptyData);
                SetInfo(ARRAY);
            }
            public void MakeEmpty(string name)
            {
                SetName(name);
                MakeEmpty();
            }
            public void CreateQBItem(string name, string value, string type)
            {
                SetName(name);
                Data = ParseData(value, type);
                SetInfo(type);
            }
            // Incomplete function below, only works for Arrays currently
            private void GetInfoFromData(object data)
            {
                if (data == null)
                {
                    MakeEmpty(); return;
                    // throw new ArgumentNullException($"Could not parse {Name} due to null data found.");
                }
                if (data is QBArrayNode)
                {
                    SetInfo(ARRAY);
                }
                else if (data is QBStructData)
                {
                    SetInfo(STRUCT);
                }
                else if (data is List<float> floatList)
                {
                    SetInfo(MULTIFLOAT);
                }
                else
                {
                    throw new NotImplementedException("This data type is not yet implemented");
                }
            }
            private string MultiFloatType()
            {
                if (Data is List<float> floatList)
                {
                    if (floatList.Count < 2 || floatList.Count > 3)
                    {
                        throw new ArgumentException("List of float values does not contain only 2 or 3 items.");
                    }
                    return floatList.Count == 2 ? PAIR : VECTOR;
                }
                throw new ArgumentException("No list of floats provided");
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
            public QBBase() { }
            private void SetFlags(byte flag)
            {
                Flags = flag;
            }
        }
        [DebuggerDisplay("{Type}")]
        public class QBItemInfo : QBBase
        {
            public bool isGhtcp = false;
            public QBItemInfo(MemoryStream stream) : base(stream)
            {
                byte typeByte = (byte)(Info >> 16);
                if (Flags > 0x7f)
                {
                    // This should never happen with official files, but it does with GHTCP's QB files
                    typeByte = (byte)(Flags - 0x80);
                    Flags = 0x20;
                    stream.Position -= 4;
                    isGhtcp = true;
                }
                Type = QbType[typeByte];
            }
            public QBItemInfo(string type)
            {
                Type = type;
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
                    try
                    {
                        Type = QbType[infoByte];
                    }
                    catch
                    {
                        Type = QBKEY;
                    }
                }
                else
                {
                    if ((infoByte & FLAG_STRUCT_GH3) != 0)
                    {
                        infoByte &= 0x7F;
                    }
                    Type = StructType[infoByte];
                }
            }
            public QBStructInfo(string type)
            {
                Type = type;
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
            public QBSharedProps(MemoryStream stream, string itemType)
            {
                ID = ReadQBKey(stream);
                QbName = ReadQBKey(stream);
                DataValue = ReadQBValue(stream, itemType);
                NextItem = Reader.ReadUInt32(stream);
            }
            public QBSharedProps(string id, string qbName, object dataValue, uint nextItem)
            {
                ID = id;
                QbName = qbName;
                DataValue = dataValue;
                NextItem = nextItem;
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
        public static string ReadQsKey(MemoryStream stream)
        {
            return DebugReader.QsDbgString(Reader.ReadUInt32(stream));
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
                case INTEGER:
                    return Reader.ReadInt32(stream);
                case QBKEY:
                case POINTER:
                    return ReadQBKey(stream);
                case QSKEY:
                    return ReadQsKey(stream);
                default:
                    return Reader.ReadUInt32(stream);
            }
        }
        // This is a function that will read a string consisting of hex string and string pairs
        // and return then as a dictionary
        public static Dictionary<uint, string> GetQsDictFromString(string qsPairs)
        {
            var qsDict = new Dictionary<uint, string>();
            string[] pairs = qsPairs.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] pairSplit = pair.Split(new string[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

                string hexString = pairSplit[0].Trim();
                string valueString = pairSplit[1].Trim();

                //Console.WriteLine($"Processing pair: {hexString} -> {valueString}");

                if (uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint key))
                {
                    qsDict.Add(key, valueString);
                }
                else
                {
                    throw new ArgumentException($"Invalid hex string found: {hexString}");
                }


            }
            return qsDict;
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
        public static List<float> ParseQBTuple(string[] floats)
        {
            var list = new List<float>();
            foreach (string floatVal in floats)
            {
                if (float.TryParse(floatVal, enUs, out float val))
                {
                    list.Add(val);
                }
                else
                {
                    throw new FloatParseException("Bad float data found");
                }
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

        private static readonly Dictionary<byte, string> QbType = new Dictionary<byte, string>()
        {
            {0x00, STRUCTFLAG }, // Should only be used in structs
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

            // Values found in WoR in-pack scripts
            {0x24, WORINTEGER },
            {0x25, WORFLOAT },
            {0x26, WORQBKEY },
            {0x2C, WORARRAY }

        };

        private static readonly Dictionary<byte, string> QbTypeGh3Ps2Struct = new Dictionary<byte, string>()
        {
            {0x00, STRUCTFLAG },
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
        private static Dictionary<string, byte> FlipDict(Dictionary<byte, string> originalDict)
        {
            Dictionary<string, byte> flippedDict = new Dictionary<string, byte>();
            foreach (KeyValuePair<byte, string> item in originalDict)
            {
                flippedDict.Add(item.Value, item.Key);
            }
            return flippedDict;
        }
        public static Dictionary<string, byte> GetTempFlippedDict()
        {
            return FlipDict(QbType);
        }
        public static Dictionary<byte, string> StructType { get; private set; }

        public static void SetStructType(string endian, string game = "GH3")
        {
            if (endian == "big")
            {
                StructType = QbType;
            }
            else
            {
                StructType = QbTypeGh3Ps2Struct;
            }

        }
        public static Dictionary<string, QBItem> QbEntryDict(List<QBItem> qbList)
        {
            var qbDict = new Dictionary<string, QBItem>();
            foreach (QBItem item in qbList)
            {
                if (!qbDict.ContainsKey(item.Name.ToLower()))
                {
                    qbDict.Add(item.Name.ToLower(), item);
                } 
            }
            return qbDict;
        }
        public static Dictionary<string, QBItem> QbEntryDictFromBytes(byte[] qbBytes, string endian = "", string songName = "")
        {
            var qbList = DecompileQb(qbBytes, endian, songName);

            return QbEntryDict(qbList);
        }
        public static byte[] CompileQbFromDict(Dictionary<string, QBItem> qbDict, string qbPath, string game = GAME_GH3, string console = CONSOLE_XBOX)
        {
            var qbList = new List<QBItem>();
            foreach (KeyValuePair<string, QBItem> item in qbDict)
            {
                qbList.Add(item.Value);
            }
            var bytes = CompileQbFile(qbList, qbPath, game, console);
            return bytes;
        }
        public static List<QBItem> DecompileQb(byte[] qbBytes, string endian = "big", string songName = "", string game = "", string console = "")
        {
            SetStructType(endian);
            Reader = new ReadWrite(endian, game, console);

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
        public static List<QBItem> DecompileQbFromFile(string qbPath, string endian = "big", string songName = "", string game = "", string console = "")
        {
            byte[] qbBytes = File.ReadAllBytes(qbPath);
            return DecompileQb(qbBytes, endian, songName, game, console);
        }
        public static string QbItemText(string itemType, object itemData)
        {
            string itemString;
            if (itemType == FLOAT)
            {
                float itemFloat = Convert.ToSingle(itemData);
                string format = itemFloat % 1 == 0 ? "0.0" : "G";

                itemString = itemFloat.ToString(format, enUs);
                //string 

                //return f.ToString(format);
            }
            else
            {
                itemString = itemData.ToString();
            }
            string test;
            switch (itemType)
            {
                case FLOAT:
                case INTEGER:
                    test = $"{itemString}";
                    break;
                case STRING:
                    test = $"'{itemString.Replace("\\", "\\\\").Replace("'", "\\'")}'";
                    break;
                case WIDESTRING:
                    test = $"\"{itemString.Replace("\\", "\\\\")}\"";
                    break;
                case QBKEY:
                case QSKEY:
                case POINTER:
                    test = GetQbKeyFormat(itemString);
                    if (itemType == QSKEY)
                    {
                        if (NeedsQsKeyWrap(itemString))
                        {
                            test = $"qs({test})";
                        }
                        else
                        {
                            test = $"\"{itemString}\"";
                        }
                    }
                    else if (itemType == POINTER)
                    {
                        test = $"${test}";
                    }
                    break;
                case WORINTEGER:
                    test = $"!i{itemString}";
                    break;
                case WORFLOAT:
                    test = $"!f{itemString}";
                    break;
                case WORQBKEY:
                    test = $"!q{itemString}";
                    break;
                case WORARRAY:
                    test = $"!a{itemString}";
                    break;
                default:
                    throw new ArgumentException("Invalid string found.");
            }

            return test;
        }
        private static bool NeedsQsKeyWrap(string qsString)
        {
            if (qsString.StartsWith("0x") && qsString.Length == 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static string GetQbKeyFormat(string toFormat)
        {
            if (toFormat == "default")
            {
                return $"`{toFormat}`";
            }
            else if (toFormat.StartsWith("0x"))
            {
                return DebugReader.DebugCheck(toFormat);
            }
            else if (float.TryParse(toFormat, enUs, out _))
            {
                return $"`{toFormat}`";
            }
            else if (QbKeyRegex.IsMatch(toFormat))
            {
                return $"{toFormat}";
            }
            else
            {
                return $"`{toFormat}`";
            }
        }
        public static string FloatsToText(List<float> floats)
        {
            var formattedFloats = floats.Select(f => {
                string format = f % 1 == 0 ? "0.0" : "G";
                return f.ToString(format, enUs);
            });
            string floatString = string.Join(", ", formattedFloats);
            return $"({floatString})";
        }
        public static void QbToText(List<QBItem> qbList, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (QBItem item in qbList)
                {
                    if (item.Data is QBArrayNode arrayNode)
                    {
                        writer.WriteLine($"{item.Name} = [");
                        arrayNode.ArrayToText(writer, 1);
                        writer.WriteLine("]");
                    }
                    else if (item.Data is QBStructData structNode)
                    {
                        writer.WriteLine($"{item.Name} = {{");
                        structNode.StructToText(writer, 1);
                        writer.WriteLine("}");
                    }
                    else if (item.Data is List<float> floats)
                    {
                        writer.WriteLine($"{item.Name} = {FloatsToText(floats)}");
                    }
                    else if (item.Data is QBScriptData scriptData)
                    {
                        writer.WriteLine();
                        writer.Write($"script {item.Name} ");
                        writer.Write(scriptData.ScriptToText(scriptData.ScriptParsed, 1));
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine($"{item.Name} = {QbItemText(item.Info.Type, item.Data.ToString())}");
                    }
                }
            }
        }
        public static string QbToTextString(List<QBItem> qbList)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                foreach (QBItem item in qbList)
                {
                    if (item.Data is QBArrayNode arrayNode)
                    {
                        writer.WriteLine($"{item.Name} = [");
                        arrayNode.ArrayToText(writer, 1);
                        writer.WriteLine("]");
                    }
                    else if (item.Data is QBStructData structNode)
                    {
                        writer.WriteLine($"{item.Name} = {{");
                        structNode.StructToText(writer, 1);
                        writer.WriteLine("}");
                    }
                    else if (item.Data is List<float> floats)
                    {
                        writer.WriteLine($"{item.Name} = {FloatsToText(floats)}");
                    }
                    else if (item.Data is QBScriptData scriptData)
                    {
                        writer.WriteLine();
                        writer.Write($"script {item.Name} ");
                        writer.Write(scriptData.ScriptToText(scriptData.ScriptParsed, 1));
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine($"{item.Name} = {QbItemText(item.Info.Type, item.Data.ToString())}");
                    }
                }
            }
            return sb.ToString();
        }
        enum ParseState
        {
            whitespace,
            inKey,
            inKeyQb, //This is if a key has a space in it
            inString,
            inQbKey,
            inQsKey,
            inComment,
            inBlockComment,
            inScript,
            inArray,
            inStruct,
            inValue,
            inMultiFloat
        }

        enum StringType
        {
            isString,
            isWideString
        }
        private class ParseLevel
        {
            public ParseLevel? Parent { get; set; }
            public ParseState State { get; set; }
            public StringType StringType { get; set; }
            public string LevelType { get; set; }
            public string? Name { get; set; }
            public QBArrayNode? Array { get; set; }
            public QBStructData? Struct { get; set; }
            public QBScriptData? Script { get; set; }
            private ParseState lastState = ParseState.whitespace;
            public ParseLevel(ParseLevel? parent, ParseState state, string type)
            {
                Parent = parent;
                State = state;
                LevelType = type;
                StringType = StringType.isString;
                if (type == ARRAY) { Array = new QBArrayNode(); }
                else if (type == STRUCT) { Struct = new QBStructData(); }
                else if (type == SCRIPT) { Script = new QBScriptData(); }
            }
            public void SetStringType(StringType str)
            {
                StringType = str;
            }
            public void SetLastState(ParseState state)
            {
                lastState = state;
            }
            public ParseState GetLastState()
            {
                return lastState;
            }
        }
        public static string GetParseType(string data)
        {
            return data switch
            {
                var d when d.StartsWith("$") => POINTER,
                var d when d.StartsWith("0x") => QBKEY,
                var d when d == "!=" => QBKEY, // Explicitly handle "!=" case here.
                var d when d.Contains('.') && float.TryParse(d, NumberStyles.Any, CultureInfo.InvariantCulture, out _) => FLOAT,
                var d when int.TryParse(d, out _) => INTEGER,
                var d when d.StartsWith("!") => d.Substring(0, 2) switch
                {
                    "!i" => WORINTEGER,
                    "!f" => WORFLOAT,
                    "!q" => WORQBKEY,
                    "!a" => WORARRAY,
                    _ => throw new ArgumentException("Invalid reference string found.")
                },
                var d when Regex.IsMatch(d, FloatPattern) => FLOAT,
                _ => QBKEY,
            };
        }
        public static object ParseData(string data, string type)
        {
            switch (type)
            {
                case INTEGER:
                    return int.Parse(data, enUs);
                case FLOAT:
                    return float.Parse(data, enUs);
                case MULTIFLOAT:
                case PAIR:
                case VECTOR:
                    return ParseMultiFloat(data);
                case STRING:
                case WIDESTRING:
                    return data;
                case QBKEY:
                case QSKEY:
                case POINTER:
                    data = StripQbKeyQuotes(data);
                    if (type == POINTER && data.StartsWith("$"))
                    {
                        data = data.Substring(1);
                    }
                    if (type == QSKEY)
                    {
                        return CRC.QBKeyQs(data);
                    }
                    return CRC.QBKey(data);
                case WORINTEGER:
                case WORFLOAT:
                case WORQBKEY:
                case WORARRAY:
                    return int.Parse(data.Substring(2));
                default:
                    throw new NotImplementedException("Not yet implemented");
            }
        }
        public static string StripQbKeyQuotes(string str)
        {
            if (str.StartsWith('`') && str.EndsWith('`'))
            {
                str = str.Substring(1, str.Length - 2);
            }
            else if (str.StartsWith("#\"") && str.EndsWith('"'))
            {
                str = str.Substring(2, str.Length - 1);
            }
            return str;
        }
        public static List<float> ParseMultiFloat(string data)
        {
            string[] floatStrings = data.Split(',');
            List<float> floatArray;
            try
            {
                floatArray = ParseQBTuple(floatStrings);
            }
            catch (FloatParseException ex)
            {
                Console.WriteLine($"Error parsing pair or vectors {data}");
                throw;
            }
            return floatArray;
        }
        public static void ClearTmpValues(ref string tmpKey, ref string tmpValue)
        {
            tmpKey = "";
            tmpValue = "";
        }
        public static (List<QBItem>, Dictionary<uint, string>) ParseQFromFile(string data)
        {
            if (File.Exists(data))
            {
                data = File.ReadAllText(data);
            }
            else
            {
                throw new FileNotFoundException("File not found", data);
            }
            return ParseQFile(data);
        }
        public static (List<QBItem>, Dictionary<uint,string>) ParseQFile(string data, string console = CONSOLE_XBOX, string game = GAME_GH3)
        {
            WideStringSwap = console == CONSOLE_PS2 || console == CONSOLE_WII;
            bool qsSwap = (game != GAME_GH3 && game != GAME_GHA);
            if (File.Exists(data))
            {
                var tempData = File.ReadAllText(data);
                if (tempData.Contains('�')) 
                {
                    data = File.ReadAllText(data, Encoding.Latin1);
                }
                else
                {
                    data = tempData;
                }
            }
            ParseLevel root = new ParseLevel(null, ParseState.whitespace, ROOT);
            ParseLevel currLevel = root;
            bool escaped = false;
            QB qbFile = new QB(qsSwap);
            QBItem currItem = new QBItem();
            data += " "; // Safety character to make sure everything gets parsed
            string tmpKey = "";
            string tmpValue = "";
            string tmpType;
            int lineNumber = 1; // Start from line 1
            bool lastCharWasCarriageReturn = false;
            bool lastForwardSlash = false;
            bool insideStringQs = false;
            char currentQuoteChar = '\0';
            try
            {
                for (int i = 0; i < data.Length; i++)
                {
                    char c = data[i];
                    if (c == '\n')
                    {
                        // Increment line number only if the last character wasn't '\r'
                        if (!lastCharWasCarriageReturn)
                        {
                            lineNumber++;
                        }
                    }
                    else if (c == '\r')
                    {
                        // Increment line number for '\r' and set the flag
                        lineNumber++;
                        lastCharWasCarriageReturn = true;
                    }
                    else
                    {
                        // Reset the flag if it's not a newline sequence
                        lastCharWasCarriageReturn = false;
                    }
                    switch (currLevel.State)
                    {
                        case ParseState.whitespace:
                            switch (c)
                            {
                                case ' ':
                                case '\r':
                                case '\n':
                                case '\t':
                                    continue;
                                case ';':
                                    tmpValue = "";
                                    currLevel.SetLastState(currLevel.State);
                                    currLevel.State = ParseState.inComment;
                                    break;
                                case '/':
                                    if (lastForwardSlash)
                                    {
                                        currLevel.SetLastState(currLevel.State);
                                        currLevel.State = ParseState.inComment;
                                    }
                                    else
                                    {
                                        lastForwardSlash = true;
                                    }
                                    break;
                                case '*':
                                    if (lastForwardSlash)
                                    {
                                        currLevel.SetLastState(currLevel.State);
                                        currLevel.State = ParseState.inBlockComment;
                                        lastForwardSlash = false;
                                    }
                                    else
                                    {
                                        throw new QFileParseException("Invalid character found at start of line");
                                    }
                                    break;
                                case '(':
                                    break;
                                default:
                                    currLevel.State = ParseState.inKey;
                                    tmpKey = new string(c, 1);
                                    break;
                            }
                            break;
                        case ParseState.inKey:
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                case '\'':
                                case '"':
                                    if (currLevel.LevelType == STRUCT)
                                    {
                                        HandleStructFlag(ref i, ref tmpKey, currLevel);
                                        break;
                                    }
                                    throw new QFileParseException($"QB Item Key {tmpKey} found without any data!");
                                case ' ':
                                case '\t':
                                    if (tmpKey == SCRIPTKEY)
                                    {
                                        currLevel.State = ParseState.inValue;
                                    }
                                    /*else if (tmpKey.StartsWith("#\"") && tmpKey.EndsWith("\""))
                                    {
                                        tmpKey = tmpKey.Substring(2, tmpKey.Length - 3); // Removing #"" from the start and end
                                    }
                                    else if (tmpKey.StartsWith("`") && tmpKey.EndsWith("`"))
                                    {
                                        tmpKey = tmpKey.Substring(1, tmpKey.Length - 2); // Removing ` from the start and end
                                    }*/
                                    else
                                    {
                                        tmpKey += c;
                                    }
                                    break;
                                case '=':
                                    tmpKey = StripQbKeyQuotes(tmpKey.Trim());
                                    currLevel.State = ParseState.inValue;
                                    if (tmpKey == "qb_file" && qbFile.Children.Count == 0)
                                    {
                                        // Old file if it's the first item and should be ignored.
                                        tmpKey = "";
                                        currLevel.State = ParseState.inComment;
                                    }
                                    break;
                                default:
                                    if (tmpKey.EndsWith(" ") || tmpKey.EndsWith("\t"))
                                    {
                                        tmpKey = tmpKey.TrimStart();
                                        if ((tmpKey.StartsWith("#\"") || tmpKey.StartsWith("`")) && !(tmpKey.EndsWith("\" ") || tmpKey.EndsWith("` ")))
                                        {
                                            tmpKey += c;
                                        }
                                        else if (currLevel.LevelType == STRUCT)
                                        {
                                            HandleStructFlag(ref i, ref tmpKey, currLevel);
                                        }
                                        else
                                        {
                                            throw new QFileParseException($"No equals sign found between two values at {tmpKey}");
                                        }
                                    }
                                    else
                                    {
                                        tmpKey += c;
                                    }
                                    break;

                            }
                            break;
                        case ParseState.inValue:
                            if (ParseSpecialCheck(c, ref tmpValue, currLevel))
                            {
                                continue;
                            }
                            if (c == '[' && tmpValue == "")
                            {
                                if (currLevel.LevelType == SCRIPT)
                                {
                                    tmpValue = new string(c, 1);
                                    AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                }
                                else
                                {
                                    AddLevel(ref currLevel, ref currItem, ParseState.inArray, ARRAY, ref tmpKey);
                                }
                                continue;
                            }
                            if (c == '{' && tmpValue == "" && currLevel.LevelType != SCRIPT)
                            {
                                AddLevel(ref currLevel, ref currItem, ParseState.inStruct, STRUCT, ref tmpKey);
                                continue;
                            }
                            else if (c == '{' && tmpValue == "" && currLevel.LevelType == SCRIPT)
                            {
                                if (escaped)
                                {
                                    AddLevel(ref currLevel, ref currItem, ParseState.inStruct, STRUCT, ref tmpKey);
                                    escaped = false;
                                }
                                else
                                {
                                    tmpValue = new string(c, 1);
                                    AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                }
                                continue;
                            }
                            switch (c)
                            {
                                case ',':
                                    if (currLevel.LevelType == ARRAY)
                                    {
                                        continue;
                                    }
                                    else if (currLevel.LevelType == SCRIPT)
                                    {
                                        tmpValue += c;
                                    }
                                    else
                                    {
                                        throw new QFileParseException($"Comma found outside of array or script at character {i}");
                                    }
                                    break;
                                case '\r':
                                case '\n':
                                case ' ':
                                case '\t':
                                    if (tmpValue == "" && currLevel.LevelType != SCRIPT)
                                    {
                                        throw new QFileParseException($"QB Item Key {tmpKey} found without any data!");
                                    }
                                    else if (tmpValue == "" && currLevel.LevelType == SCRIPT)
                                    {
                                        if (c == '\n')
                                        {
                                            currLevel.Script.AddScriptElem(NEWLINE);
                                        }
                                        continue;
                                    }
                                    else if (tmpKey == SCRIPTKEY)
                                    {
                                        AddLevel(ref currLevel, ref currItem, ParseState.inValue, SCRIPT, ref tmpValue);
                                        if (c == '\n')
                                        {
                                            currLevel.Script.AddScriptElem(NEWLINE);
                                        }
                                        ClearTmpValues(ref tmpKey, ref tmpValue);
                                        break;
                                    }
                                    StateSwitch(currLevel);
                                    AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        if (c == '\n')
                                        {
                                            currLevel.Script.AddScriptElem(NEWLINE);
                                        }
                                    }
                                    break;
                                case '(':
                                    if (tmpValue.ToLower() == "qs")
                                    {
                                        currLevel.State = ParseState.inQsKey;
                                        tmpValue = "";
                                    }
                                    else
                                    {
                                        if (tmpValue != "")
                                        {
                                            tmpValue += c;
                                            break;
                                        }
                                        else
                                        {
                                            throw new QFileParseException($"Unsupported character '(' in value of {tmpKey}");
                                        }
                                    }
                                    break;
                                case ')':
                                case ':':
                                    if (currLevel.LevelType != SCRIPT)
                                    {
                                        if (tmpValue != "")
                                        {
                                            tmpValue += c;
                                            break;
                                        }
                                        else
                                        {
                                            throw new QFileParseException($"'{c}' found where it shouldn't be!");
                                        }
                                    }
                                    AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                    tmpValue = new string(c, 1);
                                    AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                    break;
                                case '.':

                                    if (currLevel.LevelType != SCRIPT)
                                    {
                                        tmpValue += c;
                                    }
                                    else if (float.TryParse(tmpValue, enUs, out float val))
                                    {
                                        tmpValue += c;
                                    }
                                    else if (tmpValue == "<")
                                    {
                                        tmpValue += c;
                                    }
                                    else if (Regex.Match(tmpValue, ALLARGS_REGEX, RegexOptions.IgnoreCase).Success)
                                    {
                                        tmpValue += c;
                                    }
                                    else
                                    {
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                        tmpValue = new string(c, 1);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                    }
                                    break;
                                case '}':
                                    if (currLevel.LevelType == STRUCT)
                                    {
                                        StateSwitch(currLevel);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                        CloseStruct(ref currLevel, ref currItem, ref qbFile);
                                    }
                                    else if (currLevel.LevelType == SCRIPT)
                                    {
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                        tmpValue = new string(c, 1);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                    }
                                    else
                                    {
                                        throw new QFileParseException("Closing bracket } found outside of struct!");
                                    }

                                    break;
                                case ']':
                                    if (currLevel.LevelType == ARRAY)
                                    {
                                        StateSwitch(currLevel);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                        CloseArray(ref currLevel, ref currItem, ref qbFile);
                                    }
                                    else if (currLevel.LevelType == SCRIPT)
                                    {
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                        tmpValue = new string(c, 1);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                    }
                                    else
                                    {
                                        throw new QFileParseException("Closing brace ] found outside of array!");
                                    }
                                    break;
                                case '\\':
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        escaped = true;
                                    }
                                    else
                                    {
                                        throw new QFileParseException("\\ character found outside of string or script!");
                                    }
                                    break;
                                /*
                                case '*':
                                case '+':
                                case '-':
                                case '/':
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        if (tmpValue != "")
                                        {
                                            AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, GetParseType(tmpValue));
                                            tmpValue = new string(c, 1);
                                            AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                        }
                                        else
                                        {
                                            tmpValue += c;
                                        }
                                    }
                                    else
                                    {
                                        tmpValue += c;
                                    }
                                    break;
                                */
                                default:
                                    tmpValue += c;
                                    break;
                            }
                            break;
                        case ParseState.inMultiFloat:
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        i -= (tmpValue.Length + 1);
                                        tmpValue = LEFTPAR;
                                        StateSwitch(currLevel);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                    }
                                    break;
                                case ' ':
                                case '\t':
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        tmpValue += c;
                                    }
                                    break;
                                case ')':
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        string origVal = tmpValue;
                                        tmpValue = tmpValue.Replace("\t", "").Replace(" ", "");
                                        try
                                        {
                                            StateSwitch(currLevel);
                                            AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, MULTIFLOAT);
                                        }
                                        catch
                                        {
                                            i -= (origVal.Length + 1);
                                            tmpValue = LEFTPAR;
                                            StateSwitch(currLevel);
                                            AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                        }
                                    }
                                    else
                                    {
                                        StateSwitch(currLevel);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, MULTIFLOAT);
                                    }

                                    break;
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '0':
                                case ',':
                                case '.':
                                case '-':
                                case 'E':
                                    tmpValue += c;
                                    break;
                                default:
                                    if (currLevel.LevelType == SCRIPT)
                                    {
                                        i -= (tmpValue.Length + 1);
                                        tmpValue = LEFTPAR;
                                        StateSwitch(currLevel);
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                        break;
                                    }
                                    throw new QFileParseException("Unknown value found in Pair/Vector value");
                            }
                            break;
                        case ParseState.inArray:
                            if (ParseSpecialCheck(c, ref tmpValue, currLevel))
                            {
                                continue;
                            }
                            if (!(tmpValue == ""))
                            {
                                throw new NotImplementedException();
                            }
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                case ' ':
                                case '\t':
                                case ',':
                                    break;
                                case '{':
                                    AddLevel(ref currLevel, ref currItem, ParseState.inStruct, STRUCT, ref tmpKey);
                                    break;
                                case '[':
                                    AddLevel(ref currLevel, ref currItem, ParseState.inArray, ARRAY, ref tmpKey);
                                    break;
                                case ']':
                                    CloseArray(ref currLevel, ref currItem, ref qbFile);
                                    break;
                                default:
                                    tmpValue = new string(c, 1);
                                    currLevel.State = ParseState.inValue;
                                    break;
                            }
                            break;
                        case ParseState.inStruct:
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                case ' ':
                                case '\t':
                                    break;
                                case '[':
                                    tmpKey = FLAGBYTE;
                                    AddLevel(ref currLevel, ref currItem, ParseState.inArray, ARRAY, ref tmpKey);
                                    break;
                                case '{':
                                    tmpKey = FLAGBYTE;
                                    AddLevel(ref currLevel, ref currItem, ParseState.inStruct, STRUCT, ref tmpKey);
                                    break;
                                case '}':
                                    CloseStruct(ref currLevel, ref currItem, ref qbFile);
                                    break;
                                default:
                                    i--;
                                    currLevel.State = ParseState.inKey;
                                    break;
                            }
                            break;
                        case ParseState.inString:
                            switch (c)
                            {
                                case '"':
                                    if (escaped || currLevel.StringType == StringType.isString)
                                    {
                                        tmpValue += c;
                                        escaped = false;
                                        Console.WriteLine($"Warning: {currLevel.Name} contains double quotes");
                                    }
                                    else
                                    {
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, WIDESTRING);
                                        StateSwitch(currLevel);
                                    }
                                    break;
                                case '\'':
                                    if (escaped)
                                    {
                                        tmpValue += c;
                                        escaped = false;
                                    }
                                    else if (currLevel.StringType == StringType.isWideString)
                                    {
                                        tmpValue += c;
                                    }
                                    else
                                    {
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, STRING);
                                        StateSwitch(currLevel);
                                    }
                                    break;
                                case '\\':
                                    if (escaped)
                                    {
                                        tmpValue += c;
                                        escaped = false;
                                    }
                                    else
                                    {
                                        escaped = true;
                                    }
                                    break;
                                default:
                                    if (escaped)
                                    {
                                        //throw new InvalidOperationException("Invalid character found after escape character");
                                        escaped = false;
                                        tmpValue += '\\';
                                    }
                                    tmpValue += c;
                                    break;
                            }
                            break;
                        case ParseState.inQbKey:
                            switch (c)
                            {
                                case '"':
                                case '`':
                                    tmpValue += c;
                                    if (tmpKey == SCRIPTKEY)
                                    {
                                        tmpValue = StripQbKeyQuotes(tmpValue);
                                        AddLevel(ref currLevel, ref currItem, ParseState.inValue, SCRIPT, ref tmpValue);
                                        ClearTmpValues(ref tmpKey, ref tmpValue);
                                        break;
                                    }
                                    if (tmpValue.StartsWith("<"))
                                    {
                                        throw new Exception();
                                    }
                                    AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QBKEY);
                                    StateSwitch(currLevel);
                                    break;
                                default:
                                    tmpValue += c;
                                    break;
                            }
                            break;
                        case ParseState.inQsKey:
                            switch (c)
                            {
                                case '"':
                                case '`':
                                    if (insideStringQs)
                                    {
                                        if (c == currentQuoteChar)
                                        {
                                            // Closing quote, exit string
                                            insideStringQs = false;
                                        }
                                        else
                                        {
                                            // Different quote type inside string, treat as literal
                                            tmpValue += c;
                                        }
                                    }
                                    else
                                    {
                                        if (tmpValue == "#")
                                        {
                                            // Qualifier detected, clear tmpValue
                                            tmpValue = "";
                                        }
                                        else
                                        {
                                            // Opening quote, enter string
                                            insideStringQs = true;
                                            currentQuoteChar = c;
                                        }
                                    }
                                    /*if (tmpValue == "#")
                                    {
                                        tmpValue = "";
                                    }
                                    else
                                    {
                                        continue;
                                    }*/
                                    break;
                                case ')':
                                    if (insideStringQs)
                                    {
                                        // Treat as part of the string
                                        tmpValue += c;
                                    }
                                    else
                                    {
                                        // End of QSKEY
                                        AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QSKEY);
                                        StateSwitch(currLevel);
                                        // Reset string tracking variables
                                        insideStringQs = false;
                                        currentQuoteChar = '\0';
                                    }
                                    break;
                                default:
                                    if (insideStringQs || !char.IsWhiteSpace(c)) // Adjust based on whether whitespace is allowed outside strings
                                    {
                                        tmpValue += c;
                                    }
                                    break;
                            }
                            break;
                        case ParseState.inScript:
                            switch (c)
                            {
                                default:
                                    break;
                            }
                            break;
                        case ParseState.inComment:
                            switch (c)
                            {
                                case '\r':
                                case '\n':
                                    currLevel.State = currLevel.GetLastState();
                                    break;
                                default:
                                    continue;
                            }
                            break;
                        case ParseState.inBlockComment:
                            switch (c)
                            {
                                case '*':
                                    if (data[i + 1] == '/')
                                    {
                                        currLevel.State = currLevel.GetLastState();
                                        i++;
                                    }
                                    break;
                                default:
                                    continue;
                            }
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in script {GetRootItem(currLevel)}");
                throw new QFileParseException($"Line {lineNumber} - {ex.Message}");
            }
            return (qbFile.Children, qbFile.QsList);
        }
        private static string GetRootItem(ParseLevel currLevel)
        {
            if (currLevel.Parent.LevelType == ROOT)
            {
                return currLevel.Name;
            }
            else
            {
                return GetRootItem(currLevel.Parent);
            }
        }
        private static void HandleWhitespace(char c, ref ParseLevel currLevel, ref string tmpValue)
        {
            // Logic for handling whitespace
        }
        private static void StateSwitch(ParseLevel currLevel)
        {
            switch (currLevel.LevelType)
            {
                case ROOT:
                    currLevel.State = ParseState.whitespace;
                    break;
                case ARRAY:
                    currLevel.State = ParseState.inArray;
                    break;
                case STRUCT:
                    currLevel.State = ParseState.inStruct;
                    break;
                case SCRIPT:
                    currLevel.State = ParseState.inValue;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        private static void HandleStructFlag(ref int i, ref string tmpKey, ParseLevel currLevel)
        {
            i -= (tmpKey.Length + 1);
            tmpKey = FLAGBYTE;
            currLevel.State = ParseState.inValue;
        }
        private static void AddParseItem(ref ParseLevel currLevel,
            ref QBItem currItem,
            QB qbFile,
            ref string tmpKey,
            ref string tmpValue,
            string itemType)
        {
            if (tmpKey == "" && tmpValue == "" && (itemType != WIDESTRING && itemType != STRING))
            {
                return;
            }
            if (itemType == WIDESTRING)
            {
                qbFile.AddToQsList(tmpValue);

                if (WideStringSwap)
                {
                    itemType = STRING; // Make it a string if it's a wide string on PS2 and Wii
                }
                if (qbFile.QsSwap)
                {
                    itemType = QSKEY;
                }
            }
            switch (currLevel.LevelType)
            {
                case ROOT:
                    ProcessQbItem(ref currItem, qbFile, tmpKey, tmpValue, itemType);
                    break;
                case ARRAY:
                    currLevel.Array.AddParseToArray(tmpValue, itemType);
                    break;
                case STRUCT:
                    if (tmpKey == "")
                    {
                        tmpKey = FLAGBYTE;
                    }
                    currLevel.Struct.AddVarToStruct(tmpKey, tmpValue, itemType);
                    break;
                case SCRIPT:
                    currLevel.Script.AddToScript(itemType, tmpValue);
                    if (tmpValue == ENDSCRIPT)
                    {
                        CloseScript(ref currLevel, ref currItem, ref qbFile);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            ClearTmpValues(ref tmpKey, ref tmpValue);
        }
        private static bool ParseSpecialCheck(char c, ref string tmpValue, ParseLevel currLevel)
        {
            if ((c == ' ' || c == '\t') && tmpValue == "")
            {
                return true;
            }
            else if (c == '\'' && tmpValue == "")
            {
                currLevel.State = ParseState.inString;
                currLevel.SetStringType(StringType.isString);
                return true;
            }
            else if (c == '"' && tmpValue == "")
            {
                currLevel.State = ParseState.inString;
                currLevel.SetStringType(StringType.isWideString);
                return true;
            }
            else if (c == '"' && tmpValue == "#")
            {
                //tmpValue = "";
                tmpValue += c;
                currLevel.State = ParseState.inQbKey;
                return true;
            }
            else if (c == '`' && tmpValue == "")
            {
                tmpValue += c;
                currLevel.State = ParseState.inQbKey;
                return true;
            }
            else if (c == '(' && tmpValue == "")
            {
                currLevel.State = ParseState.inMultiFloat;
                return true;
            }
            else if (c == ';' && tmpValue == "")
            {
                currLevel.SetLastState(currLevel.State);
                currLevel.State = ParseState.inComment;
                return true;
            }
            else if (c == '/' && tmpValue == "")
            {
                tmpValue += c;
                return true;
            }
            else if (c == '/' && tmpValue == "/")
            {
                tmpValue = "";
                currLevel.SetLastState(currLevel.State);
                currLevel.State = ParseState.inComment;
                return true;
            }
            else if (c == '*' && tmpValue == "/")
            {
                tmpValue = "";
                currLevel.SetLastState(currLevel.State);
                currLevel.State = ParseState.inBlockComment;
                return true;
            }

            return false;
        }
        private static void AddLevel(ref ParseLevel currLevel, ref QBItem currItem, ParseState state, string levelType, ref string tmpKey)
        {
            string tmpString = tmpKey;
            if (currLevel.LevelType == ROOT)
            {
                currItem.SetName(tmpKey);
            }
            tmpKey = "";
            ParseLevel newLevel = new ParseLevel(currLevel, state, levelType);
            currLevel = newLevel;
            if (currLevel.LevelType != ROOT)
            {
                currLevel.Name = tmpString;
            }
        }
        private static void CloseArray(ref ParseLevel currLevel, ref QBItem currItem, ref QB qbFile)
        {
            if (currLevel.Array.FirstItem == null)
            {
                currLevel.Array.MakeEmpty();
            }
            if (currLevel.Parent.LevelType == ROOT)
            {
                currItem.SetData(currLevel.Array);
                currItem.SetInfo(ARRAY);
                qbFile.AddQbChild(currItem);
                currItem = new QBItem();
            }
            else if (currLevel.Parent.LevelType == ARRAY)
            {
                currLevel.Parent.Array.AddArrayToArray(currLevel.Array);
            }
            else if (currLevel.Parent.LevelType == STRUCT)
            {
                currLevel.Parent.Struct.AddArrayToStruct(currLevel.Name, currLevel.Array);
            }
            else
            {
                throw new NotImplementedException();
            }
            currLevel = currLevel.Parent;
            StateSwitch(currLevel);
        }
        private static void CloseStruct(ref ParseLevel currLevel, ref QBItem currItem, ref QB qbFile)
        {
            if (currLevel.Parent.LevelType == ROOT)
            {
                currItem.SetData(currLevel.Struct);
                currItem.SetInfo(STRUCT);
                qbFile.AddQbChild(currItem);
                currItem = new QBItem();
            }
            else if (currLevel.Parent.LevelType == ARRAY)
            {
                currLevel.Parent.Array.AddStructToArray(currLevel.Struct);
            }
            else if (currLevel.Parent.LevelType == STRUCT)
            {
                currLevel.Parent.Struct.AddStructToStruct(currLevel.Name, currLevel.Struct);
            }
            else if (currLevel.Parent.LevelType == SCRIPT)
            {
                currLevel.Parent.Script.AddStructToScript(currLevel.Struct);
            }
            else
            {
                throw new NotImplementedException();
            }
            currLevel = currLevel.Parent;
            StateSwitch(currLevel);
        }
        private static void CloseScript(ref ParseLevel currLevel, ref QBItem currItem, ref QB qbFile)
        {
            currItem.SetData(currLevel.Script);
            currItem.SetInfo(SCRIPT);
            qbFile.AddQbChild(currItem);
            currItem = new QBItem();
            currLevel = currLevel.Parent;
            StateSwitch(currLevel);
        }
        private static void ProcessQbItem(
            ref QBItem currItem,
            QB qbFile,
            string tmpKey,
            string tmpValue,
            string itemType)
        {
            currItem.CreateQBItem(tmpKey, tmpValue, itemType);
            qbFile.AddQbChild(currItem);
            currItem = new QBItem();
        }
        public static string ParseMultiFloatType(string multiFloat)
        {
            int commas = multiFloat.Count(x => x == ','); // I don't know where else to put this. Might as well be here
            switch (commas)
            {
                case 1:
                    return PAIR;
                case 2:
                    return VECTOR;
                default:
                    throw new ArgumentException("Too many or too few float values found.");
            }
        }
        public static byte[] CompileQbFile(List<QBItem> qbList, string qbName, string game = "GH3", string console = "360")
        {
            if (qbName.StartsWith("0x") && qbName.IndexOf(".") != -1)
            {
                qbName = $"{qbName.Substring(0, qbName.IndexOf("."))}";
            }
            byte consoleByte;
            Dictionary<string, byte> QbTypeLookup = FlipDict(QbType);
            Dictionary<string, byte> QbStructLookup;
            string endian;

            if (console == "PS2")
            {
                consoleByte = 0x04;
                if (game == "GH3")
                {
                    QbStructLookup = FlipDict(QbTypeGh3Ps2Struct);
                }
                else
                {
                    QbStructLookup = QbTypeLookup;
                }
                endian = "little";
            }
            else
            {
                consoleByte = 0x20;
                QbStructLookup = QbTypeLookup;
                endian = "big";
            }
            Reader = new ReadWrite(endian, game, QbTypeLookup, QbStructLookup, console);
            byte[] qbNameHex = Reader.ValueHex(qbName);

            int qbPos = 28;

            using (MemoryStream fullFile = new MemoryStream())
            using (MemoryStream stream = new MemoryStream())
            {
                foreach (QBItem item in qbList)
                {

                    byte[] parentNode = new byte[] { 0x00, consoleByte, QbTypeLookup[item.Info.Type], 0x00 };
                    stream.Write(parentNode, 0, parentNode.Length);
                    byte[] qbID = Reader.ValueHex(item.Name);
                    stream.Write(qbID, 0, qbID.Length);
                    stream.Write(qbNameHex, 0, qbNameHex.Length);

                    try
                    {
                        var (itemData, otherData) = Reader.GetItemData(item.Info.Type, item.Data, (int)stream.Position + qbPos + 8);
                        // itemData is either the data itself (for simple data), or a pointer to the data
                        // otherData is the data if itemData is not simple

                        stream.Write(itemData, 0, itemData.Length);
                        byte[] nextItem = Reader.ValueHex(0);
                        stream.Write(nextItem, 0, nextItem.Length);
                        if (otherData != null)
                        {
                            stream.Write(otherData, 0, otherData.Length);
                        }
                    }
                    catch (ImproperIfBlockException ex)
                    {
                        Console.WriteLine($"Improper if block found in script {item.Name}");
                        Console.WriteLine("This is usually caused by forgetting to place an \"endif\" in the script.");
                        Console.WriteLine("Cancelling compilation.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error compiling {item.Name}");
                        Console.WriteLine("Please copy this log and report it to AddyMills.");
                        throw;
                    }
                    Reader.PadStreamToFour(stream);
                }

                string qbHeader = GetQbHeader(game, Reader.ValueHex((int)stream.Position));
                byte[] qbHeadHex = ReadWrite.HexStringToByteArray(qbHeader);

                byte[] firstFour = Reader.ValueHex(0);
                fullFile.Write(firstFour, 0, 4);
                fullFile.Write(Reader.ValueHex((int)stream.Length + qbPos), 0, 4);
                fullFile.Write(qbHeadHex, 0, qbHeadHex.Length);
                stream.Position = 0; // Reset the position of 'stream' to the beginning
                stream.CopyTo(fullFile); // Copy the contents of 'stream' to 'fullFile'
                byte[] currentContents = fullFile.ToArray();
                return currentContents;
            }
        }
        public static string GetQbHeader(string game, byte[] qbSize)
        {
            if (game != GAME_GHWOR)
            {
                return "1C 08 02 04 10 04 08 0C 0C 08 02 04 14 02 04 0C 10 10 0C 00";
            }
            else
            {
                StringBuilder byteString = new StringBuilder();

                for (int i = 0; i < qbSize.Length; i++)
                {
                    if (i > 0)
                        byteString.Append(" "); // Add space before each byte except the first

                    byteString.AppendFormat("{0:X2}", qbSize[i]);
                }
                string result = byteString.ToString();
                string tempHeader = $"1C 00 00 00 00 00 00 00 {result} 00 00 00 00 00 00 00 00";
                return tempHeader;
            }
        }
        public static List<QBItem> DecompileQbFromFile(string file)
        {
            string fileName = Path.GetFileName(file);
            Match match = Regex.Match(fileName, @"\.([a-zA-Z])?qb", RegexOptions.IgnoreCase);
            if (!match.Success) // Files can be .qb, .sqb, .sqb.ps2, etc.
            {
                return null;
            }
            string fileNoExt = fileName.Substring(0, match.Index);
            string fileExt = Path.GetExtension(file);
            Console.WriteLine($"Decompiling {fileName}");
            string folderPath = Path.GetDirectoryName(file);
            string NewFilePath;
            if (match.Value == ".qb")
            {
                NewFilePath = Path.Combine(folderPath, "Test", $"{fileNoExt}.q");
            }
            else
            {
                char scriptType = match.Value[1];
                NewFilePath = Path.Combine(folderPath, $"{fileNoExt}.{scriptType}.q");
            }
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
            return qbList;
        }

    }
}
