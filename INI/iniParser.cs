using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_Toolkit_Core.Methods;
using IniParser;
using IniParser.Model;

namespace GH_Toolkit_Core.INI
{
    public class iniParser
    {
        public static IniData ReadIniFromPath(string path)
        {
            var parser = new IniParser.Parser.IniDataParser();
            var textData = ReadWrite.ReadFileContent(path);
            IniData data = parser.Parse(textData);
            return data;
        }


    }
}
