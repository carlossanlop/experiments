using System.IO;

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
        internal TarArchiveEntry()
        {
        }

        internal TarArchiveEntry(TarArchive archive, TarFileHeader header)
        {

        }

        public TarArchive Archive
        {
            get
            {

            }
        }

        public string FullName
        {
            get
            {
                return null;
            }
        }

        public long Length
        {
            get
            {
                return 0;
            }
        }

        public string Name
        {
            get
            {
                return null;
            }
        }

        public void Delete()
        {
        }
        public Stream Open()
        {
            return null;
        }

        public override string ToString()
        {
            return null;
        }
    }
}