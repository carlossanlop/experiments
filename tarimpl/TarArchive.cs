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
        private List<TarArchiveEntry> _entries;
        private bool _isDisposed;
        private bool _readEntries;
        private MemoryMappedFile? _mmf;
        internal Stream _stream;

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
                    _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
                    _stream = _mmf.CreateViewStream();
                    break;

                case TarMode.Update:
                    _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
                    _stream = _mmf.CreateViewStream();
                    break;

                case TarMode.Create:
                    _mmf = null;
                    _stream = new MemoryStream();
                    break;

                default:
                    throw new InvalidOperationException("Mode");
            }

            _entries = new List<TarArchiveEntry>();
            _isDisposed = false;
            _readEntries = false;
        }

        public ReadOnlyCollection<TarArchiveEntry> Entries
        {
            get
            {
                ThrowIfDisposed();
                EnsureArchiveIsRead();
                return _entries.AsReadOnly();
            }
        }

        public TarOptions Options => _options;

        public TarArchiveEntry CreateEntry(string entryName) => throw null!;

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                try
                {
                    switch (_options.Mode)
                    {
                        case TarMode.Read:
                            break;
                        case TarMode.Create:
                        case TarMode.Update:
                            WriteFiles();
                            break;
                        default:
                            throw new InvalidOperationException("TarOptions.Mode");
                    }
                }
                finally
                {
                    CloseStreams();
                    _isDisposed = true;
                }

                GC.SuppressFinalize(this);
            }
        }

        //public TarArchiveEntry? GetEntry(string entryName)
        //{
        //    EnsureArchiveIsRead();
        //    return null;
        //}

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

        private void CloseStreams()
        {
            foreach (var entry in _entries)
            {
                entry._fileContents?.Dispose();
            }
            _stream.Dispose();
        }

        private void EnsureArchiveIsRead()
        {
            if (!_readEntries)
            {
                if (_options.Mode == TarMode.Read || _options.Mode == TarMode.Update)
                {
                    ReadArchiveEntries();
                }
                _readEntries = true;
            }
        }

        private void ReadArchiveEntries()
        {
            Debug.Assert(_options.Mode == TarMode.Read || _options.Mode == TarMode.Update);

            long startHeader = 0;
            long endHeader = startHeader + TarHeader.TarFileHeaderSize;
            using var reader = new BinaryReader(_stream);

            while (startHeader < _stream.Length && endHeader < _stream.Length)
            {
                if (!TarHeader.TryReadBlock(reader, out TarHeader header))
                {
                    break;
                }

                long dataStart = endHeader + 1;

                // Directories may not have any data inside
                Stream? contents = null;
                if (header._size > 0)
                {
                    if (_mmf != null)
                    {
                        Debug.Assert(_options.Mode == TarMode.Read || _options.Mode == TarMode.Update);
                        contents = _mmf.CreateViewStream(dataStart, header._size);
                    }
                    else
                    {
                        contents = new MemoryStream();
                    }
                }

                var entry = new TarArchiveEntry(this, header, contents);
                AddEntry(entry);
                // skippedBytes contains the entry file size + block alignment padding
                startHeader += TarHeader.TarFileHeaderSize + header._size + header._blockAlignmentPadding + 1;
                endHeader = startHeader + TarHeader.TarFileHeaderSize;
            }
        }

        private void WriteFiles()
        {
            // We shouldn't be here if the tar file was opened in read mode
            Debug.Assert(_options.Mode != TarMode.Read);

            EnsureArchiveIsRead();
            _stream.Seek(0, SeekOrigin.Begin);
            _stream.SetLength(0);

            foreach (TarArchiveEntry entry in _entries)
            {
                entry.Write();
            }
        }
    }
}