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

namespace GH_Toolkit_Core.QB
{
    public class QB
    {
        public static bool FlipBytes = true;
        public static Dictionary<uint, string> SongHeaders;
        public static ReadWrite Reader;
        public List<QBItem> Children { get; set; }
        public QB()
        {
            Children = new List<QBItem>();
        }
        private void AddQbChild(QBItem item)
        {
            Children.Add(item);
        }
        [DebuggerDisplay("{Info, nq}: {Name, nq} - {Data}")]
        public class QBItem
        {
            public QBItemInfo Info { get; set; }
            public QBSharedProps Props { get; set; }
            public object Data { get; set; }
            public string Name { get; set; }
            public object? Children { get; set; }
            public QBItem() { }
            public QBItem(MemoryStream stream)
            {
                Info = new QBItemInfo(stream);
                Props = new QBSharedProps(stream, Info.Type);
                if (ReadWrite.IsSimpleValue(Info.Type))
                {
                    Data = Props.DataValue;
                }
                else
                {
                    Data = ReadQBData(stream, Info.Type);
                }
                Name = Props.ID;
            }
            public void AddName(string name)
            {
                Name = name;
            }
            public void AddData(object data)
            {
                Data = data;
            }
            public void AddInfo(string type)
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
            public void CreateQBItem(string name, string value, string type)
            {
                AddName(name);
                Data = ParseData(value, type);
                AddInfo(type);
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
            public QBItemInfo(MemoryStream stream) : base(stream)
            {
                Type = QbType[(byte)(Info >> 16)];
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
                    Type = QbType[infoByte];
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
                case INTEGER:
                    return Reader.ReadInt32(stream);
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
        public static List<float> ParseQBTuple(string[] floats)
        {
            var list = new List<float>();
            foreach (string floatVal in floats)
            {
                if (float.TryParse(floatVal, out float val))
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

        private static readonly Dictionary<byte, string> QbTypeGh3Ps2Struct = new Dictionary<byte, string>()
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
        private static Dictionary<string, byte> flipDict(Dictionary<byte, string> originalDict)
        {
            Dictionary<string, byte> flippedDict = new Dictionary<string, byte>();
            foreach (KeyValuePair<byte, string> item in originalDict)
            {
                flippedDict.Add(item.Value, item.Key);
            }
            return flippedDict;
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
        public static string QbItemText(string itemType, object itemData)
        {
            string itemString;
            if (itemType == FLOAT)
            {
                itemString = Convert.ToSingle(itemData).ToString("0.0");
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
                    test = $"'{itemString.Replace("'", "\\'")}'";
                    break;
                case WIDESTRING:
                    test = $"\"{itemString}\"";
                    break;
                case QBKEY:
                case QSKEY:
                case POINTER:
                    if (itemString.IndexOf(" ", 0) == -1)
                    {
                        test = $"{itemString}";
                    }
                    else
                    {
                        test = $"`{itemString}`";
                    }
                    if (itemType == QSKEY)
                    {
                        test = $"qs({test})";
                    }
                    else if (itemType == POINTER)
                    {
                        test = $"${test}";
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid string found.");
            }

            return test;
        }
        public static string FloatsToText(List<float> floats)
        {
            var formattedFloats = floats.Select(f => f.ToString("0.0"));
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
        enum ParseState
        {
            whitespace,
            inKey,
            inKeyQb, //This is if a key has a space in it
            inString,
            inQbKey,
            inQsKey,
            inComment,
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
        }
        public static string GetParseType(string data)
        {
            if (data.StartsWith("$"))
            {
                return POINTER;
            }
            else if (data.StartsWith("0x"))
            {
                return QBKEY;
            }
            else if (data.Contains('.') && float.TryParse(data, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue))
            {
                return FLOAT;
            }
            else if (int.TryParse(data, out int intValue))
            {
                return INTEGER;
            }
            else
            {
                return QBKEY;
            }
        }
        public static object ParseData(string data, string type)
        {
            switch (type)
            {
                case INTEGER:
                    return int.Parse(data);
                case FLOAT:
                    return float.Parse(data);
                case MULTIFLOAT:
                    return ParseMultiFloat(data);
                case STRING:
                case WIDESTRING:
                    return data;
                case QBKEY:
                case QSKEY:
                case POINTER:
                    if (type == POINTER)
                    {
                        data = data.Substring(1);
                    }
                    return CRC.QBKey(data);
                default:
                    throw new NotImplementedException("Not yet implemented");
            }
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
        public static List<QBItem> ParseQFile(string data)
        {
            ParseLevel root = new ParseLevel(null, ParseState.whitespace, ROOT);
            ParseLevel currLevel = root;
            bool escaped = false;
            QB qbFile = new QB();
            QBItem currItem = new QBItem();
            data += " "; // Safety character to make sure everything gets parsed
            string tmpKey = "";
            string tmpValue = "";
            string tmpType;
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
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
                            case '/':
                                tmpValue = "";
                                currLevel.State = ParseState.inComment;
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
                                if (currLevel.LevelType == STRUCT)
                                {
                                    HandleStructFlag(ref i, ref tmpKey, currLevel);
                                    break;
                                }
                                throw new Exception($"QB Item Key {tmpKey} found without any data!");
                            case ' ':
                            case '\t':
                                if (tmpKey == SCRIPTKEY)
                                {
                                    currLevel.State = ParseState.inValue;
                                }
                                else if (tmpKey.StartsWith("#\"") && tmpKey.EndsWith("\""))
                                {
                                    tmpKey = tmpKey.Substring(2, tmpKey.Length - 3); // Removing #"" from the start and end
                                }
                                else if (tmpKey.StartsWith("`") && tmpKey.EndsWith("`"))
                                {
                                    tmpKey = tmpKey.Substring(1, tmpKey.Length - 2); // Removing ` from the start and end
                                }
                                else
                                {
                                    tmpKey += c;
                                }
                                break;
                            case '=':
                                tmpKey = tmpKey.Trim();
                                currLevel.State = ParseState.inValue;
                                break;
                            default:
                                if (tmpKey.EndsWith(" ") || tmpKey.EndsWith("\t"))
                                {
                                    tmpKey = tmpKey.TrimStart();
                                    if (tmpKey.StartsWith("#\"") || tmpKey.StartsWith("`"))
                                    {
                                        tmpKey += c;
                                    }
                                    else if (currLevel.LevelType == STRUCT)
                                    {
                                        HandleStructFlag(ref i, ref tmpKey, currLevel);
                                    }
                                    else
                                    {
                                        throw new NotSupportedException($"No equals sign found between two values at {tmpKey}");
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
                            case '\r':
                            case '\n':
                            case ' ':
                            case '\t':
                                if (tmpValue == "" && currLevel.LevelType != SCRIPT)
                                {
                                    throw new Exception($"QB Item Key {tmpKey} found without any data!");
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
                                    throw new NotSupportedException($"Unsupported character '(' in value of {tmpKey}");
                                }
                                break;
                            case ')':
                                if (currLevel.LevelType != SCRIPT)
                                {
                                    throw new NotSupportedException($"'{c}' found where it shouldn't be!");
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
                                else if (float.TryParse(tmpValue, out float val))
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
                                    throw new NotSupportedException("Closing bracket } found outside of struct!");
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
                                    throw new NotSupportedException("Closing brace ] found outside of array!");
                                }
                                break;
                            case '\\':
                                if (currLevel.LevelType == SCRIPT)
                                {
                                    escaped = true;
                                }
                                else
                                {
                                    throw new NotSupportedException("\\ character found outside of string or script!");
                                }
                                break;
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
                                    tmpValue = tmpValue.Replace("\t","").Replace(" ", "");
                                }
                                StateSwitch(currLevel);
                                AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, MULTIFLOAT);
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
                                throw new NotSupportedException("Unknown value found in Pair/Vector value");
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
                                tmpKey = new string(c, 1);
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
                                    throw new NotSupportedException("Double quotes cannot appear in strings");
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
                                    throw new InvalidOperationException("Invalid character found after escape character");
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
                                if (tmpKey == SCRIPTKEY)
                                {
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
                                if (tmpValue == "#")
                                {
                                    tmpValue = "";
                                }
                                else
                                {
                                    continue;
                                }
                                break;
                            case ')':
                                AddParseItem(ref currLevel, ref currItem, qbFile, ref tmpKey, ref tmpValue, QSKEY);
                                StateSwitch(currLevel);
                                break;
                            default:
                                tmpValue += c;
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
                                currLevel.State = ParseState.whitespace;
                                break;
                            default:
                                continue;
                        }
                        break;
                }
            }
            return qbFile.Children;
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
            if (tmpKey == "" && tmpValue == "")
            {
                return;
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
                tmpValue = "";
                currLevel.State = ParseState.inQbKey;
                return true;
            }
            else if (c == '`' && tmpValue == "")
            {
                currLevel.State = ParseState.inQbKey;
                return true;
            }
            else if (c == '(' && tmpValue == "")
            {
                currLevel.State = ParseState.inMultiFloat;
                return true;
            }
            return false;
        }
        private static void AddLevel(ref ParseLevel currLevel, ref QBItem currItem, ParseState state, string levelType, ref string tmpKey)
        {
            string tmpString = tmpKey;
            if (currLevel.LevelType == ROOT)
            {
                currItem.AddName(tmpKey);
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
                currItem.AddData(currLevel.Array);
                currItem.AddInfo(ARRAY);
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
                currItem.AddData(currLevel.Struct);
                currItem.AddInfo(STRUCT);
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
            else
            {
                throw new NotImplementedException();
            }
            currLevel = currLevel.Parent;
            StateSwitch(currLevel);
        }
        private static void CloseScript(ref ParseLevel currLevel, ref QBItem currItem, ref QB qbFile)
        {
            currItem.AddData(currLevel.Script);
            currItem.AddInfo(SCRIPT);
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
        public static void CompileQbFile(List<QBItem> file, string qbName, string game = "GH3", string console = "360")
        {
            string qbHeader = "1C 08 02 04 10 04 08 0C 0C 08 02 04 14 02 04 0C 10 10 0C 00";

            byte consoleByte;
            Dictionary<string, byte> QbTypeLookup = flipDict(QbType);
            Dictionary<string, byte> QbStructLookup;
            string endian;

            if (console == "PS2")
            {
                consoleByte = 0x04;
                if (game == "GH3")
                {
                    QbStructLookup = flipDict(QbTypeGh3Ps2Struct);
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
            Reader = new ReadWrite(endian, game, QbTypeLookup, QbStructLookup);
            byte[] qbHeadHex = ReadWrite.HexStringToByteArray(qbHeader);
            byte[] qbNameHex = Reader.ValueHex(qbName);

            int qbPos = 28;

            using (MemoryStream fullFile = new MemoryStream())
            using (MemoryStream stream = new MemoryStream())
            {
                foreach (QBItem item in file)
                {
                    byte[] parentNode = new byte[] { 0x00, consoleByte, QbTypeLookup[item.Info.Type], 0x00 };
                    stream.Write(parentNode, 0, parentNode.Length);
                    byte[] qbID = Reader.ValueHex(item.Name);
                    stream.Write(qbID, 0, qbID.Length);
                    stream.Write(qbNameHex, 0, qbNameHex.Length);

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
                    Reader.PadStreamToFour(stream);
                }
                byte[] firstFour = Reader.ValueHex(0);
                fullFile.Write(firstFour, 0, 4);
                fullFile.Write(Reader.ValueHex((int)stream.Length + qbPos), 0, 4);
                fullFile.Write(qbHeadHex, 0, qbHeadHex.Length);
                stream.Position = 0; // Reset the position of 'stream' to the beginning
                stream.CopyTo(fullFile); // Copy the contents of 'stream' to 'fullFile'
                byte[] currentContents = fullFile.ToArray();
            }
        }

        public static void DecompileQbFromFile(string file)
        {
            string fileName = Path.GetFileName(file);
            Match match = Regex.Match(fileName, @"\.([a-zA-Z])?qb", RegexOptions.IgnoreCase);
            if (!match.Success) // Files can be .qb, .sqb, .sqb.ps2, etc.
            {
                Console.WriteLine($"Skipping {fileName}");
                return;
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
            QbToText(qbList, NewFilePath);
        }

    }
}
