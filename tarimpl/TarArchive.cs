using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace tarimpl
{
    /*
    Python:
        open()
        is_tarfile(name)
        getmember(name)
        getmembers()
        getnames()
        next()
        extractall()
        extract(member)
        extractfile(member)
        add(name, arcname[altname for file in archive], recursive[set to false to avoid adding a whole dir])
        addfile(tarinfo,fileobj)
        gettarinfo(name,arcnmae,fileobj)
        close()
    */
    public class TarArchive : IDisposable
    {
        TarOptions _options;
        MemoryMappedFile _mmf;
        MemoryMappedFileAccess _mmfAccess;
        List<TarArchiveEntry> _entries;
        ReadOnlyCollection<TarArchiveEntry> _entriesCollection;
        bool _isDisposed;
        bool _areEntriesRead;

        public TarArchive(string path, TarOptions? options)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _options = options ?? new TarOptions();

            FileMode fileMode;
            switch (_options.Mode)
            {
                case TarMode.Read:
                    fileMode = FileMode.Open;
                    _mmfAccess = MemoryMappedFileAccess.Read;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Mode");
            }

            _mmf = MemoryMappedFile.CreateFromFile(path, fileMode, null, 0, _mmfAccess);
            _entries = new List<TarArchiveEntry>();
            _entriesCollection = new ReadOnlyCollection<TarArchiveEntry>(_entries);
            _isDisposed = false;
            _areEntriesRead = false;
        }

        public ReadOnlyCollection<TarArchiveEntry> Entries
        {
            get
            {
                ThrowIfDisposed();
                EnsureArchiveIsRead();

                return _entriesCollection;
            }
        }

        public TarOptions Options => _options;

        public TarArchiveEntry CreateEntry(string entryName) => throw null!;

        internal MemoryMappedFile MemoryFile => _mmf;

        internal MemoryMappedFileAccess MemoryFileAccess => _mmfAccess;

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var entry in _entries)
                {
                    entry.Stream?.Dispose();
                }
                _mmf.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public TarArchiveEntry? GetEntry(string entryName)
        {
            EnsureArchiveIsRead();
            return null;
        }

        private void AddEntry(TarArchiveEntry entry) => _entries.Add(entry);

        internal void RemoveEntry(TarArchiveEntry entry)
        {
        }

        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private void EnsureArchiveIsRead()
        {
            if (!_areEntriesRead)
            {
                ReadArchive();
                _areEntriesRead = true;
            }
        }

        private void ReadArchive()
        {
            long startHeader = 0;
            long endHeader = startHeader + TarFileHeader.TarFileHeaderSize;
            using var stream = _mmf.CreateViewStream(startHeader, endHeader, _mmfAccess);
            var reader = new BinaryReader(stream);
            while (TarFileHeader.TryReadBlock(ref reader, out TarFileHeader currentHeader))
            {
                var entry = new TarArchiveEntry(this, currentHeader, endHeader + 1);
                AddEntry(entry);
                startHeader += TarFileHeader.TarFileHeaderSize + entry.Length + 1; // Move past content of entry file
            }
            reader.Dispose();
        }
    }
}