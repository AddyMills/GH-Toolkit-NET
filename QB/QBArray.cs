using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBConstants;

/*
 Do not call these classes/methods outside of the main QB.cs file
 */

namespace GH_Toolkit_Core.QB
{
    public class QBArray
    {
        static bool isNotEmpty(QBItemInfo info)
        {
            return (info.Type != EMPTY);
        }
        [DebuggerDisplay("{FirstItem,nq} Array - {ItemCount} item(s)")]
        public class QBArrayNode
        {
            public QBItemInfo FirstItem { get; set; }
            public uint ItemCount { get; set; }
            public List<object> Items { get; set; }
            public QBArrayNode(MemoryStream stream)
            {
                FirstItem = new QBItemInfo(stream);
                ItemCount = 0;
                Items = new List<object>();
                bool simpleArray = IsSimpleValue(FirstItem.Type);
                if (isNotEmpty(FirstItem))
                {
                    ItemCount = Reader.ReadUInt32(stream); // This is inherited when called from the QB class
                    // If the array has more than 1 item, or is not a simple array, skip to the start of the array.
                    if (ItemCount > 1 || !simpleArray)
                    {
                        uint listStart = Reader.ReadUInt32(stream);
                        stream.Position = listStart;
                    }
                }
                else
                {
                    FirstItem.Type = "Empty";
                    stream.Position += 8; // Empty Array, why both reading the values?
                    return;
                }
                if (simpleArray)
                {
                    for (int i = 0; i < ItemCount; i++)
                    {
                        Items.Add(ReadQBValue(stream, FirstItem.Type));
                    }
                    return;
                }
                else
                {
                    throw new Exception($"{FirstItem.Type} is not yet implemented!");
                }
            }

        }
    }
}
