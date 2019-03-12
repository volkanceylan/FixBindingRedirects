using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace FixBindingRedirects
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(directory, "web.config");
            if (!File.Exists(configFile))
            {
                Console.Error.WriteLine("No web.config file in current directory!");
                Environment.Exit(-1);
            }

            var binDirectory = Path.Combine(directory, @"bin\");
            if (!Directory.Exists(binDirectory))
            {
                Console.Error.WriteLine("No BIN subfolder in current directory!");
                Environment.Exit(-1);
            }

            var config = XDocument.Load(configFile, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var oldConfigLines = File.ReadAllLines(configFile);
            var ns = (XNamespace)"urn:schemas-microsoft-com:asm.v1";
            bool anyChange = false;
            foreach (var da in config.Descendants(ns + "dependentAssembly"))
            {
                var ai = da.Element(ns + "assemblyIdentity");
                if (ai == null)
                    continue;

                var name = ai.Attribute("name");
                if (name == null || string.IsNullOrWhiteSpace(name.Value))
                    continue;

                var br = da.Element(ns + "bindingRedirect");
                if (br == null)
                    continue;

                var oldVersion = br.Attribute("oldVersion");
                if (oldVersion == null || string.IsNullOrWhiteSpace(oldVersion.Value) ||
                    oldVersion.Value.IndexOf("-") < 0)
                    continue;

                var oldParts = oldVersion.Value.Trim().Replace(" ", "").Split('-');
                if (oldParts.Length != 2)
                    continue;

                Version oldStart;
                Version oldEnd;
                if (!Version.TryParse(oldParts[0], out oldStart) ||
                    !Version.TryParse(oldParts[1], out oldEnd))
                    continue;

                var newVersion = br.Attribute("newVersion");
                if (newVersion == null || string.IsNullOrWhiteSpace(newVersion.Value))
                    continue;

                var asmName = name.Value.Trim();
                var asmPath = Path.Combine(binDirectory, asmName + ".dll");
                if (!File.Exists(asmPath))
                    continue;

                var asmVer = AssemblyName.GetAssemblyName(asmPath).Version;
                if (asmVer.ToString() != newVersion.Value.Trim())
                {
                    if (oldEnd < asmVer)
                        oldEnd = asmVer;

                    newVersion.Value = asmVer.ToString();
                    oldVersion.Value = oldStart.ToString() + "-" + oldEnd.ToString();
                    anyChange = true;
                }
            }

            if (anyChange)
            {
                config.Save(configFile, SaveOptions.DisableFormatting);
                var newConfigLines = File.ReadAllLines(configFile);
                if (newConfigLines.Length == oldConfigLines.Length)
                {
                    bool whiteSpaceChange = false;
                    for (var i = 0; i < newConfigLines.Length; i++)
                    {
                        if (oldConfigLines[i] != newConfigLines[i] &&
                            newConfigLines[i].Trim().EndsWith("/>") &&
                            newConfigLines[i].Trim().IndexOf("/>") ==
                            newConfigLines[i].Trim().LastIndexOf("/>") &&
                            (newConfigLines[i].Trim().Replace("/>", " />") ==
                             oldConfigLines[i].Trim() ||
                             newConfigLines[i].Trim().Replace(" />", "/>") ==
                             oldConfigLines[i].Trim()))
                        {
                            whiteSpaceChange = true;
                            newConfigLines[i] = oldConfigLines[i];
                        }
                    }

                    if (whiteSpaceChange)
                        File.WriteAllLines(configFile, newConfigLines);
                }
            }
        }
    }
}