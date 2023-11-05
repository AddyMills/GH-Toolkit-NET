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

        public const byte FLAG_STRUCT_GH3 = 0x80;
    }
}
