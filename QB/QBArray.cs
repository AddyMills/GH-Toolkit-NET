using GH_Toolkit_Core.Methods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBArray;
using static GH_Toolkit_Core.QB.QBConstants;
using static GH_Toolkit_Core.QB.QBStruct;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        [DebuggerDisplay("{FirstItem,nq} Array - {Items.Count} item(s)")]
        public class QBArrayNode
        {
            public QBItemInfo FirstItem { get; set; }
            public uint ItemCount { get; set; }
            public List<object> Items { get; set; }
            public object? Children { get; set; }
            public object? Parent { get; set; }
            public QBArrayNode()
            {
                Items = new List<object>();
            }
            private void SetFirstItem(string type)
            {
                FirstItem = new QBItemInfo(type);
            }
            public void AddParseToArray(string value, string type)
            {
                Items.Add(ParseData(value, type));
                if (type == MULTIFLOAT)
                {
                    type = ParseMultiFloatType(value);
                }
                if (FirstItem == null)
                {
                    SetFirstItem(type);
                }
                else if (type != FirstItem.Type)
                {
                    throw new ArrayTypeMismatchException($"{value} of type {type} does not match elements in array of type {FirstItem.Type}");
                }
            }
            public void AddArrayToArray(QBArrayNode value) // When parsing from text
            {
                if (FirstItem == null)
                {
                    SetFirstItem(ARRAY);
                }
                else if (ARRAY != FirstItem.Type)
                {
                    throw new ArrayTypeMismatchException($"{ARRAY} does not match elements in array of type {FirstItem.Type}");
                }
                Items.Add(value);
            }
            public void AddStructToArray(QBStructData value) // When parsing from text
            {
                if (FirstItem == null)
                {
                    SetFirstItem(STRUCT);
                }
                else if (STRUCT != FirstItem.Type)
                {
                    throw new ArrayTypeMismatchException($"{STRUCT} does not match elements in array of type {FirstItem.Type}");
                }
                Items.Add(value);
            }
            public void MakeEmpty()
            {
                SetFirstItem(EMPTY);
            }
            public QBArrayNode(MemoryStream stream)
            {
                FirstItem = new QBItemInfo(stream);
                ItemCount = 0;
                Items = new List<object>();
                bool simpleArray = ReadWrite.IsSimpleValue(FirstItem.Type);
                uint listStart = 0;
                if (isNotEmpty(FirstItem))
                {
                    ItemCount = Reader.ReadUInt32(stream); // This is inherited when called from the QB class
                    // If the array has more than 1 item, or is not a simple array, skip to the start of the array.
                    if (ItemCount > 1 || !simpleArray)
                    {
                        listStart = Reader.ReadUInt32(stream);
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
                    List<uint> startList = new List<uint>();
                    if (ItemCount == 1)
                    {
                        // If there's only one item, add listStart directly without reading from stream
                        startList.Add(listStart);
                    }
                    else
                    {
                        // If there are multiple items, read each one from the stream
                        for (uint i = 0; i < ItemCount; i++)
                        {
                            startList.Add(Reader.ReadUInt32(stream));
                        }
                    }

                    // Define a Func delegate that takes a Stream and returns the appropriate type
                    Func<MemoryStream, object> createItem = GetItemFunction(FirstItem.Type);

                    foreach (uint start in startList)
                    {
                        stream.Position = start;
                        Items.Add(createItem(stream));
                    }

                }
            }
            public void ArrayToText(StreamWriter writer, int level = 1)
            {
                string indent = new string('\t', level);
                switch (FirstItem.Type)
                {
                    case ARRAY:
                        foreach (QBArrayNode item in Items)
                        {
                            writer.WriteLine(indent + "[");
                            item.ArrayToText(writer, level + 1);
                            writer.WriteLine(indent + "]");
                        }
                        break;
                    case STRUCT:
                        foreach (QBStructData item in Items)
                        {
                            writer.WriteLine(indent + "{");
                            item.StructToText(writer, level + 1);
                            writer.WriteLine(indent + "}");
                        }
                        break;
                    case VECTOR:
                    case PAIR:
                        foreach(List<float> list in Items)
                        {
                            writer.WriteLine(indent + FloatsToText(list));
                        }
                        break;
                    default:
                        foreach (object item in Items)
                        {
                            writer.WriteLine(indent + QbItemText(FirstItem.Type, item.ToString()));
                        }
                        break;
                }
            }
            public string ArrayToScript()
            {
                string data = "";
                switch (FirstItem.Type)
                {
                    case ARRAY:
                        foreach (QBArrayNode item in Items)
                        {
                            data += item.ArrayToScript();
                        }
                        break;
                    case STRUCT:
                        foreach (QBStructData item in Items)
                        {
                            data += item.StructToScript();
                        }
                        break;
                    case VECTOR:
                    case PAIR:
                        foreach (List<float> list in Items)
                        {
                            data += $"{FloatsToText(list)} ";
                        }
                        break;
                    default:
                        foreach (object item in Items)
                        {
                            data += $"{QbItemText(FirstItem.Type, item.ToString())} ";;
                        }
                        break;
                }
                return $"[{data.TrimEnd()}]";
            }
        }
    }
}
