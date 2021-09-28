using System;
using System.Diagnostics.CodeAnalysis;
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
        internal Stream _stream;
        private TarOptions _options;
        private TarArchiveEntry? _currentEntry;
        private long _currentHeaderBegin;
        private long _currentHeaderEnd;
        private BinaryReader _reader;
        private bool _isDisposed;

        public TarOptions Options => _options;

        public TarArchive(Stream stream, TarOptions? options)
        {
            _stream = stream;
            _options = options ?? new TarOptions();
            _currentHeaderBegin = 0;
            _currentHeaderEnd = TarHeader.TarFileHeaderSize;
            _reader = new BinaryReader(_stream);
        }

        public bool TryGetNextEntry([NotNullWhen(returnValue: true)] out TarArchiveEntry? entry)
        {
            if (_currentHeaderBegin < _stream.Length && _currentHeaderEnd < _stream.Length)
            {
                if (!TarHeader.TryReadBlock(_reader, null, out TarHeader header))
                {
                    _currentHeaderBegin = _currentHeaderEnd = _stream.Length;
                    _currentEntry = null;
                }
                else
                {
                    long dataStart = _currentHeaderEnd + 1;
                    _currentEntry = new TarArchiveEntry(this, header, dataStart);
                    // skippedBytes contains the entry file size + block alignment padding
                    _currentHeaderBegin += TarHeader.TarFileHeaderSize + header._size + header._blockAlignmentPadding + 1;
                    _currentHeaderEnd = _currentHeaderBegin + TarHeader.TarFileHeaderSize;
                }
            }
            else
            {
                _currentEntry = null;
            }

            entry = _currentEntry;
            return entry != null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _reader.Dispose();
                    _stream.Dispose();
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