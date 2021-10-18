using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace tarimpl
{
    /*
    Python:
        frombuf(buf,encoding,errors)
        fromtarfile(tarfile)
        tobuf(format,encoding,errors)
        name
        size
        mtime
        mode
        type
        linkname
        uid
        gid
        uname
        gname
        pax_headers
        isfile()
        isdir()
        issym()
        islnk()
        ischr()
        isblk()
        isfifo()
        isdev()
    */

    public class TarArchiveEntry
    {
        private readonly TarArchive _archive;
        private readonly TarHeader _header;
        internal MemoryStream? _stream;
        private long _dataStart;
        internal bool _isDeleted;
        private string? _previousLongLink;

        internal TarArchiveEntry(TarArchive archive, TarHeader header, long dataStart, string? previousLongLink)
        {
            _archive = archive;
            _header = header;
            _dataStart = dataStart;
            _isDeleted = false;
            _stream = null;
            _previousLongLink = previousLongLink;
        }

        public string FullName => !string.IsNullOrEmpty(_previousLongLink) ? _previousLongLink : _header._fullName;

        public string LinkName => _header._linkname;

        public int Mode => _header._mode;
        public int Uid => _header._uid;
        public int Gid => _header._gid;

        public string UName => _header._uname;
        public string GName => _header._gname;

        public int DevMajor => _header._devmajor;
        public int DevMinor => _header._devminor;

        // File size
        public long Length => _header._size;

        public DateTime LastWriteTime => _header._lastWriteTime;

        public int CheckSum => _header._checksum;

        public TarArchiveEntryType EntryType => (TarArchiveEntryType)_header._typeflag;

        public void Delete()
        {
        }

        public Stream Open()
        {
            if (_stream == null)
            {
                _stream = new MemoryStream();
                long originalPosition = _archive._stream.Position;
                _archive._stream.Seek(_dataStart, SeekOrigin.Begin);

                int bufferLength = 100;
                byte[] buffer = new byte[bufferLength];
                long totalBytesToRead = _header._size;
                long bytesReadSoFar = 0;
                do
                {
                    int bufferLimit = (int)Math.Min(totalBytesToRead, bufferLength);
                    int n = _archive._stream.Read(buffer, 0, bufferLimit);
                    _stream.Write(buffer, 0, bufferLimit);
                    bytesReadSoFar += n;
                    totalBytesToRead -= n;
                }
                while (totalBytesToRead > 0);
                
                _archive._stream.Position = originalPosition;
            }
            return _stream;
        }

        internal void Write()
        {
            using var writer = new StreamWriter(_archive._stream);
            RawHeader header = _header._rawHeader;

            writer.Write(header._name);
            writer.Write(header._mode);
            writer.Write('\0');
            writer.Write(header._uid);
            writer.Write('\0');
            writer.Write(header._gid);
            writer.Write(' ');
            writer.Write(header._size);
            writer.Write(' ');
            writer.Write(header._checksum);
            writer.Write("\0\0");
            writer.Write(header._typeflag);
            writer.Write(header._linkname);
            writer.Write('\0');
            writer.Write(header._magic);
            writer.Write(header._version);
            writer.Write(header._uname);
            writer.Write(header._gname);
            writer.Write(header._devmajor);
            writer.Write(header._devminor);
            writer.Write(header._prefix);
            writer.Write(header._pad);
            // Directories may have no data stored
            //if (_fileContents != null)
            //{
            //    _fileContents.CopyTo(_archive._stream);
            //    // File contents need to be aligned to block sizes of 512 bytes
            //    int padding = (int)(_fileContents.Length % 512);
            //    if (padding > 0)
            //    {
            //        writer.Write(new string('\0', padding));
            //    }
            //}
        }

        public override string ToString() => FullName;
    }
}