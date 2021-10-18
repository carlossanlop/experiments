using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace tarimpl.tests
{
    public class Tests
    {
        [Theory]
        [InlineData("brotli.tar")]
        [InlineData("brotli.tar.gz")]
        [InlineData("onefile_insidesubfolder.tar")]
        public void ReadEntries(string fileName)
        {
            string _path = Path.Join("..", "..", "..", fileName);
            using FileStream fs = File.Open(_path, FileMode.Open);
            TarOptions options = new() { Mode = TarMode.Read };
            using var archive = new TarArchive(fs, options);
            while (archive.TryGetNextEntry(out TarArchiveEntry? entry))
            {
                Assert.NotNull(entry);
            }
        }

        [Fact]
        public void CreateArchiveAddEntries()
        {
            var files = new Dictionary<string, string>()
            {
                { "file1.txt", "AAA" },
                { "dir/file2.txt", "BBB" },
                { "dir/subdir/file3.txt", "CCC" }
            };

            var fileStreamOptions = new FileStreamOptions()
            {
                Access = FileAccess.Write,
                Mode = FileMode.CreateNew
            };

            using var fs = new FileStream("created.tar", fileStreamOptions);

            var tarOptions = new TarOptions() { Mode = TarMode.Create };
            using var archive = new TarArchive(fs, tarOptions);
            
            foreach ((string fileName, string contents) in files)
            {
                TarArchiveEntry entry = archive.CreateEntry(fileName);
                using StreamWriter writer = new StreamWriter(entry.Open());
                writer.Write(contents);
            }
        }
    }
}