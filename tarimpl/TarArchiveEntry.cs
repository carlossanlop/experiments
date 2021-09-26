using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
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
        private TarArchive? _archive;
        private TarHeader _header;
        private long _dataStart;
        internal MemoryMappedViewStream? _stream;

        internal TarArchiveEntry(TarArchive archive, TarHeader header, long dataStart)
        {
            _archive = archive;
            _header = header;
            _dataStart = dataStart;

            // 0 size means directory
            if (_header._size > 0)
            {
                _stream = _archive._mmf.CreateViewStream(dataStart, _header._size);
            }
        }

        public string FullName => _header._fullName;

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

        public void Delete()
        {
            if (_archive == null)
            {
                return;
            }
            _archive.ThrowIfDisposed();
            _archive.RemoveEntry(this);
            _archive = null!;
        }

        public Stream? Open()
        {
            ThrowIfInvalidArchive();
            return _stream;
        }

        private void ThrowIfInvalidArchive()
        {
            if (_archive == null)
            {
                throw new InvalidOperationException("entry is deleted");
            }
            _archive.ThrowIfDisposed();
        }

        public override string ToString() => FullName;
    }
}