using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.QB
{
    public class QBConstants
    {
        public const string SECTION = "Section";

        public const string FLAG = "Flag";
        public const string INTEGER = "Integer";
        public const string FLOAT = "Float";
        public const string STRING = "String";
        public const string WIDESTRING = "WideString";
        public const string PAIR = "Pair";
        public const string VECTOR = "Vector";
        public const string SCRIPT = "Script";
        public const string STRUCT = "Struct";
        public const string ARRAY = "Array";
        public const string QBKEY = "QbKey";
        public const string POINTER = "Pointer";
        public const string QSKEY = "QsKey";
        public const string EMPTY = "Flag"; // Used for arrays and structs

        // QB Script constants
        public const string NEWLINE = "newline";

        public const string IF = "if";
        public const string ELSE = "else";
        public const string FASTIF = "Fast If";
        public const string FASTELSE = "Fast Else";
        public const string ELSEIF = "elseif";
        public const string ENDIF = "endif";
        public const string RETURN = "return";

        public const string EQUALS = "=";
        public const string NOTEQUALS = "!=";
        public const string MINUS = "-";
        public const string PLUS = "+";
        public const string MULTIPLY = "*";
        public const string DIVIDE = "/";
        public const string GREATERTHAN = ">";
        public const string LESSTHAN = "<";
        public const string GREATERTHANEQUAL = ">=";
        public const string LESSTHANEQUAL = "<=";

        public const string LEFTBRACE = "{";
        public const string RIGHTBRACE = "}";
        public const string LEFTBKT = "[";
        public const string RIGHTBKT = "]";

        public const string NOT = "NOT"; // NOT string, not a comparison
        public const string AND = "AND";
        public const string OR = "OR";
        public const string ALLARGS = "<...>";
        public const string ARGUMENT = "Argument";

        public const string BEGIN = "begin";
        public const string REPEAT = "repeat";
        public const string BREAK = "break";

        public const string ENDSCRIPT = "endscript";


        public const byte FLAG_STRUCT_GH3 = 0x80;
    }
}
