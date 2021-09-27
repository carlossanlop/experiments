using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace tarimpl
{
    public static class Program
    {
        public static void Main()
        {
            CreateArchiveAddEntries();
        }

        public static void TestZip()
        {
            // The dispose saves the file
            using FileStream fs = File.Create(@"C:\Users\calope\OneDrive - Microsoft\Desktop\Compression\test.zip");
            // The dispose flushes the zip into the stream
            using ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);
            // The Zip APIs are capable of creating an entry inside folders that had not yet been added as separate entries
            ZipArchiveEntry entry = zip.CreateEntry("folder/subfolder/file.txt", CompressionLevel.NoCompression);
            // The dispose flushes the written lines
            using var writer = new StreamWriter(entry.Open());
            writer.WriteLine("Hello world");
        }

        public static void ReadEntries()
        {
            TarOptions options = new() { Mode = TarMode.Read };
            using TarArchive archive = new TarArchive(@"C:\Users\calope\OneDrive - Microsoft\Desktop\Compression\brotli.tar", options);
            foreach (TarArchiveEntry entry in archive.Entries)
            {
                Console.WriteLine($"{entry.Length}    {entry.FullName}");
            }
        }

        public static void CreateArchiveAddEntries()
        {
            var files = new Dictionary<string, string>()
            {
                { "file1.txt", "AAA" },
                { "dir/file2.txt", "BBB" },
                { "dir/subdir/file3.txt", "CCC" }
            };

            string path = @"C:\Users\calope\OneDrive - Microsoft\Desktop\Compression\created.tar";
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