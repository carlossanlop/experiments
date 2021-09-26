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
        private TarOptions _options;
        internal MemoryMappedFile _mmf;
        private MemoryMappedViewStream _stream;
        private List<TarArchiveEntry> _entries;
        private ReadOnlyCollection<TarArchiveEntry> _entriesCollection;
        private bool _isDisposed;
        private bool _areEntriesRead;

        public TarArchive(string path, TarOptions? options)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _options = options ?? new TarOptions();

            switch (_options.Mode)
            {
                case TarMode.Read:
                case TarMode.Update:
                    _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
                    break;

                case TarMode.Create:
                    // TODO: Is the capacity going to preallocate that memory?
                    _mmf = MemoryMappedFile.CreateNew(path, long.MaxValue, MemoryMappedFileAccess.CopyOnWrite);
                    break;

                default:
                    throw new InvalidOperationException("Mode");
            }

            _stream = _mmf.CreateViewStream();
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

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var entry in _entries)
                {
                    entry._stream?.Dispose();
                }
                _stream.Dispose();
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
            long endHeader = startHeader + TarHeader.TarFileHeaderSize;
            using var reader = new BinaryReader(_stream);

            while (startHeader < _stream.Length && endHeader < _stream.Length)
            {
                if (!TarHeader.TryReadBlock(reader, out TarHeader header, out long skippedBytes))
                {
                    break;
                }

                long dataStart = endHeader + 1;
                var entry = new TarArchiveEntry(this, header, dataStart);
                AddEntry(entry);
                // skippedBytes contains the entry file size + nulls 
                startHeader += TarHeader.TarFileHeaderSize + skippedBytes + 1;
                endHeader = startHeader + TarHeader.TarFileHeaderSize;
            }
        }
    }
}