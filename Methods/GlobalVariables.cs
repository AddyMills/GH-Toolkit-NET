using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Methods
{
    public class GlobalVariables
    {
        public static string ExeRootFolder { get; private set; }
        
        static GlobalVariables()
        {
            ExeRootFolder = GetRootFolder();
        }

        static string GetRootFolder()
        {
            return AppContext.BaseDirectory;
        }
    }
}
