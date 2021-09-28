using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tarimpl
{
    public enum TarArchiveEntryType
    {
        OldNormal = '\0',   // LF_OLDNORMAL - Normal disk file, Unix compatible
        Normal = '0',       // LF_NORMAL - Normal disk file
        Link = '1',         // LF_LINK - Link to previously dumped file
        SymbolicLink = '2', // LF_SYMLINK - Symbolic link
        Character = '3',    // LF_CHR - Character special file
        Block = '4',        // LF_BLK - Block special file
        Directory = '5',    // LF_DIR - Directory
        Fifo = '6',         // LF_FIFO - FIFO special file
        Contiguous = '7',   // LF_CONTIG - Contiguous file

        LongLink = 'L'      // @LongLink - Next file has a long path name
}
}
