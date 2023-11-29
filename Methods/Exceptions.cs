using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Methods
{
    public class Exceptions
    {
        // Custom exception class for float parsing errors
        public class FloatParseException : Exception
        {
            public FloatParseException(string message) : base(message) { }
        }
    }
}
