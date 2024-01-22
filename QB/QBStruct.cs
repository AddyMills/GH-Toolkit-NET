using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBConstants;
using GH_Toolkit_Core.Methods;
using static GH_Toolkit_Core.MIDI.MidiDefs;
using GH_Toolkit_Core.Debug;
using System.Globalization;

namespace GH_Toolkit_Core.QB
{
    public class QBStruct
    {
        [DebuggerDisplay("{ID}")]
        public class QBStructProps
        {
            public string ID { get; set; }
            public object DataValue { get; set; }
            public uint NextItem { get; set; }
            public QBStructProps(string key, object value) 
            { 
                ID = key;
                DataValue = value;
            }
            public QBStructProps(MemoryStream stream, string itemType) 
            {
                ID = ReadQBKey(stream);
                if (ID == "0x00000000")
                {
                    ID = "Flag";
                }
                DataValue = ReadQBValue(stream, itemType);
                NextItem = Reader.ReadUInt32(stream);
            }
        }
        [DebuggerDisplay("{Info,nq} {Props,nq} - {DataKey}")]
        public class QBStructItem
        {
            public QBStructInfo Info { get; set; }
            public QBStructProps Props { get; set; }
            public object Data { get; set; }
            public object? Children { get; set; }
            public object? Parent { get; set; }
            // Property to get QB key string
            public object DataKey
            {
                get
                {
                    if (Data is string key && key.StartsWith("0x"))
                    {
                        // Remove the "0x" prefix
                        string hexValue = key.Substring(2);
                        if (uint.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint result))
                        {
                            return DebugReader.DbgString(result);
                        }
                    }
                    return Data;
                }
            }
            public QBStructItem(string key, string value, string type)
            {
                Props = new QBStructProps(key, ParseData(value, type));
                Data = Props.DataValue;
                if (type == MULTIFLOAT && Data is List<float> listFloat)
                {
                    if (listFloat.Count < 2 || listFloat.Count > 3)
                    {
                        throw new ArgumentException("List of float values does not contain only 2 or 3 items.");
                    }
                    type = listFloat.Count == 2 ? PAIR : VECTOR;
                }
                Info = new QBStructInfo(type); 
            }
            public QBStructItem(string key, int value) // Construct an integer item
            {
                Props = new QBStructProps(key, value);
                Data = Props.DataValue;
                Info = new QBStructInfo(INTEGER);
            }
            public QBStructItem(string key, QBArrayNode value) // Array
            {
                Info = new QBStructInfo(ARRAY);
                Props = new QBStructProps(key, value);
                Data = Props.DataValue;
            }
            public QBStructItem(string key, QBStructData value) // Struct
            {
                Info = new QBStructInfo(STRUCT);
                Props = new QBStructProps(key, value);
                Data = Props.DataValue;
            }
            public QBStructItem(MemoryStream stream)
            {
                Info = new QBStructInfo(stream);
                Props = new QBStructProps(stream, Info.Type);
                if (ReadWrite.IsSimpleValue(Info.Type))
                {
                    Data = Props.DataValue;
                }
                else
                {
                    Data = ReadQBData(stream, Info.Type);
                }
            }
        }
        [DebuggerDisplay("{Items.Count} item(s)")]
        public class QBStructData // This expects the start to be a marker header, no prop data
        {
            public uint HeaderMarker { get; set; }
            public uint ItemOffset { get; set; } // Can be first or next
            public List<object> Items { get; set; }
            private int ItemCount { get; set; } // Debug only
            public QBStructData()
            {
                Items = new List<object>();
            }
            public QBStructData(MemoryStream stream) // From bytes
            {
                HeaderMarker = Reader.ReadUInt32(stream);
                ItemOffset = Reader.ReadUInt32(stream);
                Items = new List<object>();
                uint nextItem = ItemOffset;
                if (ItemOffset != 0)
                {
                    while (nextItem > 0)
                    {
                        stream.Position = nextItem;
                        var item = new QBStructItem(stream);
                        Items.Add(item);
                        nextItem = item.Props.NextItem;
                    }
                    //throw new Exception("Not yet implemented!");
                }
                ItemCount = Items.Count;
            }
            public void AddToStruct(string key, int value) // Integer item
            {
                var item = new QBStructItem(key, value);
                Items.Add(item);
            }
            public void AddVarToStruct(string key, string value, string type)
            {
                var item = new QBStructItem(key, value, type);
                Items.Add(item);
            }
            public void AddArrayToStruct(string key, QBArrayNode value)
            {
                var item = new QBStructItem(key, value);
                Items.Add(item);
            }
            public void AddStructToStruct(string key, QBStructData value)
            {
                var item = new QBStructItem(key, value);
                Items.Add(item);
            }
            // Params for SetBlendTime scripts
            public void MakeLightBlendParams(string eventData)
            {
                if (int.TryParse(eventData, out int blendTime))
                {
                    AddToStruct(TIME, blendTime);
                }
                else
                {
                    AddVarToStruct(TIME, eventData, FLOAT);
                }
            }
            // Params for 2-parameter song scripts
            public void MakeTwoParams(string actor, string eventData, string paramType)
            {
                AddVarToStruct(NAME, actor, QBKEY);
                AddVarToStruct(paramType, eventData, QBKEY);
            }
            // Method to add all items from a string to the struct as flags
            public void AddFlags(string flags)
            {
                string[] flagArray = flags.Split(' ');
                // Return if there are no flags
                if (flagArray.Length == 0)
                {
                    return;
                }
                foreach (string flag in flagArray)
                {
                    if (int.TryParse(flag, out int repeat))
                    {
                        AddToStruct(REPEAT_COUNT, repeat);
                    }
                    else
                    {
                        AddVarToStruct(FLAG, flag, QBKEY);
                    }
                }
            }
            public void StructToText(StreamWriter writer, int level = 1)
            {
                string indent = new string('\t', level);
                string key;
                foreach (QBStructItem item in Items)
                {
                    key = item.Props.ID == FLAG ? "" : $"{item.Props.ID} = ";
                    if (item.Data is QBArrayNode arrayNode)
                    {
                        writer.WriteLine(indent + $"{key}[");
                        arrayNode.ArrayToText(writer, level + 1);
                        writer.WriteLine(indent + "]");
                    }
                    else if (item.Data is QBStructData structNode)
                    {
                        writer.WriteLine(indent + $"{key}{{");
                        structNode.StructToText(writer, level + 1);
                        writer.WriteLine(indent + "}");
                    }
                    else if (item.Data is List<float> floats)
                    {
                        writer.WriteLine(indent + $"{key}{FloatsToText(floats)}");
                    }
                    /*else if (item.Props.ID == FLAG)
                    {
                        writer.WriteLine(indent + $"{QbItemText(item.Info.Type, item.Data.ToString())}");
                    }*/
                    else
                    {
                        writer.WriteLine(indent + $"{key}{QbItemText(item.Info.Type, item.Data.ToString())}");
                    }
                }
            }
            public string StructToScript()
            {

                string returnString = "";
                foreach (QBStructItem item in Items)
                {
                    string data = "";
                    string name = item.Props.ID;
                    if (item.Data is QBArrayNode arrayNode)
                    {
                        data += arrayNode.ArrayToScript();
                    }
                    else if (item.Data is QBStructData structNode)
                    {
                        data += $"{{{structNode.StructToScript()}}}";
                    }
                    else if (item.Data is List<float> floats)
                    {
                        data = FloatsToText(floats);
                    }
                    else
                    {
                        data = QbItemText(item.Info.Type, item.Data.ToString());
                    }
                    if (name != FLAG)
                    {
                        returnString += $"{name} = {data} ";
                    }
                    else
                    {
                        returnString += $"{data} ";
                    }
                }
                return returnString.Trim();
            }
        }
    }
}
