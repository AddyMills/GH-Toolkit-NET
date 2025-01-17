using System.Reflection;
using YamlDotNet.Serialization;
using static GH_Toolkit_Core.Methods.GlobalVariables;

namespace GH_Toolkit_Core.PS360
{
    public class YAML
    {
        private static string ExeDirectory = ExeRootFolder;

        private static int gh3Title = 0x415607F7;
        private static int ghaTitle = 0x41560819;
        private static int ghwtTitle = 0x4156081A;
        private static int gh5Title = 0x41560840;
        private static int ghworTitle = 0x41560883;

        private static Dictionary<string, int> gameTitles = new Dictionary<string, int>
        {
            { "GH3", gh3Title },
            { "GHA", ghaTitle },
            { "GHWT", ghwtTitle },
            { "GH5", gh5Title },
            { "GHWoR", ghworTitle }
        };
        private static Dictionary<string, string> gameNames = new Dictionary<string, string>
        {
            { "GH3", "Guitar Hero 3" },
            { "GHA", "Guitar Hero Aerosmith" },
            { "GHWT", "Guitar Hero World Tour" },
            { "GH5", "Guitar Hero 5" },
            { "GHWoR", "Guitar Hero : Warriors of Rock" }
        };
        public static List<string> MakePackageDescription(string description = "Compiled by Addy's .NET Toolkit")
        {
            string[] list = [description, "", "", "", "", "", "", "", ""];
            return list.ToList();
        }
        public static List<string> MakePackageName(string packageName = "")
        {
            string[] list = [packageName, "", "", "", "", "", "", "", ""];
            return list.ToList();
        }
        public static string CreateOnyxYaml(string game = "GH3", string packageName = "")
        {
            string yamlLocation = Path.Combine(ExeDirectory, "Resources", "Onyx", "repack-stfs.yaml");
            if (!File.Exists(yamlLocation))
            {
                Console.WriteLine("YAML file not found.");
                return "Fail";
            }
            string yamlContent = File.ReadAllText(yamlLocation);
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            yamlObject["package-description"] = MakePackageDescription();
            yamlObject["package-name"] = MakePackageName(packageName);
            yamlObject["title-id"] = gameTitles[game];
            yamlObject["title-name"] = gameNames[game];
            var serializer = new SerializerBuilder().Build();
            string yamlOutput = serializer.Serialize(yamlObject).Replace("\"\"","''");
            return yamlOutput;
        }
    }
}
