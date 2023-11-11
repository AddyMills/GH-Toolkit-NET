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
            public QBStructProps(MemoryStream stream, string itemType) 
            {
                ID = ReadQBKey(stream);
                if (ID == "0x0")
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
        [DebuggerDisplay("{ItemCount} item(s)")]
        public class QBStructData // This expects the start to be a marker header, no prop data
        {
            public uint HeaderMarker { get; set; }
            public uint ItemOffset { get; set; } // Can be first or next
            public List<object> Items { get; set; }
            private int ItemCount { get; set; } // Debug only

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
                string data = "";
                foreach (QBStructItem item in Items)
                {
                    if (item.Data is QBArrayNode arrayNode)
                    {
                        return data;
                    }
                    else if (item.Data is QBStructData structNode)
                    {
                        return data;
                    }
                    else if (item.Data is List<float> floats)
                    {
                        data += $"{item.Props.ID} = {FloatsToText(floats)} ";
                    }
                    else
                    {
                        data += $"{item.Props.ID} = {QbItemText(item.Info.Type, item.Data.ToString())} ";
                    }
                }
                return data.Trim();
            }
            public QBStructData() // From text
            {

            }
        }
    }
}
