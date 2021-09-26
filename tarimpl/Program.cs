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
                Console.WriteLine(entry.FullName);
                byte[] buffer = new byte[20];
                entry.Stream!.Read(buffer);
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
            }
        }

        //private static TarArchive CreateArchiveAddEntries(Stream s)
        //{
        //    Dictionary<string, string> files = new()
        //    {
        //        { "file1.txt", "AAA" },
        //        { "dir/file2.txt", "BBB" },
        //        { "dir/subdir/file3.txt", "CCC" }
        //    };

        //    TarOptions options = new() { Mode = TarMode.Create };
        //    TarArchive archive = new TarArchive(s, options);

        //    foreach ((string fileName, string fileContents) in files)
        //    {
        //        TarArchiveEntry entry = archive.CreateEntry(fileName);
        //        using StreamWriter writer = new StreamWriter(entry.Open());
        //        writer.Write(fileContents);
        //    }

        //    return archive;
        //}


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