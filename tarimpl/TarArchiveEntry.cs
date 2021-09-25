using System;
using System.IO;
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
        private string _fullName;
        private DateTime _lastWriteTime;

        private string _name;
        private int _mode;
        private int _uid;
        private int _gid;
        private long _size;
        private long _mtime;
        private int _checksum; // "        private " is the default value when checksum is computed
        private byte _typeflag;
        private string _linkname;
        private string _magic;
        private string _version;
        private string _uname;
        private string _gname;
        private int _devmajor;
        private int _devminor;
        private string _prefix;

        internal TarArchiveEntry(TarArchive archive, TarFileHeader header)
        {
            _archive = archive;

            _name = Encoding.ASCII.GetString(header._name);
            _mode = GetInt32FromOctalString(header._mode);
            _uid = GetInt32FromOctalString(header._uid);
            _gid = GetInt32FromOctalString(header._gid);
            _size = GetInt32FromOctalString(header._size);
            _mtime = GetInt32FromOctalString(header._mtime);
            _checksum = GetInt32FromOctalString(header._checksum);
            _typeflag = header._typeflag;
            _linkname = Encoding.ASCII.GetString(header._linkname);
            _magic = Encoding.ASCII.GetString(header._magic);
            _version = Encoding.ASCII.GetString(header._version);
            _uname = Encoding.ASCII.GetString(header._uname);
            _gname = Encoding.ASCII.GetString(header._gname);

            if (_magic.Equals("ustar  \0") &&
                (string.IsNullOrEmpty(_uname) ||
                 string.IsNullOrEmpty(_gname) ||
                 !_version.Equals("00")))
            {
                throw new InvalidDataException("uname or gname empty, or version not 00, when magic is ustar");
            }

            _devmajor = Convert.ToInt32(Encoding.ASCII.GetString(header._devmajor));
            _devminor = Convert.ToInt32(Encoding.ASCII.GetString(header._devminor));
            _prefix = Encoding.ASCII.GetString(header._prefix);

            if (string.IsNullOrEmpty(_prefix))
            {
                _fullName = _name;
            }
            else
            {
                _fullName = Path.Join(_prefix, _name);
            }

            _lastWriteTime = new DateTime(_mtime);
        }

        public TarArchive Archive => _archive;

        public string FullName => _fullName;

        public string LinkName => _linkname;

        public int Mode => _mode;
        public int Uid => _uid;
        public int Gid => _gid;

        public string UName => _uname;
        public string GName => _gname;

        public int DevMajor => _devmajor;
        public int DevMinor => _devminor;

        // File size
        public long Length => _size;

        public DateTime LastWriteTime => _lastWriteTime;

        public int CheckSum => _checksum;

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

        public Stream Open()
        {
            switch (_archive.Options.Mode)
            {
                case TarMode.Read:
                    return OpenInReadMode(checkOpenable: true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Mode");
            }
        }

        private Stream OpenInReadMode(bool checkOpenable)
        {
            if (checkOpenable)
            {
                ThrowIfNotOpenable();
            }

            Stream stream = ;
        }

        private bool IsOpenable()
        {
            return true;
        }

        private void ThrowIfNotOpenable()
        {
            if (!IsOpenable())
            {
                throw new InvalidDataException("not openable");
            }
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

        private static int GetInt32FromOctalString(byte[] field) =>
            Convert.ToInt32(Encoding.ASCII.GetString(field), 8);
    }
}