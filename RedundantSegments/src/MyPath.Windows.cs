#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Path = RedundantSegments.MyPath;
using PathInternal = RedundantSegments.MyPathInternal;
using PathHelper = RedundantSegments.MyPathHelper;

namespace RedundantSegments
{
    public static partial class MyPath
    {
        public static bool IsPathRooted([NotNullWhen(true)] string? path)
        {
            return path != null && IsPathRooted(path.AsSpan());
        }

        public static bool IsPathRooted(ReadOnlySpan<char> path)
        {
            int length = path.Length;
            return (length >= 1 && PathInternal.IsDirectorySeparator(path[0]))
                || (length >= 2 && PathInternal.IsValidDriveChar(path[0]) && path[1] == PathInternal.VolumeSeparatorChar);
        }

        public static ReadOnlySpan<char> GetPathRoot(ReadOnlySpan<char> path)
        {
            if (PathInternal.IsEffectivelyEmpty(path))
                return ReadOnlySpan<char>.Empty;

            int rootLength = PathInternal.GetRootLength(path);
            return rootLength <= 0 ? ReadOnlySpan<char>.Empty : path.Slice(0, rootLength);
        }

        internal static int GetUncRootLength(ReadOnlySpan<char> path)
        {
            bool isDevice = PathInternal.IsDevice(path);

            if (!isDevice && path.Slice(0, 2).EqualsOrdinal(@"\\".AsSpan()))
                return 2;
            else if (isDevice && path.Length >= 8
                && (path.Slice(0, 8).EqualsOrdinal(PathInternal.UncExtendedPathPrefix.AsSpan())
                || path.Slice(5, 4).EqualsOrdinal(@"UNC\".AsSpan())))
                return 8;

            return -1;
        }

        internal static ReadOnlySpan<char> GetVolumeName(ReadOnlySpan<char> path)
        {
            // 3 cases: UNC ("\\server\share"), Device ("\\?\C:\"), or Dos ("C:\")
            ReadOnlySpan<char> root = GetPathRoot(path);
            if (root.Length == 0)
                return root;

            // Cut from "\\?\UNC\Server\Share" to "Server\Share"
            // Cut from  "\\Server\Share" to "Server\Share"
            int startOffset = GetUncRootLength(path);
            if (startOffset == -1)
            {
                if (PathInternal.IsDevice(path))
                {
                    startOffset = 4; // Cut from "\\?\C:\" to "C:"
                }
                else
                {
                    startOffset = 0; // e.g. "C:"
                }
            }

            ReadOnlySpan<char> pathToTrim = root.Slice(startOffset);
            return Path.EndsInDirectorySeparator(pathToTrim) ? pathToTrim.Slice(0, pathToTrim.Length - 1) : pathToTrim;
        }

        public static string GetFullPath(string path, string basePath)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!IsPathFullyQualified(basePath))
                throw new ArgumentException(nameof(basePath));

            if (basePath.Contains('\0') || path.Contains('\0'))
                throw new ArgumentException();

            if (IsPathFullyQualified(path))
                return GetFullPath(path);

            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                return basePath;

            int length = path.Length;
            string combinedPath;
            if (length >= 1 && PathInternal.IsDirectorySeparator(path[0]))
            {
                // Path is current drive rooted i.e. starts with \:
                // "\Foo" and "C:\Bar" => "C:\Foo"
                // "\Foo" and "\\?\C:\Bar" => "\\?\C:\Foo"
                combinedPath = Join(GetPathRoot(basePath.AsSpan()), path.AsSpan(1)); // Cut the separator to ensure we don't end up with two separators when joining with the root.
            }
            else if (length >= 2 && PathInternal.IsValidDriveChar(path[0]) && path[1] == PathInternal.VolumeSeparatorChar)
            {
                // Drive relative paths
                Debug.Assert(length == 2 || !PathInternal.IsDirectorySeparator(path[2]));

                if (GetVolumeName(path.AsSpan()).EqualsOrdinal(GetVolumeName(basePath.AsSpan())))
                {
                    // Matching root
                    // "C:Foo" and "C:\Bar" => "C:\Bar\Foo"
                    // "C:Foo" and "\\?\C:\Bar" => "\\?\C:\Bar\Foo"
                    combinedPath = Join(basePath.AsSpan(), path.AsSpan(2));
                }
                else
                {
                    // No matching root, root to specified drive
                    // "D:Foo" and "C:\Bar" => "D:Foo"
                    // "D:Foo" and "\\?\C:\Bar" => "\\?\D:\Foo"
                    combinedPath = !PathInternal.IsDevice(basePath.AsSpan())
                        ? path.Insert(2, @"\")
                        : length == 2
                            ? JoinInternal(basePath.AsSpan(0, 4), path.AsSpan(), @"\".AsSpan())
                            : JoinInternal(basePath.AsSpan(0, 4), path.AsSpan(0, 2), @"\".AsSpan(), path.AsSpan(2));
                }
            }
            else
            {
                // "Simple" relative path
                // "Foo" and "C:\Bar" => "C:\Bar\Foo"
                // "Foo" and "\\?\C:\Bar" => "\\?\C:\Bar\Foo"
                combinedPath = JoinInternal(basePath.AsSpan(), path.AsSpan());
            }

            // Device paths are normalized by definition, so passing something of this format (i.e. \\?\C:\.\tmp, \\.\C:\foo)
            // to Windows APIs won't do anything by design. Additionally, GetFullPathName() in Windows doesn't root
            // them properly. As such we need to manually remove segments and not use GetFullPath().

            return PathInternal.IsDevice(combinedPath.AsSpan())
                ? RemoveRedundantSegments(combinedPath.AsSpan())
                : GetFullPath(combinedPath);
        }

        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // If the path would normalize to string empty, we'll consider it empty
            if (PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                throw new ArgumentException("path empty", nameof(path));

            // Embedded null characters are the only invalid character case we trully care about.
            // This is because the nulls will signal the end of the string to Win32 and therefore have
            // unpredictable results.
            if (path.Contains('\0'))
                throw new ArgumentException("invalid chars", nameof(path));

            if (PathInternal.IsExtended(path.AsSpan()))
            {
                // \\?\ paths are considered normalized by definition. Windows doesn't normalize \\?\
                // paths and neither should we. Even if we wanted to GetFullPathName does not work
                // properly with device paths. If one wants to pass a \\?\ path through normalization
                // one can chop off the prefix, pass it to GetFullPath and add it again.
                return path;
            }

            return PathHelper.Normalize(path);
        }
    }
}