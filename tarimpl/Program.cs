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
    }
}