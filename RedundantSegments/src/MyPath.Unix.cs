#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Path = RedundantSegments.MyPath;
using PathInternal = RedundantSegments.MyPathInternal;

namespace RedundantSegments
{
    public static partial class MyPath
    {
        public static bool IsPathRooted([NotNullWhen(true)] string? path)
        {
            if (path == null)
                return false;

            return IsPathRooted(path.AsSpan());
        }

        public static bool IsPathRooted(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && path[0] == PathInternal.DirectorySeparatorChar;
        }

        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
                throw new ArgumentException("path empty", nameof(path));

            if (path.Contains('\0'))
                throw new ArgumentException("invalid chars", nameof(path));

            // Expand with current directory if necessary
            if (!IsPathRooted(path))
            {
                path = Combine(Interop.Sys.GetCwd(), path);
            }

            // We would ideally use realpath to do this, but it resolves symlinks, requires that the file actually exist,
            // and turns it into a full path, which we only want if fullCheck is true.
            string collapsedString = Path.RemoveRedundantSegments(path.AsSpan());

            Debug.Assert(collapsedString.Length < path.Length || collapsedString.ToString() == path,
                "Either we've removed characters, or the string should be unmodified from the input path.");

            string result = collapsedString.Length == 0 ? PathInternal.DirectorySeparatorCharAsString : collapsedString;

            return result;
        }
        
        public static string GetFullPath(string path, string basePath)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!IsPathFullyQualified(basePath))
                throw new ArgumentException("base path not fully qualified", nameof(basePath));

            if (basePath.Contains('\0') || path.Contains('\0'))
                throw new ArgumentException("invalid chars");

            if (IsPathFullyQualified(path))
                return GetFullPath(path);

            return GetFullPath(CombineInternal(basePath, path));
        }

    }
}