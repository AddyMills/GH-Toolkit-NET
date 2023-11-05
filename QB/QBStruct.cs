using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static GH_Toolkit_Core.QB.QB;

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
        [DebuggerDisplay("{Props} - {Data}")]
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
        [DebuggerDisplay("Struct - {ItemCount} item(s)")]
        public class QBStructData // This expects the start to be a marker header, no prop data
        {
            public uint HeaderMarker { get; set; }
            public uint ItemOffset { get; set; } // Can be first or next
            public List<object> Items { get; set; }
            private int ItemCount { get; set; } // Debug only

            public QBStructData(MemoryStream stream)
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
        }
    }
}
