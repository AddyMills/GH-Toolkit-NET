using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace GH_Toolkit_Core.INI
{
    public class iniParser
    {
        public static IniData ReadIniFromPath(string path)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(path);
            return data;
        }
    }
}
