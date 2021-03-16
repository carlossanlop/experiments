using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyNamespace
{
    class MyClass
    {
        private const int OneKibibyte = 1 << 10; // 1024
        private const int HalfKibibyte = OneKibibyte >> 1;
        private const int FourKibibytes = OneKibibyte << 2; // default Stream buffer size
        private const int OneMibibyte = OneKibibyte << 10;

        private static readonly string SourceFilePath = Path.Combine(Path.GetTempPath(), "TempFile.txt");
        private static readonly string DestinationFilePath = Path.Combine(Path.GetTempPath(), "Destination.txt");

        [System.Runtime.Versioning.UnsupportedOSPlatform("macos")]
        public static async Task Main()
        {
            File.Delete(SourceFilePath);
            File.Delete(DestinationFilePath);
            File.WriteAllBytes(SourceFilePath, new byte[OneMibibyte]);
            File.WriteAllBytes(DestinationFilePath, new byte[OneMibibyte]);

            // // Uncomment to verify env variable values are properly set
            // CheckEnvVars("DOTNET_ROOT", "DOTNET_MULTILEVEL_LOOKUP", "DOTNET_SYSTEM_IO_USELEGACYFILESTREAM", "PATH");
            // Console.WriteLine("Successful checks! Starting test...");

            // Same (not too large) number of iterations for all runs, to ensure a 1:1 comparison
            for (int i = 1; i <= 250; i++)
            {
                // Method to test goes here
                //await WriteAsync(FourKibibytes, HalfKibibyte, FileOptions.Asynchronous);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool OpenClose(FileOptions options)
        {
            bool b;
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                b = fileStream.IsAsync;
            }
            return b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [System.Runtime.Versioning.UnsupportedOSPlatform("macos")]
        public static void LockUnlock(FileOptions options)
        {
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FourKibibytes, options))
            {
                fileStream.Lock(0, fileStream.Length);
                fileStream.Unlock(0, fileStream.Length);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SeekForward(FileOptions options)
        {
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                for (long offset = 0; offset < OneKibibyte; offset++)
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SeekBackward(FileOptions options)
        {
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                for (long offset = -1; offset >= -OneKibibyte; offset--)
                {
                    fileStream.Seek(offset, SeekOrigin.End);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReadByte(FileOptions options)
        {
            int result = default;
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FourKibibytes, options))
            {
                for (long i = 0; i < OneKibibyte; i++)
                {
                    result += fileStream.ReadByte();
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WriteByte(FileOptions options)
        {
            using (var fileStream = new FileStream(DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, FourKibibytes, options))
            {
                for (int i = 0; i < OneKibibyte; i++)
                {
                    fileStream.WriteByte(default);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long Read(
            int bufferSize, // 1, FourKibibytes
            int userBufferSize) // HalfKibibyte = buffering makes sense ; FourKibibytes = buffering makes NO sense
        {
            byte[] userBuffer = new byte[userBufferSize];
            long bytesRead = 0;
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.None))
            {
                while (bytesRead < OneMibibyte)
                {
                    bytesRead += fileStream.Read(userBuffer, 0, userBuffer.Length);
                }
            }
            return bytesRead;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Write(
            int bufferSize, // 1, FourKibibytes
            int userBufferSize) // HalfKibibyte, FourKibibytes
        {
            byte[] userBuffer = new byte[userBufferSize];
            using (var fileStream = new FileStream(DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.None))
            {
                for (int i = 0; i < OneMibibyte / userBufferSize; i++)
                {
                    fileStream.Write(userBuffer, 0, userBuffer.Length);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Flush(
            int bufferSize,     // 1, FourKibibytes
            FileOptions options)
        {
            using (var fileStream = new FileStream(DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, options))
            {
                for (int i = 0; i < OneKibibyte; i++)
                {
                    fileStream.WriteByte(default); // make sure that Flush has something to actualy flush to disk
                    fileStream.Flush();
                } 
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CopyTo(
            int bufferSize,     // 1, FourKibibytes
            FileOptions options)
        {
            using (var source = new FileStream(SourceFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read, bufferSize, options))
            using (var destination = new FileStream(DestinationFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, bufferSize, options))
            {
                source.CopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task<long> ReadAsync(
            int bufferSize,     // 1, FourKibibytes
            int userBufferSize, // HalfKibibyte, FourKibibytes
            FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] buffer = new byte[userBufferSize];
            var userBuffer = new Memory<byte>(buffer);
            long bytesRead = 0;
            using (var fileStream = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, options))
            {
                while (bytesRead < OneMibibyte)
                {
                    bytesRead += await fileStream.ReadAsync(userBuffer, cancellationToken);
                }
            }

            return bytesRead;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task WriteAsync(
            int bufferSize,     // 1, FourKibibytes
            int userBufferSize, // HalfKibibyte, FourKibibytes
            FileOptions options)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            byte[] buffer = new byte[userBufferSize];
            Memory<byte> userBuffer = new Memory<byte>(buffer);
            using (var fileStream = new FileStream(DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, options))
            {
                for (int i = 0; i < OneMibibyte / userBufferSize; i++)
                {
                    await fileStream.WriteAsync(userBuffer, cancellationToken);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task FlushAsync(
            int bufferSize,     // 1, FourKibibytes
            FileOptions options)
        {
            using (var fileStream = new FileStream(DestinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, options))
            {
                for (int i = 0; i < OneKibibyte; i++)
                {
                    fileStream.WriteByte(default);
                    await fileStream.FlushAsync();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task CopyToAsync(
            int bufferSize, // 1, FourKibibytes
            FileOptions options)
        {
            using (var source = new FileStream(SourceFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read, bufferSize, options))
            using (var destination = new FileStream(DestinationFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, bufferSize, options))
            {
                await source.CopyToAsync(destination);
            }
        }

        //private static void CheckEnvVars(params string[] varNames)
        //{
        //    foreach (string varName in varNames)
        //    {
        //        if (Environment.GetEnvironmentVariable(varName) is string varValue)
        //        {
        //            Console.WriteLine($"Found {varName}: {varValue}");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Did NOT find {varName}!");
        //            Environment.Exit(0);
        //        }
        //    }
        //}
    }
}
