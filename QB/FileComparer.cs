using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_Toolkit_Core.QB
{
    public class FileComparer
    {
        public static void CompareFolders(string folder1, string folder2)
        {
            DirectoryInfo dir1 = new DirectoryInfo(folder1);
            DirectoryInfo dir2 = new DirectoryInfo(folder2);

            CompareDirectories(dir1, dir2);
        }

        private static void CompareDirectories(DirectoryInfo dir1, DirectoryInfo dir2)
        {
            FileInfo[] files = dir1.GetFiles("*", SearchOption.AllDirectories);

            foreach (FileInfo file1 in files)
            {
                string relativePath = file1.FullName.Substring(dir1.FullName.Length + 1);
                FileInfo file2 = new FileInfo(Path.Combine(dir2.FullName, relativePath));

                if (file2.Exists && !FileBytesEqual(file1, file2))
                {
                    Console.WriteLine(relativePath);
                }
            }
        }

        private static bool FileBytesEqual(FileInfo file1, FileInfo file2)
        {
            byte[] file1Bytes = File.ReadAllBytes(file1.FullName);
            byte[] file2Bytes = File.ReadAllBytes(file2.FullName);

            return file1Bytes.SequenceEqual(file2Bytes);
        }
    }
}
