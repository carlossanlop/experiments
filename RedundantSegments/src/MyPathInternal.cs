#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RedundantSegments
{
    public static partial class MyPathInternal
    {
        internal static bool IsRoot(ReadOnlySpan<char> path)
            => path.Length == GetRootLength(path);

        [return: NotNullIfNotNull("path")]
        internal static string? TrimEndingDirectorySeparator(string? path) =>
            EndsInDirectorySeparator(path) && !IsRoot(path.AsSpan()) ?
                path!.Substring(0, path.Length - 1) :
                path;

        internal static bool EndsInDirectorySeparator(string? path) =>
              !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]);

        internal static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) =>
            EndsInDirectorySeparator(path) && !IsRoot(path) ?
                path.Slice(0, path.Length - 1) :
                path;
        internal static bool EndsInDirectorySeparator(ReadOnlySpan<char> path) =>
            path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
    }
}