using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBConstants;
using System.Diagnostics;
using System.Data.SqlTypes;
using GH_Toolkit_Core.Methods;
using System.IO;
using GH_Toolkit_Core.Debug;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using GH_Toolkit_Core.Checksum;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Emit;

namespace GH_Toolkit_Core.QB
{
    public class QBScript
    {
        enum State
        {
            inDefault,
            inRandom
        }
        public static ReadWrite ScriptReader = new ReadWrite("little"); // Scripts are always little endian, but qbkeys within structs are not...
        [DebuggerDisplay("{ScriptSize} bytes ({CompressedSize} compressed)")]
        public class QBScriptData
        {
            public string ScriptCRC { get; set; }
            public uint ScriptSize { get; set; }
            public uint CompressedSize { get; set; }
            public byte[] CompressedData { get; set; }
            public byte[] ScriptData { get; set; }
            public List<object> ScriptParsed { get; set; }
            public QBScriptData(MemoryStream stream)
            { 
                ScriptCRC = ReadQBKey(stream);
                ScriptSize = Reader.ReadUInt32(stream);
                CompressedSize = Reader.ReadUInt32(stream);
                CompressedData = ReadCompScript(stream, (int)CompressedSize);
                if (ScriptSize != CompressedSize)
                {
                    Lzss lz = new Lzss();
                    ScriptData = lz.Decompress(CompressedData);
                }
                else
                {
                    ScriptData = CompressedData;
                }
                //byte[] tryArray = CompressedData.Where(b => b != 0x01 && b != 0x24).ToArray();
                //string tryCrc = CRC.GenQBKey(tryArray);
                /*
                 * Trying to RE the script CRC value
                byte[] tryArray = new byte[ScriptSize];
                for (int i = 0; i < ScriptSize; i++)
                {
                    tryArray[i] = ScriptData[i];
                }
                byte[] tryArray2 = new byte[CompressedSize];
                for (int i = 0; i < CompressedSize; i++)
                {
                    tryArray2[i] = CompressedData[i];
                }
                string tryCrc = CRC.GenQBKey(tryArray);
                string tryCrc2 = CRC.GenQBKey(tryArray2);
                */
                ScriptParsed = ParseScript(ScriptData);
            }
            public string GetIndent(int level)
            {
                return new string('\t', level);
            }
            private class RandomState
            {
                private uint RandomEntries;
                private uint MaxEntries;
                private bool AppendEntry;
                private bool NoClose;
                private bool MultiLine;
                public RandomState()
                {
                    RandomEntries = 0;
                    MaxEntries = 0;
                    AppendEntry = false; // To keep track of multiline random entries and adding @ if true
                    NoClose = false; // To keep track of closing parentheses in random events
                    MultiLine = false;
                }
                public void setEntries(uint entries)
                {
                    RandomEntries = entries;
                    MaxEntries = entries;
                }
                public void useEntry()
                {
                    RandomEntries--;
                }
                public void setAppend(bool status)
                {
                    AppendEntry = status;
                }
                public void setNoClose(bool status)
                {
                    NoClose = status;
                }
                public void setMulti(bool status)
                {
                    MultiLine = status;
                }
                public uint getEntries()
                {
                    return RandomEntries;
                }
                public uint getMax()
                {
                    return MaxEntries;
                }
                public bool getAppend()
                {
                    return AppendEntry;
                }
                public bool getNoClose()
                {
                    return NoClose;
                }
                public bool getMulti()
                {
                    return MultiLine;
                }
            }
            public void DeleteTabs(StringBuilder sb)
            {
                int i = sb.Length - 1;
                while (i >= 0 && sb[i] == '\t')
                {
                    i--;
                }

                // Remove all characters from i + 1 to the end
                if (i < sb.Length - 1)
                {
                    sb.Remove(i + 1, sb.Length - i - 1);
                }

                // Now sb contains the string without the trailing tab characters
            }
            public static bool isArgument = false;
            public static string stringIndent = "\t";
            public void StringParser(object item, ref int level, StringBuilder builder, StringBuilder currentLine)
            {
                switch (item)
                {
                    case NEWLINE:
                        // Trim trailing whitespace and append the NEWLINE
                        builder.Append(currentLine.ToString().TrimEnd());
                        builder.AppendLine();
                        currentLine.Clear();
                        currentLine.Append(stringIndent);
                        break;
                    case DOT:
                        builder.Append(currentLine.ToString().TrimEnd());
                        currentLine.Clear();
                        currentLine.Append(item);
                        break;
                    case COLON:
                        currentLine.Append(item);
                        break;
                    case ARGUMENT:
                        isArgument = true;
                        break;
                    case ENDIF:
                        currentLine.Clear(); // Clear the current line buffer
                        level--;
                        currentLine.Append(GetIndent(level)); // Update the indent
                        currentLine.Append("endif");
                        break;
                    case SWITCH:
                        currentLine.Append($"{item} ");
                        level++;
                        break;
                    case LEFTBKT:
                    case LEFTBRACE:
                    case LEFTPAR:
                        currentLine.Append(item);
                        level++;
                        break;
                    case RIGHTBKT:
                    case RIGHTBRACE:
                    case RIGHTPAR:
                    case ENDSWITCH:
                        level--;
                        if (currentLine.ToString() == stringIndent)
                        {
                            currentLine.Clear();
                            currentLine.Append(GetIndent(level));
                        }
                        else
                        {
                            builder.Append(currentLine.ToString().TrimEnd());
                            currentLine.Clear();
                        }
                        currentLine.Append(item);
                        break;
                    default:
                        currentLine.Append($"{item} ");
                        break;
                }
            }
            
            public string ScriptToText(int level = 1)
            {
                StringBuilder builder = new StringBuilder();
                StringBuilder currentLine = new StringBuilder();
                State state = new State();
                state = State.inDefault;
                isArgument = false;
                RandomState rState = new RandomState();
                foreach (object item in ScriptParsed)
                {
                    stringIndent = GetIndent(level);
                    switch (state) { 
                        case State.inDefault:
                            if (item is string)
                            {
                                StringParser(item, ref level, builder, currentLine);
                            }
                            else if (item is ScriptNode node)
                            {
                                string currString = currentLine.ToString();
                                if (currString.EndsWith("\t") && currString != stringIndent)
                                {
                                    DeleteTabs(currentLine);
                                }
                                string data = node.NodeToText(ref isArgument);
                                currentLine.Append(data);
                            }
                            else if (item is Conditional conditional)
                            {
                                switch (conditional.Name)
                                {
                                    case IF:
                                    case FASTIF:
                                        level++;
                                        currentLine.Append("if ");
                                        break;
                                    case ELSEIF:
                                        currentLine.Clear(); // Clear the current line buffer
                                        currentLine.Append(GetIndent(level - 1)); // Update the indent
                                        currentLine.Append("elseif ");
                                        break;
                                    case ELSE:
                                    case FASTELSE:
                                        currentLine.Clear(); // Clear the current line buffer
                                        currentLine.Append(GetIndent(level - 1)); // Update the indent
                                        currentLine.Append("else ");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            else if (item is ScriptTuple tuple)
                            {
                                currentLine.Append($"{FloatsToText(tuple.Data)} ");
                            }
                            else if (item is ScriptRandom random)
                            {
                                //level++;
                                state = State.inRandom;
                                rState.setEntries(random.Entries);
                                currentLine.Append($"{random.Name} (");
                                rState.setAppend(true);
                            }
                            else
                            {
                                throw new NotImplementedException("Not implemented");
                            }
                            break;
                        case State.inRandom:
                            if (rState.getAppend() && !(rState.getMax() == rState.getEntries()))
                            {
                                currentLine.Append("@ ");
                                rState.setAppend(false);
                            }
                            if (item is ScriptNode randnode)
                            {
                                
                                string data = randnode.NodeToText(ref isArgument);
                                currentLine.Append($"{data}");
                                if (rState.getEntries() == 1 && !rState.getMulti())
                                {
                                    builder.Append(currentLine.ToString().TrimEnd());
                                    currentLine.Clear();
                                    currentLine.Append(") ");
                                    rState.useEntry();
                                }
                            }
                            else if (item is ScriptLongJump jump)
                            {
                                rState.useEntry();
                                if (jump.Jump == 0)
                                {
                                    currentLine.Append("@ ");
                                    rState.useEntry();
                                }
                                rState.setAppend(true);
                            }
                            else if (item == NEWLINE)
                            {
                                if (rState.getEntries() > 1 && !rState.getMulti())
                                {
                                    rState.setMulti(true);
                                    level++;
                                }
                                builder.Append(currentLine.ToString().TrimEnd()); // This line here needs to be changed to add the space for multi-line randoms. Might not be needed
                                if (currentLine.ToString() == GetIndent(level))
                                {
                                    currentLine.Clear();
                                    rState.useEntry();
                                    level--;
                                    builder.Append(GetIndent(level));
                                    builder.Append(")");
                                    builder.AppendLine();
                                    rState.setNoClose(true);
                                }
                                else
                                {
                                    builder.AppendLine();
                                    currentLine.Clear();
                                    currentLine.Append(GetIndent(level));
                                    if (rState.getAppend() && (rState.getMax() == rState.getEntries()))
                                    {
                                        currentLine.Append("@ ");
                                        rState.setAppend(false);
                                    }
                                }

                            }
                            else if (item is string)
                            {
                                StringParser(item, ref level, builder, currentLine);
                            }
                            else
                            {
                                throw new NotImplementedException("Not implemented");
                            }
                            if (rState.getEntries() <= 0)
                            {
                                // builder.Append(currentLine.ToString().TrimEnd());

                                currentLine.Append(GetIndent(level));
                                state = State.inDefault;
                                rState = new RandomState();
                            }
                            break;
                    }  
                }
                builder.AppendLine("endscript");
                return builder.ToString();
            }
        }
        [DebuggerDisplay("{Type,nq} - {Data,nq}")]
        public class ScriptNode
        {
            public string Type { get; set; }
            public object Data { get; set; }
            public ScriptNode(string nodeType, object data)
            {
                Type = nodeType;
                Data = data;
            }
            public string NodeToText(ref bool isArgument) 
            {
                string dataString;
                if (Data is ScriptNodeStruct structData)
                {
                    dataString = $"\\{{{structData.Data.StructToScript()}}}";
                }
                else
                {
                    dataString = QbItemText(Type, Data.ToString());
                }
                if (isArgument)
                {
                    if (dataString.StartsWith("$"))
                    {
                        dataString = $"$<{dataString.Substring(1)}>";
                    }
                    else
                    {
                        dataString = $"<{dataString}>";
                    }
                    isArgument = false;
                }
                dataString += " ";
                return dataString;
            }
        }
        [DebuggerDisplay("{Data}")]
        public class ScriptNodeStruct
        {
            public uint ByteSize { get; set; }
            public QBStruct.QBStructData Data { get; set; }
            public ScriptNodeStruct(MemoryStream stream)
            {
                ByteSize = ScriptReader.ReadUInt16(stream);
                ReadWrite.MoveToModFour(stream);
                byte[] buffer = new byte[ByteSize];
                stream.Read(buffer, 0, buffer.Length);
                using (MemoryStream structStream = new MemoryStream(buffer))
                {
                    Data = new QBStruct.QBStructData(structStream);
                }
                
            }
        }
        [DebuggerDisplay("{Name}")]
        public class Conditional
        {
            public string Name { get; set; }
            public uint Jump { get; set; }
            public Conditional(string name, uint jump)
            {
                Name = name;
                Jump = jump;
            }
        }
        [DebuggerDisplay("{Type, nq} - {Text, nq}")]
        public class ScriptString
        {
            public string Type { get; set; }
            public string Text { get; set; }
            public ScriptString(string name, uint length, MemoryStream stream)
            {
                Type = name;
                byte[] buffer = new byte[length];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                switch (Type)
                {
                    case STRING:
                        Text = Encoding.UTF8.GetString(buffer);
                        break;
                    case WIDESTRING:
                        Text = Encoding.BigEndianUnicode.GetString(buffer); 
                        break;
                }
            }
        }
        [DebuggerDisplay("{Type, nq} - {ListDisplay, nq}")]
        public class ScriptTuple
        {
            public string Type { get; set; }
            public List<float> Data { get; set; }
            public ScriptTuple(string type, MemoryStream stream)
            {
                Type = type;
                uint floats = (uint)(type == PAIR ? 2 : 3);
                Data = new List<float>();
                for (int i = 0; i < floats; i++)
                {
                    Data.Add(ScriptReader.ReadFloat(stream));
                }
            }
            private string ListDisplay
            {
                get { return string.Join(", ", Data); }
            }
        }
        [DebuggerDisplay("{Name} - {Entries} Entries")]
        public class ScriptRandom
        {
            public string Name { get; set; }
            public uint Entries { get; set; }
            public ScriptRandom(string random, MemoryStream stream)
            {
                Name = random;
                Entries = ScriptReader.ReadUInt32(stream);
                stream.Position += (Entries * 2); // Skip weights
                stream.Position += (Entries * 4); // Skip offsets
            }
        }
        [DebuggerDisplay("Long Jump - {Jump} Bytes")]
        public class ScriptLongJump
        { 
            public uint Jump { get; set; }
            public ScriptLongJump(uint jump) 
            {
                Jump = jump;
            }
        }
        private static byte[] ReadCompScript(MemoryStream stream, int size)
        {
            byte[] buffer = new byte[size];
            stream.Read(buffer, 0, size);
            return buffer;
        }
        public static string ReadScriptString(string name, uint length, MemoryStream stream)
        {
            string textString;
            switch (name)
            {
                case STRING:
                    textString = ReadWrite.ReadUntilNullByte(stream);
                    if (textString.Length + 1 != length)
                    {
                        throw new Exception("String length is not what script says it is.");
                    }
                    break;
                case WIDESTRING:
                    textString = ReadWrite.ReadWideString(stream);
                    if (textString.Length*2 + 2 != length)
                    {
                        throw new Exception("String length is not what script says it is.");
                    }
                    break;
                default:
                    throw new Exception("Unknown string type found.");
            }
            return textString;

        }
        public static string ReadScriptQBKey(MemoryStream stream)
        {
            return DebugReader.DebugCheck(SongHeaders, ScriptReader.ReadUInt32(stream));
        }
        private static List<object> ParseScript(byte[] script)
        {
            List<object> list = new List<object>();
            bool nextGlobal = false;
            bool nextArg = false;
            using (MemoryStream stream = new MemoryStream(script))
            {
                while (stream.Position < stream.Length)
                {
                    var scriptByte = Reader.ReadUInt8(stream);
                    uint length;
                    switch (scriptByte)
                    {
                        case 0x01:
                            list.Add(NEWLINE); // New line
                            break;
                        case 0x03:
                            list.Add(LEFTBRACE);
                            break;
                        case 0x04:
                            list.Add(RIGHTBRACE);
                            break;
                        case 0x05:
                            list.Add(LEFTBKT);
                            break;
                        case 0x06:
                            list.Add(RIGHTBKT);
                            break;
                        case 0x07:
                            list.Add(EQUALS);
                            break;
                        case 0x08:
                            list.Add(DOT);
                            break;
                        case 0x09:
                            list.Add(COMMA);
                            break;
                        case 0x0A:
                            list.Add(MINUS);
                            break;
                        case 0x0B:
                            list.Add(PLUS);
                            break;
                        case 0x0C:
                            list.Add(DIVIDE);
                            break;
                        case 0x0D:
                            list.Add(MULTIPLY);
                            break;
                        case 0x0E:
                            list.Add(LEFTPAR);
                            break;
                        case 0x0F:
                            list.Add(RIGHTPAR);
                            break;
                        case 0x12:
                            list.Add(LESSTHAN);
                            break;
                        case 0x13:
                            list.Add(LESSTHANEQUAL);
                            break;
                        case 0x14:
                            list.Add(GREATERTHAN);
                            break;
                        case 0x15:
                            list.Add(GREATERTHANEQUAL);
                            break;
                        case 0x16:
                            if (nextGlobal)
                            {
                                list.Add(new ScriptNode(POINTER, ReadScriptQBKey(stream)));
                                nextGlobal = false;
                            }
                            else
                            {
                                list.Add(new ScriptNode(QBKEY, ReadScriptQBKey(stream)));
                            }
                            break;
                        case 0x17:
                            list.Add(new ScriptNode(INTEGER, ScriptReader.ReadInt32(stream)));
                            break;
                        case 0x1A:
                            list.Add(new ScriptNode(FLOAT, ScriptReader.ReadFloat(stream)));
                            break;
                        case 0x1B:
                            length = ScriptReader.ReadUInt32(stream);
                            list.Add(new ScriptNode(STRING, ReadScriptString(STRING, length, stream)));
                            break;
                        case 0x1E:
                            list.Add(new ScriptTuple(VECTOR, stream));
                            break;
                        case 0x1F:
                            list.Add(new ScriptTuple(PAIR, stream));
                            break;
                        case 0x20:
                            list.Add(BEGIN); // Loop
                            break;
                        case 0x21:
                            list.Add(REPEAT);
                            break;
                        case 0x22:
                            list.Add(BREAK);
                            break;
                        case 0x24:
                            list.Add(ENDSCRIPT);
                            break;
                        case 0x27:
                            uint nextComp = ScriptReader.ReadUInt16(stream); // This is either another else if or else. It can also be an endif if there are no more comparisons
                            uint lastComp = ScriptReader.ReadUInt16(stream); // I think this is the last byte before the end of the else if statement
                            list.Add(ELSEIF);
                            break;
                        case 0x28:
                            list.Add(ENDIF);
                            break;
                        case 0x29:
                            list.Add(RETURN);
                            break;
                        case 0x2C:
                            list.Add(ALLARGS); // All Args
                            break;
                        case 0x2D:
                            list.Add(ARGUMENT); // surround next item in <> when parsing
                            break;
                        case 0x2E:
                            list.Add(new ScriptLongJump(ScriptReader.ReadUInt32(stream)));
                            break;
                        case 0x2F:
                            list.Add(new ScriptRandom(RANDOM, stream));
                            break;
                        case 0x30:
                            list.Add(RANDOMRANGE); // Random Range?
                            break;
                        case 0x32:
                            list.Add(ORCOMP);
                            break;
                        case 0x33:
                            list.Add(ANDCOMP);
                            break;
                        case 0x39:
                            list.Add(NOT);
                            break;
                        case 0x3A:
                            list.Add(AND);
                            break;
                        case 0x3B:
                            list.Add(OR);
                            break;
                        case 0x3C:
                            list.Add(SWITCH);
                            break;
                        case 0x3D:
                            list.Add(ENDSWITCH);
                            break;
                        case 0x3E:
                            list.Add(CASE);
                            break;
                        case 0x3F:
                            list.Add(DEFAULT);
                            break;
                        case 0x40:
                            list.Add(new ScriptRandom(RANDOMNOREPEAT, stream));
                            break;
                        case 0x42:
                            list.Add(COLON);
                            break;
                        case 0x47:
                            list.Add(new Conditional(FASTIF, ScriptReader.ReadUInt16(stream)));
                            break;
                        case 0x48:
                            list.Add(new Conditional(FASTELSE, ScriptReader.ReadUInt16(stream)));
                            break;
                        case 0x49:
                            stream.Position += 2;
                            break;
                        case 0x4A:
                            list.Add(new ScriptNode(STRUCT, new ScriptNodeStruct(stream)));
                            ReadWrite.MoveToModFour(stream);
                            break;
                        case 0x4B: // This byte makes the next QbKey a Pointer instead
                            nextGlobal = true;
                            break;
                        case 0x4C:
                            if (Reader.Endian() == "little")
                            {
                                list.Add(NOTEQUALS);
                            }
                            else
                            {
                                length = ScriptReader.ReadUInt32(stream);
                                list.Add(new ScriptNode(WIDESTRING, ReadScriptString(WIDESTRING, length, stream)));
                            }
                            
                            break;
                        case 0x4D:
                            if (Reader.Endian() == "little")
                            {
                                list.Add(NOTEQUALS);
                            }
                            else
                            {
                                list.Add(NOTEQUALS);
                            }
                            break;
                        case 0x4E:
                            list.Add(new ScriptNode(QSKEY, ReadScriptQBKey(stream)));
                            break;
                        case 0x4F:
                            list.Add(RANDOMFLOAT);
                            break;
                        case 0x50:
                            list.Add(RANDOMINTEGER);
                            break;
                        default:
                            throw new Exception("Not supported");
                    }
                }
            }
            return list;
        }
    }
}
