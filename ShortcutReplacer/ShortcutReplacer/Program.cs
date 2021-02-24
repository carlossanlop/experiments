using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShortcutReplacer
{
    class Program
    {
        static void Main()
        {

            Dictionary<string, string> shortcuts = GetShortcuts();
            IEnumerable<string> xmlPaths = GetXmlFiles();

            foreach ((string mdPath, string mdContents) in shortcuts)
            {
                Console.WriteLine($"Shortcut: {mdPath}");
                Console.WriteLine();
                foreach (string xmlPath in xmlPaths)
                {
                    Replace(xmlPath, mdPath, mdContents);
                }
                Console.WriteLine($"--------------- Finished {mdPath} --------------");
                Console.WriteLine();
            }
        }

        static void Replace(string xmlPath, string mdPath, string mdContents)
        {
            string xmlContents = File.ReadAllText(xmlPath);
            if (xmlContents.Contains(mdPath))
            {
                Console.WriteLine();
                Console.WriteLine($"Replacing {xmlPath}");
                string pattern = $@"\[\!INCLUDE\[[a-zA-Z0-9_\-]+\]\({mdPath}\)\]";
                string updated = Regex.Replace(xmlContents, pattern, mdContents);
                File.WriteAllText(xmlPath, updated);
            }
            else
            {
                Console.Write('.');
            }
        }

        static Dictionary<string, string> GetShortcuts()
        {
            var dict = new Dictionary<string, string>();

            var options = new EnumerationOptions()
            {
                RecurseSubdirectories = false
            };

            var enumeration = new FileSystemEnumerable<string>(
                directory: @"D:\dotnet-api-docs\includes",
                transform: (ref FileSystemEntry entry) => entry.ToFullPath(),
                options: options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                {
                    string fullPath = entry.ToFullPath();
                    string fileName = Path.GetFileNameWithoutExtension(fullPath);
                    return !entry.IsDirectory &&
                            Path.GetExtension(fullPath) == ".md" &&
                            fileName.EndsWith("-md");
                }
            };

            Console.WriteLine($"Collecting shortcuts ({enumeration.Count()})...");
            foreach (string file in enumeration)
            {
                string contents = File.ReadAllText(file).Trim();
                if (contents.Length <= 50)
                {
                    string path = file.Replace(@"D:\dotnet-api-docs\includes\", @"~/includes/");
                    dict.TryAdd<string, string>(path, contents);
                    Console.WriteLine($"Adding: {file}");
                }
                else
                {
                    Console.WriteLine($"Skipping {file}");
                }
            }
            Console.WriteLine();

            return dict;
        }

        static IEnumerable<string> GetXmlFiles()
        {
            var options = new EnumerationOptions()
            {
                RecurseSubdirectories = true
            };

            var enumeration = new FileSystemEnumerable<string>(
               directory: @"D:\dotnet-api-docs\xml",
               transform: (ref FileSystemEntry entry) => entry.ToFullPath(),
               options: options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => {
                    return !entry.IsDirectory &&
                            Path.GetExtension(entry.ToFullPath()) == ".xml" &&
                            (entry.Directory.StartsWith("System") || entry.Directory.StartsWith("Microsoft."));
                }
            };

            return enumeration;
        }
    }
}
