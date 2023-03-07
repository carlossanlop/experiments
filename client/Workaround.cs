using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Client;

internal static class Workaround
{
    internal static SafePipeHandle? GetSafePipeHandle(PipeDirection direction,
        HandleInheritability inheritability,
        TokenImpersonationLevel impersonationLevel,
        PipeOptions pipeOptions,
        int timeout,
        string serverName,
        string pipeName)
    {
        SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability);

        int pipeFlags = (int)(pipeOptions & ~PipeOptions.CurrentUserOnly);
        if (impersonationLevel != TokenImpersonationLevel.None)
        {
            pipeFlags |= SecurityOptions.SECURITY_SQOS_PRESENT;
            pipeFlags |= (((int)impersonationLevel - 1) << 16);
        }

        // This was the problem - If we want to modify the handle after
        // connecting, we need to specify write access to the pipe
        // https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createnamedpipea#remarks
        int access = FileOperations.FILE_WRITE_ATTRIBUTES;

        if ((PipeDirection.Out & direction) != 0)
        {
            access |= GenericOperations.GENERIC_WRITE;
        }
        if ((PipeDirection.In & direction) != 0)
        {
            access |= GenericOperations.GENERIC_READ;
        }

        string normalizedPipePath = GetPipePath(serverName, pipeName);
        SafePipeHandle safePipeHandle = CreateNamedPipeClient(normalizedPipePath, ref secAttrs, pipeFlags, access);

        if (safePipeHandle.IsInvalid)
        {
            int errorCode = Marshal.GetLastPInvokeError();

            safePipeHandle.Dispose();

            if (errorCode == Errors.ERROR_FILE_NOT_FOUND)
            {
                return null;
            }

            if (errorCode != Errors.ERROR_PIPE_BUSY)
            {
                throw GetExceptionForWin32Error(errorCode);
            }

            if (!WaitNamedPipeW(normalizedPipePath, timeout))
            {
                errorCode = Marshal.GetLastPInvokeError();

                if (errorCode == Errors.ERROR_FILE_NOT_FOUND ||
                    errorCode == Errors.ERROR_SEM_TIMEOUT)
                {
                    return null;
                }

                throw GetExceptionForWin32Error(errorCode);
            }

            safePipeHandle = CreateNamedPipeClient(normalizedPipePath, ref secAttrs, pipeFlags, access);

            if (safePipeHandle.IsInvalid)
            {
                errorCode = Marshal.GetLastPInvokeError();

                safePipeHandle.Dispose();

                if (errorCode == Errors.ERROR_PIPE_BUSY ||
                    errorCode == Errors.ERROR_FILE_NOT_FOUND)
                {
                    return null;
                }

                throw GetExceptionForWin32Error(errorCode);
            }
        }

        if (((pipeOptions & PipeOptions.CurrentUserOnly) != 0))
        {
            Type pipeSecurityType = typeof(PipeSecurity);
            Type[] parameterTypes = new[] { typeof(SafePipeHandle), typeof(AccessControlSections) };
            ConstructorInfo pipeHandleConstructor = pipeSecurityType.GetConstructor(BindingFlags.NonPublic, parameterTypes) ?? throw new TypeLoadException("Could not find the required internal PipeSecurity constructor.");

            PipeSecurity accessControl = (PipeSecurity)pipeHandleConstructor.Invoke(new object[] { safePipeHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group });

            IdentityReference? remoteOwnerSid = accessControl.GetOwner(typeof(SecurityIdentifier));

            using WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            SecurityIdentifier? currentUserSid = currentIdentity.Owner;
            if (remoteOwnerSid != currentUserSid)
            {
                safePipeHandle.Dispose();
                throw new UnauthorizedAccessException("Could not connect to the pipe because it was not owned by the current user.");
            }
        }

        return safePipeHandle;
    }

    private static string GetPipePath(string serverName, string pipeName)
    {
        string normalizedPipePath = Path.GetFullPath(@"\\" + serverName + @"\pipe\" + pipeName);
        if (string.Equals(normalizedPipePath, @"\\.\pipe\anonymous", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(pipeName), "The pipeName 'anonymous' is reserved.");
        }
        return normalizedPipePath;
    }

    private static unsafe SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
    {
        SECURITY_ATTRIBUTES secAttrs = new()
        {
            nLength = (uint)sizeof(SECURITY_ATTRIBUTES),
            bInheritHandle = ((inheritability & HandleInheritability.Inheritable) != 0) ? BOOL.TRUE : BOOL.FALSE
        };

        return secAttrs;
    }

    private static Exception GetExceptionForWin32Error(int errorCode, string? path = "")
    {
        // ERROR_SUCCESS gets thrown when another unexpected interop call was made before checking GetLastWin32Error().
        // Errors have to get retrieved as soon as possible after P/Invoking to avoid this.
        Debug.Assert(errorCode != Errors.ERROR_SUCCESS);

        switch (errorCode)
        {
            case Errors.ERROR_FILE_NOT_FOUND:
                return new FileNotFoundException(
                    string.IsNullOrEmpty(path) ? "Unable to find the specified file." : string.Format("Could not find file '{0}'.", path), path);
            case Errors.ERROR_PATH_NOT_FOUND:
                return new DirectoryNotFoundException(
                    string.IsNullOrEmpty(path) ? "Could not find a part of the path." : string.Format("Could not find a part of the path '{0}'.", path));
            case Errors.ERROR_ACCESS_DENIED:
                return new UnauthorizedAccessException(
                    string.IsNullOrEmpty(path) ? "Access to the path is denied." : string.Format("Access to the path '{0}' is denied.", path));
            case Errors.ERROR_ALREADY_EXISTS:
                if (string.IsNullOrEmpty(path))
                    goto default;
                return new IOException(string.Format("Cannot create '{0}' because a file or directory with the same name already exists.", path), MakeHRFromErrorCode(errorCode));
            case Errors.ERROR_FILENAME_EXCED_RANGE:
                return new PathTooLongException(
                    string.IsNullOrEmpty(path) ? "The specified file name or path is too long, or a component of the specified path is too long." : string.Format("The path '{0}' is too long, or a component of the specified path is too long.", path));
            case Errors.ERROR_SHARING_VIOLATION:
                return new IOException(
                    string.IsNullOrEmpty(path) ? "The process cannot access the file because it is being used by another process." : string.Format("The process cannot access the file '{0}' because it is being used by another process.", path),
                    MakeHRFromErrorCode(errorCode));
            case Errors.ERROR_FILE_EXISTS:
                if (string.IsNullOrEmpty(path))
                    goto default;
                return new IOException(string.Format("The file '{0}' already exists.", path), MakeHRFromErrorCode(errorCode));
            case Errors.ERROR_OPERATION_ABORTED:
                return new OperationCanceledException();
            case Errors.ERROR_INVALID_PARAMETER:
            default:
                string msg = string.IsNullOrEmpty(path)
                    ? GetPInvokeErrorMessage(errorCode)
                    : $"{GetPInvokeErrorMessage(errorCode)} : '{path}'";
                return new IOException(
                    msg,
                    MakeHRFromErrorCode(errorCode));
        }

        static string GetPInvokeErrorMessage(int errorCode)
        {
            // Call Kernel32.GetMessage directly in CoreLib. It eliminates one level of indirection and it is necessary to
            // produce correct error messages for CoreCLR Win32 PAL.
#if NET7_0_OR_GREATER && !SYSTEM_PRIVATE_CORELIB
            return Marshal.GetPInvokeErrorMessage(errorCode);
#else
            return GetMessage(errorCode);
#endif
        }
    }

    [DllImport(Kernel32, SetLastError = true)]
    private static extern unsafe int FormatMessageW(
       int dwFlags,
       IntPtr lpSource,
       uint dwMessageId,
       int dwLanguageId,
       void* lpBuffer,
       int nSize,
       IntPtr arguments);

    internal static string GetMessage(int errorCode) =>
        GetMessage(errorCode, IntPtr.Zero);

    private static string GetAndTrimString(Span<char> buffer)
    {
        int length = buffer.Length;
        while (length > 0 && buffer[length - 1] <= 32)
        {
            length--;
        }
        return buffer[..length].ToString();
    }

    internal static unsafe string GetMessage(int errorCode, IntPtr moduleHandle)
    {
        int flags = FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY;
        if (moduleHandle != IntPtr.Zero)
        {
            flags |= FORMAT_MESSAGE_FROM_HMODULE;
        }

        Span<char> stackBuffer = stackalloc char[256];
        fixed (char* bufferPtr = stackBuffer)
        {
            int length = FormatMessageW(flags, moduleHandle, unchecked((uint)errorCode), 0, bufferPtr, stackBuffer.Length, IntPtr.Zero);
            if (length > 0)
            {
                return GetAndTrimString(stackBuffer[..length]);
            }
        }

        if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
        {
            IntPtr nativeMsgPtr = default;
            try
            {
                int length = FormatMessageW(flags | FORMAT_MESSAGE_ALLOCATE_BUFFER, moduleHandle, unchecked((uint)errorCode), 0, &nativeMsgPtr, 0, IntPtr.Zero);
                if (length > 0)
                {
                    return GetAndTrimString(new Span<char>((char*)nativeMsgPtr, length));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(nativeMsgPtr);
            }
        }

        return $"Unknown error (0x{errorCode:x})";
    }

    private static int MakeHRFromErrorCode(int errorCode)
    {
        // Don't convert it if it is already an HRESULT
        if ((0xFFFF0000 & errorCode) != 0)
            return errorCode;

        return unchecked(((int)0x80070000) | errorCode);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        internal uint nLength;
        internal IntPtr lpSecurityDescriptor;
        internal BOOL bInheritHandle;
    }

    private static class GenericOperations
    {
        internal const int GENERIC_READ = unchecked((int)0x80000000);
        internal const int GENERIC_WRITE = 0x40000000;
    }

    internal static partial class FileOperations
    {
        internal const int FILE_WRITE_ATTRIBUTES = 0x100;
    }

    private static class SecurityOptions
    {
        internal const int SECURITY_SQOS_PRESENT = 0x00100000;
    }

    private enum BOOL : int
    {
        FALSE = 0,
        TRUE = 1,
    }

    private static class Errors
    {
        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_FILE_EXISTS = 0x50;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_PIPE_BUSY = 0xE7;
        internal const int ERROR_SEM_TIMEOUT = 0x79;
        internal const int ERROR_SHARING_VIOLATION = 0x20;
    }

    private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
    private const int FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
    private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
    private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    private const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
    private const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

    private const string Kernel32 = "kernel32.dll";

    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WaitNamedPipeW(string? name, int timeout);

    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafePipeHandle CreateFileW(
        string? lpFileName,
        int dwDesiredAccess,
        FileShare dwShareMode,
        ref SECURITY_ATTRIBUTES secAttrs,
        FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    private static SafePipeHandle CreateNamedPipeClient(string? path, ref SECURITY_ATTRIBUTES secAttrs, int pipeFlags, int access)
            => CreateFileW(path, access, FileShare.None, ref secAttrs, FileMode.Open, pipeFlags, hTemplateFile: IntPtr.Zero);
}
