﻿using System;
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
        [DebuggerDisplay("{Info,nq} {Props,nq} - {Data}")]
        public class QBStructItem
        {
            public QBStructInfo Info { get; set; }
            public QBStructProps Props { get; set; }
            public object Data { get; set; }
            public object? Children { get; set; }
            public object? Parent { get; set; }
            public QBStructItem(string key, string value, string type)
            {
                Info = new QBStructInfo(type);
                Props = new QBStructProps(key, ParseData(value, type));
                Data = Props.DataValue;
            }
            public QBStructItem(string key, QBArrayNode value) // Array
            {
                Info = new QBStructInfo(ARRAY);
                Props = new QBStructProps(key, value);
                Data = Props.DataValue;
            }
            public QBStructItem(string key, QBStructData value) // Array
            {
                Info = new QBStructInfo(STRUCT);
                Props = new QBStructProps(key, value);
                Data = Props.DataValue;
            }
            public QBStructItem(MemoryStream stream)
            {
                Info = new QBStructInfo(stream);
                Props = new QBStructProps(stream, Info.Type);
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
            public void StructToText(StreamWriter writer, int level = 1)
            {
                string indent = new string('\t', level);
                foreach (QBStructItem item in Items)
                {
                    if (item.Data is QBArrayNode arrayNode)
                    {
                        writer.WriteLine(indent + $"{item.Props.ID} = [");
                        arrayNode.ArrayToText(writer, level + 1);
                        writer.WriteLine(indent + "]");
                    }
                    else if (item.Data is QBStructData structNode)
                    {
                        writer.WriteLine(indent + $"{item.Props.ID} = {{");
                        structNode.StructToText(writer, level + 1);
                        writer.WriteLine(indent + "}");
                    }
                    else if (item.Data is List<float> floats)
                    {
                        writer.WriteLine(indent + $"{item.Props.ID} = {FloatsToText(floats)}");
                    }
                    else
                    {
                        writer.WriteLine(indent + $"{item.Props.ID} = {QbItemText(item.Info.Type, item.Data.ToString())}");
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
