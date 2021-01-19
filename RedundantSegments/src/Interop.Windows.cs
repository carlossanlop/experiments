#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Interop
{
    public static class Errors
    {
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
    }
    public static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        public static extern uint GetFullPathNameW(ref char lpFileName, uint nBufferLength, ref char lpBuffer, IntPtr lpFilePart);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        internal static extern uint GetLongPathNameW(ref char lpszShortPath, ref char lpszLongPath, uint cchBuffer);
    }
}
