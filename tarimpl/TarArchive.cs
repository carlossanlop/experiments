using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

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
        internal Stream _stream;
        private TarOptions _options;
        private TarArchiveEntry? _currentEntry;
        private string? _previousLongLink;
        private long _currentHeaderBegin;
        private long _currentHeaderEnd;
        private BinaryReader _reader;
        private bool _isDisposed;

        public TarOptions Options => _options;

        public TarArchive(Stream stream, TarOptions? options)
        {
            _stream = stream;
            _previousLongLink = null;
            _options = options ?? new TarOptions();
            _currentHeaderBegin = 0;
            _currentHeaderEnd = TarHeader.TarFileHeaderSize;
            _reader = new BinaryReader(_stream);
        }

        public bool TryGetNextEntry([NotNullWhen(returnValue: true)] out TarArchiveEntry? entry)
        {
            if (_currentEntry != null)
            {
                _currentEntry._stream?.Dispose();
                _currentEntry = null;
            }
            if (_currentHeaderBegin < _stream.Length && _currentHeaderEnd < _stream.Length)
            {
                if (!TarHeader.TryReadBlock(_reader, null, out TarHeader header))
                {
                    _currentHeaderBegin = _currentHeaderEnd = _stream.Length;
                    _currentEntry?._stream?.Dispose();
                    _currentEntry = null;
                }
                else
                {
                    long dataStart = _currentHeaderEnd + 1;
                    _currentEntry = new TarArchiveEntry(this, header, dataStart, _previousLongLink);
                    // skippedBytes contains the entry file size + block alignment padding
                    _currentHeaderBegin += TarHeader.TarFileHeaderSize + header._size + header._blockAlignmentPadding + 1;
                    _currentHeaderEnd = _currentHeaderBegin + TarHeader.TarFileHeaderSize;

                    if (_currentEntry.EntryType == TarArchiveEntryType.LongLink)
                    {
                        using var reader = new BinaryReader(_currentEntry.Open());
                        _previousLongLink = Encoding.ASCII.GetString(reader.ReadBytes((int)header._size));
                    }
                    else
                    {
                        _previousLongLink = null;
                    }
                }
            }
            else
            {
                _currentEntry?._stream?.Dispose();
                _currentEntry = null;
            }

            entry = _currentEntry;
            return entry != null;
        }

        public TarArchiveEntry CreateEntry(ReadOnlySpan<char> path)
        {
            return null!;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _reader.Dispose();
                    _stream.Dispose();
                    _currentEntry?._stream?.Dispose();
                    _currentEntry = null;
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}