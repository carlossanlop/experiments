using System;
using System.Collections.Generic;
using System.IO;

namespace tarimpl
{
    public static class Program
    {
        public static void Main()
        {
        }

        private static void ReadEntries()
        {
            using FileStream fs = File.Open("file.tar", FileMode.Open);
            TarOptions options = new() { Mode = TarMode.Read };
            using TarArchive archive = new TarArchive(fs, options);
            foreach (TarArchiveEntry entry in archive.Entries)
            {
                Console.WriteLine(entry.FullName);
            }
        }

        private static TarArchive CreateArchiveAddEntries(Stream s)
        {
            Dictionary<string, string> files = new()
            {
                { "file1.txt", "AAA" },
                { "dir/file2.txt", "BBB" },
                { "dir/subdir/file3.txt", "CCC" }
            };

            TarOptions options = new() { Mode = TarMode.Create };
            TarArchive archive = new TarArchive(s, options);

            foreach ((string fileName, string fileContents) in files)
            {
                TarArchiveEntry entry = archive.CreateEntry(fileName);
                using StreamWriter writer = new StreamWriter(entry.Open());
                writer.Write(fileContents);
            }

            return archive;
        }


        private static void UpdateByDeletion(TarArchive archive)
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.Name == "file1.txt") // First item
                {
                    entry.Delete();
                }
            }
        }
    }
}