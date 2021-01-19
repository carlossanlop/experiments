#nullable enable

using System;
using System.IO;
using System.Buffers;
using System.Runtime.InteropServices;

internal static class Interop
{
    internal enum Error
    {
        SUCCESS          = 0,

        E2BIG            = 0x10001,           // Argument list too long.
        EACCES           = 0x10002,           // Permission denied.
        EADDRINUSE       = 0x10003,           // Address in use.
        EADDRNOTAVAIL    = 0x10004,           // Address not available.
        EAFNOSUPPORT     = 0x10005,           // Address family not supported.
        EAGAIN           = 0x10006,           // Resource unavailable, try again (same value as EWOULDBLOCK),
        EALREADY         = 0x10007,           // Connection already in progress.
        EBADF            = 0x10008,           // Bad file descriptor.
        EBADMSG          = 0x10009,           // Bad message.
        EBUSY            = 0x1000A,           // Device or resource busy.
        ECANCELED        = 0x1000B,           // Operation canceled.
        ECHILD           = 0x1000C,           // No child processes.
        ECONNABORTED     = 0x1000D,           // Connection aborted.
        ECONNREFUSED     = 0x1000E,           // Connection refused.
        ECONNRESET       = 0x1000F,           // Connection reset.
        EDEADLK          = 0x10010,           // Resource deadlock would occur.
        EDESTADDRREQ     = 0x10011,           // Destination address required.
        EDOM             = 0x10012,           // Mathematics argument out of domain of function.
        EDQUOT           = 0x10013,           // Reserved.
        EEXIST           = 0x10014,           // File exists.
        EFAULT           = 0x10015,           // Bad address.
        EFBIG            = 0x10016,           // File too large.
        EHOSTUNREACH     = 0x10017,           // Host is unreachable.
        EIDRM            = 0x10018,           // Identifier removed.
        EILSEQ           = 0x10019,           // Illegal byte sequence.
        EINPROGRESS      = 0x1001A,           // Operation in progress.
        EINTR            = 0x1001B,           // Interrupted function.
        EINVAL           = 0x1001C,           // Invalid argument.
        EIO              = 0x1001D,           // I/O error.
        EISCONN          = 0x1001E,           // Socket is connected.
        EISDIR           = 0x1001F,           // Is a directory.
        ELOOP            = 0x10020,           // Too many levels of symbolic links.
        EMFILE           = 0x10021,           // File descriptor value too large.
        EMLINK           = 0x10022,           // Too many links.
        EMSGSIZE         = 0x10023,           // Message too large.
        EMULTIHOP        = 0x10024,           // Reserved.
        ENAMETOOLONG     = 0x10025,           // Filename too long.
        ENETDOWN         = 0x10026,           // Network is down.
        ENETRESET        = 0x10027,           // Connection aborted by network.
        ENETUNREACH      = 0x10028,           // Network unreachable.
        ENFILE           = 0x10029,           // Too many files open in system.
        ENOBUFS          = 0x1002A,           // No buffer space available.
        ENODEV           = 0x1002C,           // No such device.
        ENOENT           = 0x1002D,           // No such file or directory.
        ENOEXEC          = 0x1002E,           // Executable file format error.
        ENOLCK           = 0x1002F,           // No locks available.
        ENOLINK          = 0x10030,           // Reserved.
        ENOMEM           = 0x10031,           // Not enough space.
        ENOMSG           = 0x10032,           // No message of the desired type.
        ENOPROTOOPT      = 0x10033,           // Protocol not available.
        ENOSPC           = 0x10034,           // No space left on device.
        ENOSYS           = 0x10037,           // Function not supported.
        ENOTCONN         = 0x10038,           // The socket is not connected.
        ENOTDIR          = 0x10039,           // Not a directory or a symbolic link to a directory.
        ENOTEMPTY        = 0x1003A,           // Directory not empty.
        ENOTRECOVERABLE  = 0x1003B,           // State not recoverable.
        ENOTSOCK         = 0x1003C,           // Not a socket.
        ENOTSUP          = 0x1003D,           // Not supported (same value as EOPNOTSUP).
        ENOTTY           = 0x1003E,           // Inappropriate I/O control operation.
        ENXIO            = 0x1003F,           // No such device or address.
        EOVERFLOW        = 0x10040,           // Value too large to be stored in data type.
        EOWNERDEAD       = 0x10041,           // Previous owner died.
        EPERM            = 0x10042,           // Operation not permitted.
        EPIPE            = 0x10043,           // Broken pipe.
        EPROTO           = 0x10044,           // Protocol error.
        EPROTONOSUPPORT  = 0x10045,           // Protocol not supported.
        EPROTOTYPE       = 0x10046,           // Protocol wrong type for socket.
        ERANGE           = 0x10047,           // Result too large.
        EROFS            = 0x10048,           // Read-only file system.
        ESPIPE           = 0x10049,           // Invalid seek.
        ESRCH            = 0x1004A,           // No such process.
        ESTALE           = 0x1004B,           // Reserved.
        ETIMEDOUT        = 0x1004D,           // Connection timed out.
        ETXTBSY          = 0x1004E,           // Text file busy.
        EXDEV            = 0x1004F,           // Cross-device link.
        ESOCKTNOSUPPORT  = 0x1005E,           // Socket type not supported.
        EPFNOSUPPORT     = 0x10060,           // Protocol family not supported.
        ESHUTDOWN        = 0x1006C,           // Socket shutdown.
        EHOSTDOWN        = 0x10070,           // Host is down.
        ENODATA          = 0x10071,           // No data available.

        // Custom Error codes to track errors beyond kernel interface.
        EHOSTNOTFOUND    = 0x20001,           // Name lookup failed
        ESOCKETERROR     = 0x20002,           // Unspecified socket error

        // POSIX permits these to have the same value and we make them always equal so
        // that we do not introduce a dependency on distinguishing between them that
        // would not work on all platforms.
        EOPNOTSUPP      = ENOTSUP,            // Operation not supported on socket.
        EWOULDBLOCK     = EAGAIN,             // Operation would block.
    }

    internal static Exception GetExceptionForIoErrno(ErrorInfo errorInfo, string? path = null, bool isDirectory = false)
    {
        // Translate the errno into a known set of exception types.  For cases where multiple errnos map
        // to the same exception type, include an inner exception with the details.
        switch (errorInfo.Error)
        {
            case Error.ENOENT:
                if (isDirectory)
                {
                    return !string.IsNullOrEmpty(path) ?
                        new DirectoryNotFoundException($"path not found {path}") :
                        new DirectoryNotFoundException("path not found");
                }
                else
                {
                    return !string.IsNullOrEmpty(path) ?
                        new FileNotFoundException($"path not found {path}") :
                        new FileNotFoundException("path not found");
                }

            case Error.EACCES:
            case Error.EBADF:
            case Error.EPERM:
                Exception inner = GetIOException(errorInfo);
                return !string.IsNullOrEmpty(path) ?
                    new UnauthorizedAccessException($"unauthorized access io denied {path}", inner) :
                    new UnauthorizedAccessException("unauthorized access io denied", inner);

            case Error.ENAMETOOLONG:
                return !string.IsNullOrEmpty(path) ?
                    new PathTooLongException($"path too long {path}") :
                    new PathTooLongException("path too long");

            case Error.EWOULDBLOCK:
                return !string.IsNullOrEmpty(path) ?
                    new IOException($"io sharing violation {path}", errorInfo.RawErrno) :
                    new IOException("io sharing violation", errorInfo.RawErrno);

            case Error.ECANCELED:
                return new OperationCanceledException();

            case Error.EFBIG:
                return new ArgumentOutOfRangeException("value", "file length too big");

            case Error.EEXIST:
                if (!string.IsNullOrEmpty(path))
                {
                    return new IOException($"file exists {path}", errorInfo.RawErrno);
                }
                goto default;

            default:
                return GetIOException(errorInfo);
        }
    }

    internal static Exception GetIOException(Interop.ErrorInfo errorInfo)
    {
        return new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
    }

    // Represents a platform-agnostic Error and underlying platform-specific errno
    internal struct ErrorInfo
    {
        private readonly Error _error;
        private int _rawErrno;

        internal ErrorInfo(int errno)
        {
            _error = Interop.Sys.ConvertErrorPlatformToPal(errno);
            _rawErrno = errno;
        }

        internal ErrorInfo(Error error)
        {
            _error = error;
            _rawErrno = -1;
        }

        internal Error Error
        {
            get { return _error; }
        }

        internal int RawErrno
        {
            get { return _rawErrno == -1 ? (_rawErrno = Interop.Sys.ConvertErrorPalToPlatform(_error)) : _rawErrno; }
        }

        internal string GetErrorMessage()
        {
            return Interop.Sys.StrError(RawErrno);
        }

        public override string ToString()
        {
            return $"RawErrno: {RawErrno} Error: {Error} GetErrorMessage: {GetErrorMessage()}"; // No localization required; text is member names used for debugging purposes
        }
    }

    internal static class Libraries
    {
        internal const string SystemNative = "libSystem.Native";
    };

    internal static class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetCwd", SetLastError = true)]
        private static extern unsafe byte* GetCwd(byte* buffer, int bufferLength);

        internal static unsafe string GetCwd()
        {
            const int StackLimit = 256;

            // First try to get the path into a buffer on the stack
            byte* stackBuf = stackalloc byte[StackLimit];
            string? result = GetCwdHelper(stackBuf, StackLimit);
            if (result != null)
            {
                return result;
            }

            // If that was too small, try increasing large buffer sizes
            int bufferSize = StackLimit;
            while (true)
            {
                checked { bufferSize *= 2; }
                byte[] buf = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    fixed (byte* ptr = &buf[0])
                    {
                        result = GetCwdHelper(ptr, buf.Length);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buf);
                }
            }
        }

        private static unsafe string? GetCwdHelper(byte* ptr, int bufferSize)
        {
            // Call the real getcwd
            byte* result = GetCwd(ptr, bufferSize);

            // If it returned non-null, the null-terminated path is in the buffer
            if (result != null)
            {
                return Marshal.PtrToStringAnsi((IntPtr)ptr);
            }

            // Otherwise, if it failed due to the buffer being too small, return null;
            // for anything else, throw.
            ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
               return null;
            }
            throw Interop.GetExceptionForIoErrno(errorInfo);
        }

        internal static ErrorInfo GetLastErrorInfo()
        {
            return new ErrorInfo(Marshal.GetLastWin32Error());
        }

        internal static unsafe string StrError(int platformErrno)
        {
            int maxBufferLength = 1024; // should be long enough for most any UNIX error
            byte* buffer = stackalloc byte[maxBufferLength];
            byte* message = StrErrorR(platformErrno, buffer, maxBufferLength);

            if (message == null)
            {
                // This means the buffer was not large enough, but still contains
                // as much of the error message as possible and is guaranteed to
                // be null-terminated. We're not currently resizing/retrying because
                // maxBufferLength is large enough in practice, but we could do
                // so here in the future if necessary.
                message = buffer;
            }

            return Marshal.PtrToStringAnsi((IntPtr)message)!;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ConvertErrorPlatformToPal")]
        [SuppressGCTransition]
        internal static extern Error ConvertErrorPlatformToPal(int platformErrno);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ConvertErrorPalToPlatform")]
        [SuppressGCTransition]
        internal static extern int ConvertErrorPalToPlatform(Error error);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_StrErrorR")]
        private static extern unsafe byte* StrErrorR(int platformErrno, byte* buffer, int bufferSize);

    }
}