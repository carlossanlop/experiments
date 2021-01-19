#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Path = RedundantSegments.MyPath;

namespace RedundantSegments
{
    public static partial class MyPathInternal
    {
        internal const char DirectorySeparatorChar = '/';
        internal const char AltDirectorySeparatorChar = '/';
        internal const string DirectorySeparatorCharAsString = "/";

        internal static int GetRootLength(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return c == DirectorySeparatorChar;
        }
        [return: NotNullIfNotNull("path")]
        internal static string? NormalizeDirectorySeparators(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Make a pass to see if we need to normalize so we can potentially skip allocating
            bool normalized = true;

            for (int i = 0; i < path.Length; i++)
            {
                if (IsDirectorySeparator(path[i])
                    && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
                {
                    normalized = false;
                    break;
                }
            }

            if (normalized)
                return path;

            StringBuilder builder = new StringBuilder(path.Length);

            for (int i = 0; i < path.Length; i++)
            {
                char current = path[i];

                // Skip if we have another separator following
                if (IsDirectorySeparator(current)
                    && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
                    continue;

                builder.Append(current);
            }

            return builder.ToString();
        }

        internal static bool IsPartiallyQualified(ReadOnlySpan<char> path)
        {
            // This is much simpler than Windows where paths can be rooted, but not fully qualified (such as Drive Relative)
            // As long as the path is rooted in Unix it doesn't use the current directory and therefore is fully qualified.
            return !Path.IsPathRooted(path);
        }

        internal static bool IsEffectivelyEmpty(string? path)
        {
            return string.IsNullOrEmpty(path);
        }

        internal static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
        {
            return path.IsEmpty;
        }
    }
}