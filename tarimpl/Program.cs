using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace tarimpl
{
    public static class Program
    {
        public static void Main()
        {
            ReadEntries();
        }

        private static void ReadEntries()
        {
            TarOptions options = new() { Mode = TarMode.Read };
            using TarArchive archive = new TarArchive(@"C:\Users\calope\OneDrive - Microsoft\Desktop\Compression\brotli.tar", options);
            foreach (TarArchiveEntry entry in archive.Entries)
            {
                Console.WriteLine($"{entry.Length}    {entry.FullName}");
            }
        }

        private static void CreateArchiveAddEntries()
        {
            var files = new Dictionary<string, string>()
            {
                { "file1.txt", "AAA" },
                { "dir/file2.txt", "BBB" },
                { "dir/subdir/file3.txt", "CCC" }
            };

            string path = @"C:\Users\calope\OneDrive - Microsoft\Desktop\Compression\brotli.tar";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var options = new TarOptions() { Mode = TarMode.Create };
            using (var archive = new TarArchive(path, options))
            {
                foreach ((string fileName, string contents) in files)
                {
                    TarArchiveEntry entry = archive.CreateEntry(fileName);
                    using StreamWriter writer = new StreamWriter(entry.Open());
                    writer.Write(contents);
                }
            }
        }


        //private static void UpdateByDeletion(TarArchive archive)
        //{
        //    foreach (var entry in archive.Entries)
        //    {
        //        if (entry.FullName == "file1.txt") // First item
        //        {
        //            entry.Delete();
        //        }
        //    }
        //}
    }
}