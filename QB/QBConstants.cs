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
        public const string FLAGBYTE = "0x0";
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
        public const string ROOT = "Root";
        public const string MULTIFLOAT = "MultiFloat";

        public const string SCRIPTKEY = "script";

        // QB Script constants
        public const string NEWLINE = "newline";

        public const string IF = "if";
        public const string FASTIF = "fastif";
        public const string ELSE = "else";
        public const string FASTELSE = "fastelse";
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
        public const string ORCOMP = "||";
        public const string ANDCOMP = "&&";

        public const string LEFTBRACE = "{";
        public const string RIGHTBRACE = "}";
        public const string LEFTBKT = "[";
        public const string RIGHTBKT = "]";
        public const string LEFTPAR = "(";
        public const string RIGHTPAR = ")";

        public const string COLON = ":";
        public const string COMMA = ",";
        public const string DOT = ".";

        public const string RANDOM = "Random";
        public const string RANDOM2 = "Random2";
        public const string RANDOMNOREPEAT = "RandomNoRepeat";
        public const string RANDOMPERMUTE = "RandomPermute";

        public const string RANDOMRANGE = "RandomRange";
        public const string RANDOMFLOAT = "RandomFloat";
        public const string RANDOMINTEGER = "RandomInteger";

        public const string NOT = "NOT"; // NOT string, not a comparison
        public const string AND = "AND";
        public const string OR = "OR";
        public const string ALLARGS = "<...>";
        public const string ARGUMENT = "Argument";

        public const string BEGIN = "begin";
        public const string REPEAT = "repeat";
        public const string BREAK = "break";

        public const string SWITCH = "switch";
        public const string ENDSWITCH = "endswitch";
        public const string CASE = "case";
        public const string DEFAULT = "default";

        public const string ENDSCRIPT = "endscript";

        public const string LONGJUMP = "longjump";
        public const string SHORTJUMP = "shortjump";
        public const string NEXTGLOBAL = "nextglobal";

        // Byte Constants
        public const byte NEWLINE_BYTE = 0x01;
        public const byte LEFTBRACE_BYTE = 0x03;
        public const byte RIGHTBRACE_BYTE = 0x04;
        public const byte LEFTBKT_BYTE = 0x05;
        public const byte RIGHTBKT_BYTE = 0x06;
        public const byte EQUALS_BYTE = 0x07;
        public const byte DOT_BYTE = 0x08;
        public const byte COMMAE_BYTE = 0x09;
        public const byte MINUS_BYTE = 0x0A;
        public const byte PLUS_BYTE = 0x0B;
        public const byte DIVIDE_BYTE = 0x0C;
        public const byte MULTIPLY_BYTE = 0x0D;
        public const byte LEFTPAR_BYTE = 0x0E;
        public const byte RIGHTPAR_BYTE = 0x0F;
        public const byte LESSTHAN_BYTE = 0x12;
        public const byte LESSTHANEQUAL_BYTE = 0x13;
        public const byte GREATERTHAN_BYTE = 0x14;
        public const byte GREATERTHANEQUAL_BYTE = 0x15;
        public const byte QBKEY_BYTE = 0x16;
        public const byte INTEGER_BYTE = 0x17;
        public const byte FLOAT_BYTE = 0x1A;
        public const byte STRING_BYTE = 0x1B;
        public const byte VECTOR_BYTE = 0x1E;
        public const byte PAIR_BYTE = 0x1F;
        public const byte BEGIN_BYTE = 0x20;
        public const byte REPEAT_BYTE = 0x21;
        public const byte BREAK_BYTE = 0x22;
        public const byte ENDSCRIPT_BYTE = 0x24;
        public const byte ELSEIF_BYTE = 0x27;
        public const byte ENDIF_BYTE = 0x28;
        public const byte RETURN_BYTE = 0x29;
        public const byte ALLARGS_BYTE = 0x2C;
        public const byte ARGUMENT_BYTE = 0x2D;
        public const byte LONGJUMP_BYTE = 0x2E;
        public const byte RANDOM_BYTE = 0x2F;
        public const byte RANDOMRANGE_BYTE = 0x30;
        public const byte ORCOMP_BYTE = 0x32;
        public const byte ANDCOMP_BYTE = 0x33;
        public const byte RANDOM2_BYTE = 0x37;
        public const byte NOT_BYTE = 0x39;
        public const byte AND_BYTE = 0x3A;
        public const byte OR_BYTE = 0x3B;
        public const byte SWITCH_BYTE = 0x3C;
        public const byte ENDSWITCH_BYTE = 0x3D;
        public const byte CASE_BYTE = 0x3E;
        public const byte DEFAULT_BYTE = 0x3F;
        public const byte RANDOMNOREPEAT_BYTE = 0x40;
        public const byte RANDOMPERMUTE_BYTE = 0x41;
        public const byte COLON_BYTE = 0x42;
        public const byte FASTIF_BYTE = 0x47;
        public const byte FASTELSE_BYTE = 0x48;
        public const byte SHORTJUMP_BYTE = 0x49;
        public const byte STRUCT_BYTE = 0x4A;
        public const byte NEXTGLOBAL_BYTE = 0x4B;
        public const byte WIDESTRING_BYTE = 0x4C;
        public const byte NOTEQUALS_BYTE = 0x4D;
        public const byte QSKEY_BYTE = 0x4E;
        public const byte RANDOMFLOAT_BYTE = 0x4F;
        public const byte RANDOMINTEGER_BYTE = 0x50;

        public const byte FLAG_STRUCT_GH3 = 0x80;

        public static Dictionary<string, byte> scriptDict = new Dictionary<string, byte>()
        {
            { NEWLINE, NEWLINE_BYTE },
            { LEFTBRACE, LEFTBRACE_BYTE },
            { RIGHTBRACE, RIGHTBRACE_BYTE },
            { LEFTBKT, LEFTBKT_BYTE },
            { RIGHTBKT, RIGHTBKT_BYTE },
            { EQUALS, EQUALS_BYTE },
            { DOT, DOT_BYTE },
            { COMMA, COMMAE_BYTE },
            { MINUS, MINUS_BYTE },
            { PLUS, PLUS_BYTE },
            { DIVIDE, DIVIDE_BYTE },
            { MULTIPLY, MULTIPLY_BYTE },
            { LEFTPAR, LEFTPAR_BYTE },
            { RIGHTPAR, RIGHTPAR_BYTE },
            { LESSTHAN, LESSTHAN_BYTE },
            { LESSTHANEQUAL, LESSTHANEQUAL_BYTE },
            { GREATERTHAN, GREATERTHAN_BYTE },
            { GREATERTHANEQUAL, GREATERTHANEQUAL_BYTE },
            { QBKEY, QBKEY_BYTE },
            { POINTER, NEXTGLOBAL_BYTE},
            { INTEGER, INTEGER_BYTE },
            { FLOAT, FLOAT_BYTE },
            { STRING, STRING_BYTE },
            { VECTOR, VECTOR_BYTE },
            { PAIR, PAIR_BYTE },
            { BEGIN, BEGIN_BYTE },
            { REPEAT, REPEAT_BYTE },
            { BREAK, BREAK_BYTE },
            { ENDSCRIPT, ENDSCRIPT_BYTE },
            { ELSEIF, ELSEIF_BYTE },
            { ENDIF, ENDIF_BYTE },
            { RETURN, RETURN_BYTE },
            { ALLARGS, ALLARGS_BYTE },
            { ARGUMENT, ARGUMENT_BYTE },
            { LONGJUMP, LONGJUMP_BYTE },
            { RANDOM, RANDOM_BYTE },
            { RANDOMRANGE, RANDOMRANGE_BYTE },
            { RANDOM2, RANDOM2_BYTE },
            { RANDOMPERMUTE, RANDOMPERMUTE_BYTE },
            { ORCOMP, ORCOMP_BYTE },
            { ANDCOMP, ANDCOMP_BYTE },
            { NOT, NOT_BYTE },
            { AND, AND_BYTE },
            { OR, OR_BYTE },
            { SWITCH, SWITCH_BYTE },
            { ENDSWITCH, ENDSWITCH_BYTE },
            { CASE, CASE_BYTE },
            { DEFAULT, DEFAULT_BYTE },
            { RANDOMNOREPEAT, RANDOMNOREPEAT_BYTE },
            { COLON, COLON_BYTE },
            { FASTIF, FASTIF_BYTE },
            { FASTELSE, FASTELSE_BYTE },
            { SHORTJUMP, SHORTJUMP_BYTE },
            { STRUCT, STRUCT_BYTE },
            { NEXTGLOBAL, NEXTGLOBAL_BYTE },
            { WIDESTRING, WIDESTRING_BYTE },
            { NOTEQUALS, NOTEQUALS_BYTE },
            { QSKEY, QSKEY_BYTE },
            { RANDOMFLOAT, RANDOMFLOAT_BYTE },
            { RANDOMINTEGER, RANDOMINTEGER_BYTE }
        };
    }
}
