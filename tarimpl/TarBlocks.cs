// Wiki: https://en.wikipedia.org/wiki/Tar_(computing)https://man.netbsd.org/tar.5
// All Specs: https://man.netbsd.org/tar.5

using System;
using System.IO;
using System.Text;

namespace tarimpl
{
    internal struct TarHeader
    {
        internal const short TarFileHeaderSize = 512;

        internal string _name;
        internal int _mode;
        internal int _uid;
        internal int _gid;
        internal long _size;
        internal long _mtime;
        internal int _checksum; // "        internal " is the default value when checksum is computed
        internal byte _typeflag;
        internal string _linkname;
        internal string _magic;
        internal string _version;
        internal string _uname;
        internal string _gname;
        internal int _devmajor;
        internal int _devminor;
        internal string _prefix;

        internal string _fullName;
        internal DateTime _lastWriteTime;

        internal static bool TryReadBlock(BinaryReader reader, out TarHeader header, out long skippedBytes)
        {
            header = default;
            skippedBytes = 0;

            if (!RawHeader.TryReadBlock(reader, out RawHeader rawHeader))
            {
                return false;
            }

            header._name = Encoding.ASCII.GetString(rawHeader._name).TrimEnd('\0');
            header._mode = GetInt32FromOctalString(rawHeader._mode);
            header._uid = GetInt32FromOctalString(rawHeader._uid);
            header._gid = GetInt32FromOctalString(rawHeader._gid);
            header._size = GetInt32FromOctalString(rawHeader._size);
            header._mtime = GetInt32FromOctalString(rawHeader._mtime);
            header._checksum = GetInt32FromOctalString(rawHeader._checksum);
            header._typeflag = rawHeader._typeflag;
            header._linkname = Encoding.ASCII.GetString(rawHeader._linkname).TrimEnd('\0');
            header._magic = Encoding.ASCII.GetString(rawHeader._magic);
            header._version = Encoding.ASCII.GetString(rawHeader._version);
            header._uname = Encoding.ASCII.GetString(rawHeader._uname).TrimEnd('\0');
            header._gname = Encoding.ASCII.GetString(rawHeader._gname).TrimEnd('\0');

            if (header._magic.Equals("ustar ") &&
                (string.IsNullOrEmpty(header._uname) ||
                 string.IsNullOrEmpty(header._gname) ||
                 (!header._version.Equals(" \0") && !header._version.Equals("\0\0"))))
            {
                throw new InvalidDataException("uname or gname empty, or version not 00, when magic is ustar");
            }

            // DevMajor and DevMinor are only available for block and character files
            if (header._typeflag.Equals('3') || header._typeflag.Equals('4'))
            {
                header._devmajor = Convert.ToInt32(Encoding.ASCII.GetString(rawHeader._devmajor));
                header._devminor = Convert.ToInt32(Encoding.ASCII.GetString(rawHeader._devminor));
            }
            header._prefix = Encoding.ASCII.GetString(rawHeader._prefix).TrimEnd('\0');

            if (string.IsNullOrEmpty(header._prefix))
            {
                header._fullName = header._name;
            }
            else
            {
                header._fullName = Path.Join(header._prefix, header._name);
            }

            header._lastWriteTime = DateTime.UnixEpoch.AddSeconds(header._mtime);

            // Advance the reader to skip the file data bytes
            skippedBytes = TarHeader.SkipFileData(reader, header._size);

            return true;
        }

        private static int GetInt32FromOctalString(byte[] field) =>
            Convert.ToInt32(Encoding.ASCII.GetString(field), 8);

        // Move the BinaryReader pointer to the first byte of the next file header.
        private static long SkipFileData(BinaryReader reader, long total)
        {
            long skippedBytes = total;
            while (total > 0)
            {
                if (total > int.MaxValue)
                {
                    reader.ReadBytes(int.MaxValue);
                    total -= int.MaxValue;
                }
                else
                {
                    reader.ReadBytes((int)total);
                    break;
                }
            }

            // After the file contents, there may be zero or more null characters
            while (reader.PeekChar() == 0)
            {
                reader.ReadByte();
                skippedBytes++;
            }

            return skippedBytes;
        }

        private struct TypeFlags
        {
            internal const char OldNormal = '\0';   // LF_OLDNORMAL - Normal disk file, Unix compatible
            internal const char Normal = '0';       // LF_NORMAL - Normal disk file
            internal const char Link = '1';         // LF_LINK - Link to previously dumped file
            internal const char SymbolicLink = '2'; // LF_SYMLINK - Symbolic link
            internal const char Character = '3';    // LF_CHR - Character special file
            internal const char Block = '4';        // LF_BLK - Block special file
            internal const char Directory = '5';    // LF_DIR - Directory
            internal const char Fifo = '6';         // LF_FIFO - FIFO special file
            internal const char Contiguous = '7';   // LF_CONTIG - Contiguous file
        }

        /*
        internal struct filepermissions
        {
            internal const ushort ownerread = 0x100; // turead - owner read (octal 400)
            internal const ushort ownerwrite = 0x80; // tuwrite - owner write (octal 200)
            internal const ushort ownerexecute = 0x40; // tuexec - owner search/execute (octal 100)

            internal const ushort groupread = 0x20; // tgread - group read (octal 40)
            internal const ushort groupwrite = 0x10; // tgwrite - group write (octal 20)
            internal const ushort groupexecute = 0x8; // tgexec - group execute (octal 10)

            internal const ushort otherread = 0x4; // toread - other read
            internal const ushort otherwrite = 0x2; // towrite - other write
            internal const ushort otherexecute = 0x1; // toexec - other execute

            // bits used in the mode field.
            internal const ushort setuid = 0x800; // set uid on execution (octal 4000)
            internal const ushort setgid = 0x400; // set gid on execution (octal 2000)
            internal const ushort savetext = 0x200; // save text (sticky bit) (octal 1000)
        }
        */

        internal struct RawHeader
        {
            // All the header fields use 8-bit ASCII characters.
            // Number fields (mode, uid, gid, size, mtime, checksum, linkflag) use octal numbers in ASCII.
            // Old tar implementations filled unused leading bytes in numeric fields with spaces.
            // Modern tar implementations fill unused leading bytes in numberi fields with zeros (best portability).

            internal byte[] _name;
            internal byte[] _mode;
            internal byte[] _uid;
            internal byte[] _gid; //  ends in space and a null byte

            // For regular files, size indicates the amount of data following the header;
            //  for directories, size may indicate the total size of all files in the directory, so
            //  operating systems that preallocate directory space can use it;
            //  for all other types, it should be zero and ignored by readers.
            internal byte[] _size;

            // last modification timestamp
            internal byte[] _mtime;
            internal byte[] _checksum;

            // The entry type
            internal byte _typeflag;
            internal byte[] _linkname;

            // Contains the magic value 'ustar', to indicate it's POSIX standard archive.
            // For full compliance, uname and gname must be properly set.
            internal byte[] _magic;

            internal byte[] _version;

            internal byte[] _uname;
            internal byte[] _gname;

            // Major number for a character device or block device entry.
            internal byte[] _devmajor;
            // Minor number for a character device or block device entry.
            internal byte[] _devminor;

            // First part of the pathname. If pathname is too long to fit in the 100 bytes of 'Name',
            //  which are provided by the standard format, then it can be split by any  '/' characters,
            //  with the first portion being stored here.
            //  So, if 'Prefix' is not empty, to obtain the regular pathname, we append 'Prefix' + '/' + 'Name'.
            internal byte[] _prefix;

            internal byte[] _pad;

            internal static bool TryReadBlock(BinaryReader reader, out RawHeader header)
            {
                header = default;

                // null terminated
                header._name = reader.ReadBytes(FieldSizes.Name);
                // ends in null
                header._mode = reader.ReadBytes(FieldSizes.Mode);
                byte x = reader.ReadByte();
                // ends in null
                header._uid = reader.ReadBytes(FieldSizes.Uid);
                x = reader.ReadByte();
                // ends in null
                header._gid = reader.ReadBytes(FieldSizes.Gid);
                x = reader.ReadByte();
                // ends in space
                header._size = reader.ReadBytes(FieldSizes.Size);
                x = reader.ReadByte();
                // ends in space
                header._mtime = reader.ReadBytes(FieldSizes.MTime);
                x = reader.ReadByte();
                // ends in null
                header._checksum = reader.ReadBytes(FieldSizes.CheckSum);
                byte[] y = reader.ReadBytes(2);
                header._typeflag = reader.ReadByte();
                // null terminated
                header._linkname = reader.ReadBytes(FieldSizes.LinkName);
                // null terminated
                header._magic = reader.ReadBytes(FieldSizes.Magic);
                // two nulls
                header._version = reader.ReadBytes(FieldSizes.Version);
                // null terminated
                header._uname = reader.ReadBytes(FieldSizes.UName);
                // null terminated
                header._gname = reader.ReadBytes(FieldSizes.GName);
                header._devmajor = reader.ReadBytes(FieldSizes.DevMajor);
                header._devminor = reader.ReadBytes(FieldSizes.DevMinor);
                // null terminated
                header._prefix = reader.ReadBytes(FieldSizes.Prefix);
                header._pad = reader.ReadBytes(FieldSizes.Pad);

                return true;
            }

            private struct FieldSizes
            {
                private const ushort PathLength = 100;

                internal const ushort Name = PathLength;
                internal const ushort Mode = 7; // excludes null
                internal const ushort Uid = 7; // excludes null
                internal const ushort Gid = 7; // excludes null
                internal const ushort Size = 11; // excludes space
                internal const ushort MTime = 11; // excludes space
                internal const ushort CheckSum = 6; // excludes null and space
                internal const ushort LinkName = PathLength;
                internal const ushort Magic = 6;
                internal const ushort Version = 2; // two nulls or a space and a null
                internal const ushort UName = 32;
                internal const ushort GName = 32;
                internal const ushort DevMajor = 8;
                internal const ushort DevMinor = 8;
                internal const ushort Prefix = 155;
                internal const ushort Pad = 12;
            }
        }
    }
}