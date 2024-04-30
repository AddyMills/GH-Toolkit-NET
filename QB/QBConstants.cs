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

        public const string STRUCTFLAG = "StructFlag";
        public const string FLAGBYTE = "0x00000000";
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
        public const string EMPTY = "StructFlag"; // Used for arrays and structs
        public const string EMPTYARRAY = "Empty"; 
        public const string ROOT = "Root";
        public const string MULTIFLOAT = "MultiFloat";

        public const string WORINTEGER = "WoRInteger";
        public const string WORFLOAT = "WoRFloat";
        public const string WORQBKEY = "WoRQbKey";
        public const string WORARRAY = "WoRArray";

        public const string SCRIPTKEY = "script";

        public const string CONSOLE_PS2 = "PS2";
        public const string CONSOLE_XBOX = "360";
        public const string CONSOLE_PC = "PC";

        public const string HEXSTART = "0x";

        public const string GAME_GH3 = "GH3";
        public const string GAME_GHA = "GHA";
        public const string GAME_GHWT = "GHWT";
        public const string GAME_GHWOR = "GHWoR";

        public const string SKELETON_GH3_SINGER = "gh3_singer";
        public const string SKELETON_GH3_SINGER_PS2 = "gh3_singer_ps2";
        public const string SKELETON_GH3_GUITARIST = "gh3_guitarist";
        public const string SKELETON_GHA_SINGER = "gha_singer";
        public const string SKELETON_DMC_SINGER = "dmc_singer";
        public const string SKELETON_WT_ROCKER = "wt_rocker";
        public const string SKELETON_STEVE = "steve";
        public const string SKELETON_CAMERA = "camera";
        public const string SKELETON_OTHER = "other";


        public const string DOTNGC = ".ngc";
        public const string DOTPS2 = ".ps2";
        public const string DOTPS3 = ".ps3";
        public const string DOTXEN = ".xen";

        public const string DOT_Q = ".q";
        public const string DOT_QB = ".qb";
        public const string DOT_MQB = ".mqb";
        public const string DOT_SQB = ".sqb";
        public const string DOT_QS = ".qs";
        public const string DOT_MID_QB = ".mid.qb";
        public const string DOT_MID_QS = ".mid.qs";
        public const string DOT_PAB = ".pab";
        public const string DOT_PAB_XEN = ".pab.xen";
        public const string DOT_PAK = ".pak";
        public const string DOT_PAK_XEN = ".pak.xen";
        public const string DOT_SKA = ".ska";


        public const string _GFX = "_gfx";
        public const string _SFX = "_sfx";

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
        public const string AT = "@";

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
        public const string ARGUMENT = "<>";

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

        // Unknown Script Things

        public const string UNKNOWN52 = "0x52";
        public const string UNKNOWN54 = "0x54";
        public const string UNKNOWN55 = "0x55";
        public const string UNKNOWN59 = "0x59";
        public const string UNKNOWN5A = "0x5A";

        // Regex Strings

        public const string ALLARGS_REGEX = @"^<\.{1,2}$";

        // Script Constants
        public const string TIME = "time";
        public const string DELAY = "delay";
        public const string INITIAL_DELAY = "initial_delay";
        public const string Z_PRIORITY = "z_priority";
        public const string ALPHA = "alpha";
        public const string NAME = "name";
        public const string NODE = "node";
        public const string ANIM = "anim";
        public const string TYPE = "type";
        public const string REPEAT_COUNT = "repeat_count";

        // Default values for fadeoutandin
        public const string DefaultTime = "2.0";
        public const string DefaultDelay = "0.0";
        public const string DefaultZPriority = "0";
        public const string DefaultAlpha = "1.0";
        public const string DefaultIDelay = "0.0";
            
        // Compiling Constants
        public const string SONG_PERFORMANCE = "song_performance";
        public const string FEMALE_ANIM_STRUCT = "car_female_anim_struct";
        public const string MALE_ANIM_STRUCT = "car_male_anim_struct";

        public const string COMPILING = "Compiling...";

        public static string downloadRef = "scripts\\guitar\\guitar_download.qb";
        public static string gh3DownloadSongs = "gh3_download_songs";
        public static string songlistRef = "scripts\\guitar\\songlist.qb";
        public static string downloadSonglist = "download_songlist";
        public static string gh3Songlist = "gh3_songlist";
        public static string downloadProps = "download_songlist_props";
        public static string permanentProps = "permanent_songlist_props";

        // Byte Constants
        public const byte NEWLINE_BYTE = 0x01;
        public const byte LEFTBRACE_BYTE = 0x03;
        public const byte RIGHTBRACE_BYTE = 0x04;
        public const byte LEFTBKT_BYTE = 0x05;
        public const byte RIGHTBKT_BYTE = 0x06;
        public const byte EQUALS_BYTE = 0x07;
        public const byte DOT_BYTE = 0x08;
        public const byte COMMA_BYTE = 0x09;
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
            { COMMA, COMMA_BYTE },
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
