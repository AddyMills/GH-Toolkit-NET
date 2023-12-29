using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.Methods
{
    public class FileTesting
    {
        public Action<string> TestFunc { get; set; }
        public FileTesting(Action<string> testFunc)
        {
            TestFunc = testFunc;
        }
        public void TestProgram(string filePath)
        {
            List<string> files = new List<string>();
            if (Directory.Exists(filePath))
            {
                string[] allFiles = Directory.GetFileSystemEntries(filePath, "*", SearchOption.AllDirectories);
                foreach (string file in allFiles)
                {
                    if (File.Exists(file)) { files.Add(file); }
                }
            }
            else if (File.Exists(filePath))
            {
                files.Add(filePath);
            }
            else
            {
                throw new Exception("Could not find valid file or folder to parse.");
            }
            foreach (string file in files)
            {
                TestFunc(file);
            }
        }
    }
}
