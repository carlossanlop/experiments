using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

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
        Stream _archiveStream;
        BinaryReader? _archiveReader;
        List<TarArchiveEntry> _entries;
        ReadOnlyCollection<TarArchiveEntry> _entriesCollection;
        bool _isDisposed;
        bool _areEntriesRead;

        public TarArchive(Stream stream, TarOptions? options)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _options = options ?? new TarOptions();

            if (!stream.CanSeek)
            {
                _archiveStream = new MemoryStream();
                stream.CopyTo(_archiveStream);
                _archiveStream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                _archiveStream = stream;
            }

            switch (_options.Mode)
            {
                case TarMode.Create:
                    if (!stream.CanWrite)
                    {
                        throw new ArgumentException("Create");
                    }
                    break;

                case TarMode.Read:
                    if (!stream.CanRead)
                    {
                        throw new ArgumentException("Read");
                    }
                    break;

                case TarMode.Update:
                    if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
                    {
                        throw new ArgumentException("Update");
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Mode");
            }

            if (_options.Mode != TarMode.Create)
            {
                _archiveReader = new BinaryReader(_archiveStream);
            }

            _entries = new List<TarArchiveEntry>();
            _entriesCollection = new ReadOnlyCollection<TarArchiveEntry>(_entries);
            _isDisposed = false;
            _areEntriesRead = false;
        }

        public ReadOnlyCollection<TarArchiveEntry> Entries
        {
            get
            {
                if (_options.Mode == TarMode.Create)
                {
                    throw new NotSupportedException("Entries Create");
                }

                ThrowIfDisposed();
                EnsureArchiveIsRead();

                return _entriesCollection;
            }
        }

        public TarOptions Options => _options;

        public TarArchiveEntry CreateEntry(string entryName)
        {
            return null;
        }

        public void Dispose()
        {

        }

        protected virtual void Dispose(bool disposing)
        {

        }

        public TarArchiveEntry? GetEntry(string entryName)
        {
            return null;
        }

        private void ThrowIfDisposed()
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
            _archiveStream.Seek(0, SeekOrigin.Begin);

            long numberOfEntries = 0;

            Debug.Assert(_archiveReader != null);

            TarFileHeader currentHeader;
            while (TarFileHeader.TryReadBlock(_archiveReader, out currentHeader))
            {
                AddEntry(new TarArchiveEntry(this, currentHeader));
                numberOfEntries++;
            }
        }

        private void AddEntry(TarArchiveEntry entry)
        {
            _entries.Add(entry);
        }
    }
}