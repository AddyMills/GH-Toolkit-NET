using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.PAK;

namespace GH_Toolkit_Core
{
    public class QB
    {
        public class QbEntry
        {
            
        }
        private class QbHeader
        {
            public uint Flags { get; set; }
            public uint FileSize { get; set; }

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
            {0x1A, "QbKeyGlobal" },
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
            {0x35, "QbKeyGlobal" },
        };


        public static Dictionary<string, uint[]> QbNodeHeaders = new Dictionary<string, uint[]>()
        {
                                                        //Wii-PC-360      PS2
            { "Flag", new uint[]                        { 0x00000000, 0x00000000 } },
            { "SectionInteger", new uint[]              { 0x00200100, 0x00010400 } },
            { "SectionFloat", new uint[]                { 0x00200200, 0x00020400 } },
            { "SectionString", new uint[]               { 0x00200300, 0x00030400 } },
            { "SectionStringW", new uint[]              { 0x00200400, 0x00040400 } },
            { "SectionFloatsX2", new uint[]             { 0x00200500, 0x00050400 } },
            { "SectionFloatsX3", new uint[]             { 0x00200600, 0x00060400 } },
            { "SectionScript", new uint[]               { 0x00200700, 0x00070400 } },
            { "SectionStruct", new uint[]               { 0x00200A00, 0x000A0400 } },
            { "SectionArray", new uint[]                { 0x00200C00, 0x000C0400 } },
            { "SectionQbKey", new uint[]                { 0x00200D00, 0x000D0400 } },
            { "SectionQbKeyString", new uint[]          { 0x00201A00, 0x00041A00 } },
            { "SectionStringPointer", new uint[]        { 0x00201B00, 0x001A0400 } },
            { "SectionQbKeyStringQs", new uint[]        { 0x00201C00, 0x001C0400 } },
            { "ArrayInteger", new uint[]                { 0x00010100, 0x00010100 } },
            { "ArrayFloat", new uint[]                  { 0x00010200, 0x00020100 } },
            { "ArrayString", new uint[]                 { 0x00010300, 0x00030100 } },
            { "ArrayStringW", new uint[]                { 0x00010400, 0x00040100 } },
            { "ArrayFloatsX2", new uint[]               { 0x00010500, 0x00050100 } },
            { "ArrayFloatsX3", new uint[]               { 0x00010600, 0x00060100 } },
            { "ArrayStruct", new uint[]                 { 0x00010A00, 0x000A0100 } },
            { "ArrayArray", new uint[]                  { 0x00010C00, 0x000C0100 } },
            { "ArrayQbKey", new uint[]                  { 0x00010D00, 0x000D0100 } },
            { "ArrayQbKeyString", new uint[]            { 0x00011A00, 0x001A0100 } },
            { "ArrayStringPointer", new uint[]          { 0x00011B00, 0x001B0100 } },
            { "ArrayQbKeyStringQs", new uint[]          { 0x00011C00, 0x001C0100 } },
            { "StructItemInteger", new uint[]           { 0x00810000, 0x00000300 } },
            { "StructItemFloat", new uint[]             { 0x00820000, 0x00000500 } },
            { "StructItemString", new uint[]            { 0x00830000, 0x00000700 } },
            { "StructItemStringW", new uint[]           { 0x00840000, 0x00000900 } },
            { "StructItemFloatsX2", new uint[]          { 0x00850000, 0x00000B00 } },
            { "StructItemFloatsX3", new uint[]          { 0x00860000, 0x00000D00 } },
            { "StructItemStruct", new uint[]            { 0x008A0000, 0x00001500 } },
            { "StructItemArray", new uint[]             { 0x008C0000, 0x00001900 } },
            { "StructItemQbKey", new uint[]             { 0x008D0000, 0x00001B00 } },
            { "StructItemQbKeyString", new uint[]       { 0x009A0000, 0x00003500 } },
            { "StructItemStringPointer", new uint[]     { 0x009B0000, 0xFFFFFFFF } },
            { "StructItemQbKeyStringQs", new uint[]     { 0x009C0000, 0xFFFFFFFF } },
            { "Floats", new uint[]                      { 0x00010000, 0x00000100 } },
            { "StructHeader", new uint[]                { 0x00000100, 0x00010000 } }
        };
        public static Dictionary<uint, string> QbTypeNext { get; private set; }
        public static Dictionary<uint, string> QbTypePS2 { get; private set; }

        static QB()
        {
            QbTypeNext = GetQBType("360");
            QbTypePS2 = GetQBType("PS2");
        }

        public static Dictionary<uint, string> GetQBType(string console) // Might turn this into a reverse-lookup dictionary
        {
            byte selector = 0;
            Dictionary<uint, string> keyValuePairs = new Dictionary<uint, string>();
            if (console == "PS2")
            {
                selector = 1;
            }
            foreach (var kvp in QbNodeHeaders) 
            {
                keyValuePairs[kvp.Value[selector]] = kvp.Key;
            }
            return keyValuePairs;
        }
        private static void ReadQbHeader(MemoryStream stream)
        {

        }
        public static void DecompileQbFile(string file)
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
            List<QbEntry> qbSections = new List<QbEntry>();
            if (fileName.Contains(songCheck))
            {
                songName = fileName.Substring(0, fileName.IndexOf(songCheck));
            }
            byte[] qb_bytes = File.ReadAllBytes(file);
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
        }
    }
}
