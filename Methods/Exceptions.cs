using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * This file is intended to be a collection of custom exceptions for use in GH_Toolkit_Core
 * 
 * 
 * Author: AddyMills
 */

namespace GH_Toolkit_Core.Methods
{
    public class Exceptions
    {
        // Custom exception class for float parsing errors
        public class FloatParseException : Exception
        {
            public FloatParseException(string message) : base(message) { }
        }

        // Custom exception class for MIDI compile errors
        public class MidiCompileException : Exception
        {
            public MidiCompileException(string message) : base(message) { }
        }

        // Custom exception calss for Q File parsing errors
        public class QFileParseException : Exception
        {
            public QFileParseException(string message) : base(message) { }
        }
        public class ClipNotFoundException : Exception
        {
            public ClipNotFoundException(string message) : base(message) { }
        }
    }
}
