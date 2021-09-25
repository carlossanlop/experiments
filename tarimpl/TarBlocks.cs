// Wiki: https://en.wikipedia.org/wiki/Tar_(computing)
// Spec: https://www.fileformat.info/format/tar/corion.htm
// Excellent explanation: https://github.com/Keruspe/tar-parser.rs/blob/master/tar.specs

using System.IO;

namespace tarimpl
{
    internal struct TarFileHeader
    {
        // Defines the type of file.
        internal enum EntryType
        {
            // POSIX entry types.

            OldNormal = '\0',   // LF_OLDNORMAL - Normal disk file, Unix compatible
            Normal = '0',       // LF_NORMAL - Normal disk file
            Link = '1',         // LF_LINK - Link to previously dumped file
            SymbolicLink = '2', // LF_SYMLINK - Symbolic link
            Character = '3',    // LF_CHR - Character special file
            Block = '4',        // LF_BLK - Block special file
            Directory = '5',    // LF_DIR - Directory
            Fifo = '6',         // LF_FIFO - FIFO special file
            Contiguous = '7',   // LF_CONTIG - Contiguous file

            // Any other unrecognized value should be treated as a regular file.
        }

        internal struct FilePermissions
        {
            internal const ushort OwnerRead = 0x100; // TUREAD - Owner read (octal 400)
            internal const ushort OwnerWrite = 0x80; // TUWRITE - Owner write (octal 200)
            internal const ushort OwnerExecute = 0x40; // TUEXEC - Owner search/execute (octal 100)

            internal const ushort GroupRead = 0x20; // TGREAD - Group read (octal 40)
            internal const ushort GroupWrite = 0x10; // TGWRITE - Group write (octal 20)
            internal const ushort GroupExecute = 0x8; // TGEXEC - Group execute (octal 10)

            internal const ushort OtherRead = 0x4; // TOREAD - Other read
            internal const ushort OtherWrite = 0x2; // TOWRITE - Other write
            internal const ushort OtherExecute = 0x1; // TOEXEC - Other execute
        }

        internal struct HeaderFieldSizes
        {
            internal const ushort NameSize = 100;   // NAMSIZ
            internal const ushort Mode = 8;
            internal const ushort Uid = 8;
            internal const ushort Gid = 8;
            internal const ushort Size = 12;
            internal const ushort MTime = 12;
            internal const ushort CheckSum = 8;
            internal const ushort LinkName = NameSize;
            internal const ushort Magic = 6;
            internal const ushort Version = 2;
            internal const ushort UName = 32; // TUNMLEN
            internal const ushort GName = 32; // TGNMLEN
            internal const ushort DevMajor = 8;
            internal const ushort DevMinor = 8;
            internal const ushort Prefix = 155;
        }

        internal const ushort RecordSize = 512; // RECORDSIZE

        // The checksum field is filled with this while the checksum is computed.
        internal readonly char[] CheckSumBlanks = new[] { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' }; // CHKBLANKS
        // The magic field is filled with this if uname and gname are valid.
        internal readonly char[] UstarMagicField = new[] { 'u', 's', 't', 'a', 'r', ' ' }; // TMAGIC

        // Bits used in the mode field.
        internal const ushort SetUid = 0x800; // Set UID on execution (octal 4000)
        internal const ushort SetGid = 0x400; // Set GID on execution (octal 2000)
        internal const ushort SaveText = 0x200; // Save text (sticky bit) (octal 1000)

        // All the header fields use 8-bit ASCII characters.
        // Number fields (mode, uid, gid, size, mtime, checksum, linkflag) use octal numbers in ASCII.
        // Old tar implementations filled unused leading bytes in numeric fields with spaces.
        // Modern tar implementations fill unused leading bytes in numberi fields with zeros (best portability).

        internal byte[] _name; // null terminated
        internal byte[] _mode; // ends in space and a null byte
        internal byte[] _uid; // ends in space and a null byte
        internal byte[] _gid; //  ends in space and a null byte

        // For regular files, size indicates the amount of data following the header;
        //  for directories, size may indicate the total size of all files in the directory, so
        //  operating systems that preallocate directory space can use it;
        //  for all other types, it should be zero and ignored by readers.
        internal byte[] _size; // size - ends in space

        internal byte[] _mtime; // ends in space
        internal byte[] _checksum; // ends in null and a space

        // The entry type
        internal byte _typeflag;
        internal byte[] _linkname; // null terminated

        // Contains the magic value 'ustar', to indicate it's POSIX standard archive.
        // For full compliance, uname and gname must be properly set.
        internal byte[] _magic; // null terminated

        internal byte[] _version; // Two nulls

        internal byte[] _uname; // null terminated
        internal byte[] _gname; // null terminated

        // Major number for a character device or block device entry.
        internal byte[] _devmajor;
        // Minor number for a character device or block device entry.
        internal byte[] _devminor;

        // First part of the pathname. If pathname is too long to fit in the 100 bytes of 'Name',
        //  which are provided by the standard format, then it can be split by any  '/' characters,
        //  with the first portion being stored here.
        //  So, if 'Prefix' is not empty, to obtain the regular pathname, we append 'Prefix' + '/' + 'Name'.
        internal byte[] _prefix;

        internal static bool TryReadBlock(BinaryReader reader, out TarFileHeader header)
        {
            header = default;

            header._name = reader.ReadBytes(HeaderFieldSizes.NameSize);
            header._mode = reader.ReadBytes(HeaderFieldSizes.Mode);
            header._uid = reader.ReadBytes(HeaderFieldSizes.Uid);
            header._gid = reader.ReadBytes(HeaderFieldSizes.Gid);
            header._size = reader.ReadBytes(HeaderFieldSizes.Size);
            header._mtime = reader.ReadBytes(HeaderFieldSizes.MTime);
            header._checksum = reader.ReadBytes(HeaderFieldSizes.CheckSum);
            header._typeflag = reader.ReadByte();
            header._linkname = reader.ReadBytes(HeaderFieldSizes.LinkName);
            header._magic = reader.ReadBytes(HeaderFieldSizes.Magic);
            header._version = reader.ReadBytes(HeaderFieldSizes.Version);

            return true;
        }
    }
}