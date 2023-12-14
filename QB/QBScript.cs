using GH_Toolkit_Core.Debug;
using GH_Toolkit_Core.Methods;
using System.Diagnostics;
using System.Text;
using static GH_Toolkit_Core.QB.QB;
using static GH_Toolkit_Core.QB.QBConstants;

namespace GH_Toolkit_Core.QB
{
    public class QBScript
    {
        private static void UpdateItem(ref string currItem, object newItem)
        {
            if (newItem is string stringEntry)
            {
                currItem = stringEntry;
            }
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
            public byte[] CRCData { get; set; }
            public List<object> ScriptParsed { get; set; }
            public QBScriptData()
            {
                ScriptParsed = new List<object>();
            }
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
                ScriptParsed = ParseScript(ScriptData);
            }
            public void AddScriptElem(string byteType)
            {
                ScriptParsed.Add(byteType);
            }
            public void StripArgData(ref string arg)
            {
                if (arg.StartsWith("<") && arg.EndsWith(">"))
                {
                    arg = arg.Substring(1, arg.Length - 2);
                    AddScriptElem(ARGUMENT);
                }
            }
            public void AddToScript(string type, object data)
            {
                if (data is string strData)
                {
                    switch (strData)
                    {
                        case EQUALS:
                        case NOTEQUALS:
                        case MINUS:
                        case PLUS:
                        case MULTIPLY:
                        case DIVIDE:
                        case GREATERTHAN:
                        case LESSTHAN:
                        case GREATERTHANEQUAL:
                        case LESSTHANEQUAL:
                        case ORCOMP:
                        case ANDCOMP:
                        case LEFTBRACE:
                        case RIGHTBRACE:
                        case LEFTBKT:
                        case RIGHTBKT:
                        case LEFTPAR:
                        case RIGHTPAR:
                        case COLON:
                        case COMMA:
                        case DOT:
                        case NOT:
                        case AND:
                        case OR:
                        case ALLARGS:
                        case ARGUMENT:
                        case BEGIN:
                        case REPEAT:
                        case BREAK:
                        case SWITCH:
                        case ENDSWITCH:
                        case CASE:
                        case DEFAULT:
                        case ENDSCRIPT:
                        case RANDOM:
                        case RANDOMNOREPEAT:
                        case RANDOMRANGE:
                        case RANDOMFLOAT:
                        case RANDOMINTEGER:
                        case IF:
                        case ELSE:
                        case FASTELSE:
                        case ELSEIF:
                        case ENDIF:
                        case RETURN:
                            AddScriptElem(strData);
                            return;
                        default:
                            break;
                    }
                    StripArgData(ref strData);
                    switch (type)
                    {
                        case MULTIFLOAT:
                            ScriptParsed.Add(new ScriptTuple(strData));
                            break;
                        default:
                            ScriptParsed.Add(new ScriptNode(type, strData));
                            break;
                    }
                }

            }
            public string GetIndent(int level)
            {
                return new string('\t', level);
            }
            private class RandomState
            {
                private uint RandomEntries;
                private uint MaxEntries;
                private uint CurrEntry;
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
                    CurrEntry = 0;
                }
                public void useEntry()
                {
                    RandomEntries--;
                    CurrEntry++;
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
                        currentLine.Append($"{item} ");
                        break;
                    default:
                        currentLine.Append($"{item} ");
                        break;
                }
            }

            public string ScriptToText(List<object> list, int level = 1)
            {
                StringBuilder builder = new StringBuilder();
                StringBuilder currentLine = new StringBuilder();
                isArgument = false;
                foreach (object item in list)
                {
                    stringIndent = GetIndent(level);
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
                        switch (conditional.Type)
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
                        currentLine.Append($"{random.Name} (");
                        foreach (RandomEntry randomEntry in random.RandomEntries)
                        {
                            currentLine.Append(randomEntry.WeightString());
                            currentLine.Append(ScriptToText(randomEntry.Actions, level + 1));
                        }
                        currentLine.Append(")");
                    }
                    else if (item is ScriptLongJump)
                    {
                        continue;
                    }
                    else
                    {
                        throw new NotImplementedException("Not implemented");
                    }

                }
                string lineCheck = currentLine.ToString();
                if (lineCheck.Length > 0)
                {
                    if (lineCheck.IndexOf(ENDSCRIPT) >= 0)
                    {
                        lineCheck = lineCheck.Trim();
                    }
                    builder.Append(lineCheck);
                }
                return builder.ToString();
            }
        }
        public class SwitchNode
        {
            public List<object> Condition { get; set; }
            public List<CaseNode> Cases { get; set; }
            public byte[] CrcBytes { get; set; }
            public byte[] ScriptBytes { get; set; }
            public SwitchNode(List<object> script,
                ref int scriptPos,
                MemoryStream noCrcStream,
                MemoryStream scriptStream,
                ReadWrite writer
                )
            {
                Condition = new List<object>();
                Cases = new List<CaseNode>();
                string currItem = "";
                while (currItem != NEWLINE)
                {
                    object scriptItem = script[scriptPos];
                    UpdateItem(ref currItem, scriptItem);
                    Condition.Add(scriptItem);
                    scriptPos += 1;
                }
                CaseNode? currCase = new CaseNode(CASE);
                while (currItem != ENDSWITCH)
                {
                    object scriptItem = script[scriptPos];
                    UpdateItem(ref currItem, scriptItem);
                    if (currItem == CASE || currItem == DEFAULT)
                    {
                        if (currCase.Actions.Count != 0)
                        {
                            Cases.Add(currCase);
                        }
                        currCase = new CaseNode(currItem);
                        currItem = "";
                    }
                    else if (currItem == SWITCH)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        currCase.Actions.Add(scriptItem);
                    }
                    scriptPos += 1;
                }

                Cases.Add(currCase);

                int loopStart = 0;

                for (int i = 0; i < Cases.Count; i++)
                {
                    CaseNode caseNode = Cases[i];
                    using (MemoryStream switchStreamForCrc = new MemoryStream())
                    using (MemoryStream switchStream = new MemoryStream())
                    {
                        loopStart = 0;
                        writer.ScriptLoop(caseNode.Actions, ref loopStart, switchStreamForCrc, switchStream);
                        caseNode.CrcBytes = switchStreamForCrc.ToArray();
                        caseNode.ScriptBytes = switchStream.ToArray();
                        caseNode.SetJumpIfFalse();
                    }
                    caseNode.CasesLeft = Cases.Count - (i + 1);
                }

                using (MemoryStream switchCrc = new MemoryStream())
                using (MemoryStream switchStream = new MemoryStream())
                {
                    writer.AddScriptToStream(SWITCH_BYTE, switchCrc, switchStream);
                    writer.ScriptLoop(Condition, ref loopStart, switchCrc, switchStream);
                    for (int i = 0; i < Cases.Count; i++)
                    {
                        loopStart = 0;
                        CaseNode caseNode = Cases[i];
                        writer.ScriptStringParse(caseNode.Type, script, ref loopStart, switchCrc, switchStream);
                        writer.AddScriptToStream(SHORTJUMP_BYTE, switchCrc, switchStream);
                        writer.AddShortToStream((short)caseNode.JumpIfFalse, switchCrc, switchStream);
                        writer.WriteNoFlipBytes(switchCrc, caseNode.CrcBytes);
                        writer.WriteNoFlipBytes(switchStream, caseNode.ScriptBytes);
                        if (caseNode.Type != DEFAULT)
                        {
                            caseNode.JumpIfTrue = MakeJumpIfTrue(i);
                            writer.AddScriptToStream(SHORTJUMP_BYTE, switchCrc, switchStream);
                            writer.AddShortToStream((short)caseNode.JumpIfTrue, switchCrc, switchStream);
                        }
                    }
                    CrcBytes = switchCrc.ToArray();
                    ScriptBytes = switchStream.ToArray();
                }
            }
            public int MakeJumpIfTrue(int i)
            {
                int trueJump = 0;
                for (int j = i + 1; j < Cases.Count; j++)
                {
                    trueJump += Cases[j].JumpIfFalse;
                    trueJump += 2; // Byte for case or default and byte for short jump
                }
                trueJump += 3; // More missing bytes at the end
                return trueJump;

            }
        }
        public class CaseNode
        {
            public List<object> Actions { get; set; }
            public string Type { get; set; }
            public int JumpIfFalse { get; set; } // The jump value at the start of a case. Used if the case is not true
            public int JumpIfTrue { get; set; } // The jump value at the end of the case. Only used if the case is true.
            public byte[] ScriptBytes { get; set; }
            public byte[] CrcBytes { get; set; }
            public int CasesLeft { get; set; }
            public CaseNode(string type)
            {
                Actions = new List<object>();
                Type = type;
            }
            public void SetJumpIfFalse()
            {
                if (Type == CASE)
                {
                    JumpIfFalse = ScriptBytes.Length + 5; // 2 bytes for the False short, plus 1 for the short jump byte plus 2 bytes for the True short
                }
                else if (Type == DEFAULT)
                {
                    JumpIfFalse = ScriptBytes.Length + 1; // This should place it one before the endswitch byte. Add 2 for the if false short, subtract one because endswitch is included
                }
                else
                {
                    throw new NotSupportedException("Strange Case Type found.");
                }
            }
        }

        [DebuggerDisplay("{Type,nq} - {Data,nq}")]
        public class ScriptNode
        {
            public string Type { get; set; }
            public object Data { get; set; }
            public object DataQb { get; set; }
            public ScriptNode(string nodeType, object data)
            {
                Type = nodeType;
                Data = data;
            }
            public ScriptNode(string nodeType, string data)
            {
                Type = nodeType;
                Data = data;
                DataQb = ParseData(data, Type);
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
            public byte[] Bytes { get; set; }
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
        public class ConditionalCollection
        {
            public List<Conditional> Conditionals { get; set; }
            public byte[] CrcBytes { get; set; }
            public byte[] ScriptBytes { get; set; }
            public ConditionalCollection(List<object> script,
                ref int scriptPos,
                MemoryStream noCrcStream,
                MemoryStream scriptStream,
                ReadWrite writer
                )
            {
                Conditionals = new List<Conditional>();
                Conditional currCondition = new Conditional(IF);
                string currItem = "";
                int elseIfs = 0;
                while (currItem != ENDIF)
                {
                    object scriptItem = script[scriptPos];
                    UpdateItem(ref currItem, scriptItem);
                    if (currItem == ELSEIF || currItem == ELSE)
                    {
                        if (currCondition.Actions.Count != 0)
                        {
                            currCondition.NextType = currItem;
                            Conditionals.Add(currCondition);
                        }
                        if (currItem == ELSEIF)
                        {
                            elseIfs += 1;
                        }
                        currCondition = new Conditional(currItem);
                        currItem = "";
                    }
                    else if (currItem == IF)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        currCondition.Actions.Add(scriptItem);
                    }
                    scriptPos += 1;
                }

                Conditionals.Add(currCondition);

                int loopStart = 0;

                for (int i = 0; i < Conditionals.Count; i++)
                {
                    Conditional conditional = Conditionals[i];
                    using (MemoryStream switchStreamForCrc = new MemoryStream())
                    using (MemoryStream switchStream = new MemoryStream())
                    {
                        loopStart = 0;
                        writer.ScriptLoop(conditional.Actions, ref loopStart, switchStreamForCrc, switchStream);
                        conditional.CrcBytes = switchStreamForCrc.ToArray();
                        conditional.ScriptBytes = switchStream.ToArray();
                        conditional.SetJump();
                    }
                }

                if (elseIfs > 0)
                {
                    for (int i = 0; i < Conditionals.Count; i++)
                    {
                        Conditional conditional = Conditionals[i];
                        if (conditional.Type == ELSEIF)
                        {
                            conditional.JumpEnd = MakeJumpEnd(i);
                        }
                    }
                }

                using (MemoryStream ifCrc = new MemoryStream())
                using (MemoryStream ifStream = new MemoryStream())
                {
                    for (int i = 0; i < Conditionals.Count; i++)
                    {
                        loopStart = 0;
                        Conditional conditional = Conditionals[i];
                        byte condByte = writer.GetScriptByte(conditional.Type);
                        writer.AddScriptToStream(condByte, ifCrc, ifStream);
                        writer.AddShortToStream((short)conditional.Jump, ifCrc, ifStream);

                        if (conditional.Type == ELSEIF)
                        {
                            writer.AddShortToStream((short)conditional.JumpEnd, ifCrc, ifStream);
                        }

                        writer.WriteNoFlipBytes(ifCrc, conditional.CrcBytes);
                        writer.WriteNoFlipBytes(ifStream, conditional.ScriptBytes);
                    }

                    CrcBytes = ifCrc.ToArray();
                    ScriptBytes = ifStream.ToArray();
                }
            }
            public uint MakeJumpEnd(int i)
            {
                uint jumpEnd = 0;
                for (int j = i; j < Conditionals.Count; j++)
                {
                    jumpEnd += Conditionals[j].Jump;
                    jumpEnd -= 2; // Since this takes place 2 bytes after the Jump byte, also for else jump being after
                    try
                    {
                        if (Conditionals[j + 1].Type == ELSEIF)
                        {
                            jumpEnd += 3;
                        }
                    }
                    catch { }
                }
                return jumpEnd;
            }
        }
        [DebuggerDisplay("{Type}")]
        public class Conditional
        {
            public string Type { get; set; }
            public List<object> Actions { get; set; }
            public string NextType { get; set; } = "end"; // Elseif, or else
            public uint Jump { get; set; }
            public uint JumpEnd { get; set; } // Only set for elseifs
            public byte[] CrcBytes { get; set; }
            public byte[] ScriptBytes { get; set; }
            public Conditional(string name)
            {
                Type = name;
                Actions = new List<object>();
            }
            public Conditional(string name, uint jump)
            {
                Type = name;
                Jump = jump;
            }
            public void SetJump()
            {
                uint jumpVal = (uint)(ScriptBytes.Length);
                jumpVal += 2; // To cover the 2 bytes in the short jump
                if (Type == ELSEIF)
                {
                    jumpVal += 2; // 2 bytes for the Jump to End value found after the Next Jump value
                }
                if (NextType == ELSE)
                {
                    jumpVal += 3; // To cover the Else byte and jump val
                }
                Jump = jumpVal;
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
            public ScriptTuple(string tupleData)
            {
                Type = ParseMultiFloatType(tupleData);
                Data = ParseMultiFloat(tupleData);
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
            public List<RandomEntry> RandomEntries { get; set; } = new List<RandomEntry>();
            public ScriptRandom(string random, MemoryStream stream)
            {
                Name = random;
                Entries = ScriptReader.ReadUInt32(stream);
                for (int i = 0; i < Entries; i++)
                {
                    RandomEntry entry = new RandomEntry(stream);
                    RandomEntries.Add(entry);
                }

                stream.Position += (Entries * 4); // Skip offsets

                uint entriesCountdown = Entries;
                uint randEnd = 0xffffffff;
                bool nextGlobal = false;
                bool nextArg = false;
                for (int i = 0; i < Entries; i++)
                {
                    object currItem = 0;
                    RandomEntry entry = RandomEntries[i];
                    while (stream.Position < randEnd)
                    {
                        AddToList(stream, entry.Actions, ref nextGlobal, ref nextArg);
                        currItem = entry.Actions.Last();
                        if (currItem is ScriptLongJump longJump)
                        {
                            randEnd = (uint)stream.Position + longJump.Jump;
                            entriesCountdown--;
                            break;
                        }
                    }
                }
            }
        }
        public class RandomEntry
        {
            public short Weight { get; set; }
            public List<object> Actions { get; set; } = new List<object>();
            public RandomEntry(MemoryStream stream)
            {
                Weight = ScriptReader.ReadInt16(stream);
            }
            public string WeightString()
            {
                string entryString;
                if (Weight > 1)
                {
                    entryString = $"@*{Weight} ";
                }
                else
                {
                    entryString = "@ ";
                }
                return entryString;
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
                    if (textString.Length * 2 + 2 != length)
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
            using (MemoryStream stream = new MemoryStream(script))
            {
                ParseList(stream, list);
            }
            return list;
        }
        private static void ParseList(MemoryStream stream, List<object> list)
        {
            bool nextGlobal = false;
            bool nextArg = false;
            while (stream.Position < stream.Length)
            {
                AddToList(stream, list, ref nextGlobal, ref nextArg);
            }
        }

        private static void AddToList(MemoryStream stream, List<object> list, ref bool nextGlobal, ref bool nextArg)
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
                case 0x37:
                    list.Add(new ScriptRandom(RANDOM2, stream));
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
                case 0x41:
                    list.Add(new ScriptRandom(RANDOMPERMUTE, stream));
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
                    // Short Jump
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
}
