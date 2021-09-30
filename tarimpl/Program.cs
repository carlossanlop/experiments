using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace tarimpl
{
    public static class Program
    {
        private static string _oneDrive = Environment.GetEnvironmentVariable("OneDrive")!;
        public static void Main()
        {
            ReadEntries();
        }

        public static void TestZip()
        {
            // The dispose saves the file
            string path = Path.Join(_oneDrive, "Desktop", "Compression", "test.zip");
            using FileStream fs = File.Create(path);
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
            string _path = Path.Join(_oneDrive, "Desktop", "Compression", "brotli.tar");
            using FileStream fs = File.Open(_path, FileMode.Open);
            TarOptions options = new() { Mode = TarMode.Read };
            using var archive = new TarArchive(fs, options);
            while (archive.TryGetNextEntry(out TarArchiveEntry? entry))
            {
                Console.WriteLine($"{entry.Length, 10} {entry.EntryType, 10} {entry.FullName}");
            }
        }

        //public static void CreateArchiveAddEntries()
        //{
        //    var files = new Dictionary<string, string>()
        //    {
        //        { "file1.txt", "AAA" },
        //        { "dir/file2.txt", "BBB" },
        //        { "dir/subdir/file3.txt", "CCC" }
        //    };

        //    string path = Path.Join(_oneDrive, "Desktop","Compression","created.tar");
        //    if (File.Exists(path))
        //    {
        //        File.Delete(path);
        //    }

        //    var options = new TarOptions() { Mode = TarMode.Create };
        //    using (var archive = new TarArchive(path, options))
        //    {
        //        foreach ((string fileName, string contents) in files)
        //        {
        //            TarArchiveEntry entry = archive.CreateEntry(fileName);
        //            using StreamWriter writer = new StreamWriter(entry.Open());
        //            writer.Write(contents);
        //        }
        //    }
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