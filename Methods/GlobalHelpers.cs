using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Methods
{
    public class GlobalHelpers
    {
        public static string GetConsoleExtension(string console)
        {
            string extension = "";

            switch (console)
            {
                case "PS2":
                    extension = ".ps2";
                    break;
                case "PS3":
                    extension = ".PS3";
                    break;
                case "WII":
                    extension = ".ngc";
                    break;
                default:
                    extension = ".xen";
                    break;
            }
            return extension;
        }
    }
}
