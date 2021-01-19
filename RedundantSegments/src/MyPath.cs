#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Path = RedundantSegments.MyPath;
using PathInternal = RedundantSegments.MyPathInternal;

namespace RedundantSegments
{
    public static partial class MyPath
    {
        public static readonly char DirectorySeparatorChar = PathInternal.DirectorySeparatorChar;
        public static readonly char AltDirectorySeparatorChar = PathInternal.AltDirectorySeparatorChar;

        // Initial cross-platform length for a buffer that is to be passed to the ValueStringBuilder constructor.
        // The ValueStringBuilder will increase the internal buffer length when necessary.
        // The value is equivalent to PathInternal.MaxShortPath, a limitation that only exists in older Windows versions.
        private const int InitialValueStringBuilderBufferLength = 260;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinal(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length != value.Length)
                return false;
            if (value.Length == 0)  // span.Length == value.Length == 0
                return true;
            return span.SequenceEqual(value);
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0, "should have dealt with empty paths");

            bool firstHasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);
            bool thirdHasSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                || PathInternal.IsDirectorySeparator(third[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t = &MemoryMarshal.GetReference(third))
            {
#if MS_IO_REDIST
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    first.Length + second.Length + third.Length + (firstHasSeparator ? 0 : 1) + (thirdHasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        Third: (IntPtr)t, ThirdLength: third.Length, FirstHasSeparator: firstHasSeparator, ThirdHasSeparator: thirdHasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.FirstHasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.FirstHasSeparator ? 0 : 1)));
                        if (!state.ThirdHasSeparator)
                            destination[destination.Length - state.ThirdLength - 1] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Third, state.ThirdLength).CopyTo(destination.Slice(destination.Length - state.ThirdLength));
                    });
            }
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0 && fourth.Length > 0, "should have dealt with empty paths");

            bool firstHasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);
            bool thirdHasSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                || PathInternal.IsDirectorySeparator(third[0]);
            bool fourthHasSeparator = PathInternal.IsDirectorySeparator(third[third.Length - 1])
                || PathInternal.IsDirectorySeparator(fourth[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t = &MemoryMarshal.GetReference(third), u = &MemoryMarshal.GetReference(fourth))
            {
#if MS_IO_REDIST
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    first.Length + second.Length + third.Length + fourth.Length + (firstHasSeparator ? 0 : 1) + (thirdHasSeparator ? 0 : 1) + (fourthHasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        Third: (IntPtr)t, ThirdLength: third.Length, Fourth: (IntPtr)u, FourthLength: fourth.Length,
                        FirstHasSeparator: firstHasSeparator, ThirdHasSeparator: thirdHasSeparator, FourthHasSeparator: fourthHasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.FirstHasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.FirstHasSeparator ? 0 : 1)));
                        if (!state.ThirdHasSeparator)
                            destination[state.FirstLength + state.SecondLength + (state.FirstHasSeparator ? 0 : 1)] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Third, state.ThirdLength).CopyTo(destination.Slice(state.FirstLength + state.SecondLength + (state.FirstHasSeparator ? 0 : 1) + (state.ThirdHasSeparator ? 0 : 1)));
                        if (!state.FourthHasSeparator)
                            destination[destination.Length - state.FourthLength - 1] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Fourth, state.FourthLength).CopyTo(destination.Slice(destination.Length - state.FourthLength));
                    });
            }
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

            bool hasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                || PathInternal.IsDirectorySeparator(second[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second))
            {
#if MS_IO_REDIST
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    first.Length + second.Length + (hasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length, HasSeparator: hasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.HasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(destination.Slice(state.FirstLength + (state.HasSeparator ? 0 : 1)));
                    });
            }
        }

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
        {
            if (path1.Length == 0)
                return path2.ToString();
            if (path2.Length == 0)
                return path1.ToString();

            return JoinInternal(path1, path2);
        }

        private static string CombineInternal(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return second;

            if (string.IsNullOrEmpty(second))
                return first;

            if (IsPathRooted(second.AsSpan()))
                return second;

            return JoinInternal(first.AsSpan(), second.AsSpan());
        }

        private static string CombineInternal(string first, string second, string third)
        {
            if (string.IsNullOrEmpty(first))
                return CombineInternal(second, third);
            if (string.IsNullOrEmpty(second))
                return CombineInternal(first, third);
            if (string.IsNullOrEmpty(third))
                return CombineInternal(first, second);

            if (IsPathRooted(third.AsSpan()))
                return third;
            if (IsPathRooted(second.AsSpan()))
                return CombineInternal(second, third);

            return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan());
        }

        public static bool IsPathFullyQualified(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return IsPathFullyQualified(path.AsSpan());
        }

        public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
        {
            return !PathInternal.IsPartiallyQualified(path);
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : nameof(path2));

            return CombineInternal(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : nameof(path3));

            return CombineInternal(path1, path2, path3);
        }

        /// <summary>
        /// Removes redundant segments from the specified path string.
        /// </summary>
        /// <param name="path">The path to analyze.</param>
        /// <returns>A string without redundant segments.</returns>
        [return: NotNullIfNotNull("path")]
        public static string? RemoveRedundantSegments(string? path)
        {
            if (path == null)
            {
                return null;
            }

            var spanPath = path.AsSpan();
            if (PathInternal.IsEffectivelyEmpty(spanPath))
            {
                return string.Empty;
            }

            var sb = new ValueStringBuilder(spanPath.Length > InitialValueStringBuilderBufferLength? InitialValueStringBuilderBufferLength: spanPath.Length);

            if (!RedundantSegmentHelper.TryRemoveRedundantSegments(spanPath, ref sb))
            {
                sb.Dispose();
                return path;
            }

            return sb.ToString(); // Disposes
        }

        /// <summary>
        /// Removes redundant segments from the specified path read-only span.
        /// </summary>
        /// <param name="path">The path to analyze.</param>
        /// <returns>A string without redundant segments.</returns>
        public static string RemoveRedundantSegments(ReadOnlySpan<char> path)
        {
            if (PathInternal.IsEffectivelyEmpty(path))
            {
                return string.Empty;
            }

            var sb = new ValueStringBuilder(path.Length > InitialValueStringBuilderBufferLength ? InitialValueStringBuilderBufferLength : path.Length);

            if (!RedundantSegmentHelper.TryRemoveRedundantSegments(path, ref sb))
            {
                sb.Dispose();
                return path.ToString();
            }
            else
            {
                return sb.ToString(); // Disposes
            }
        }

        /// <summary>
        /// Tries to remove redundant segments from the specified path read-only span.
        /// </summary>
        /// <param name="path">The path to analyze.</param>
        /// <param name="destination">A span where the output is saved.</param>
        /// <param name="charsWritten">The total number of characters written to <paramref name="destination" />, which is less or equal than the length of <paramref name="path" />.</param>
        /// <returns><see langword="true" /> if the original path was modified and writing into <paramref name="destination" /> was successful; <see langword="false" /> otherwise.</returns>
        public static bool TryRemoveRedundantSegments(ReadOnlySpan<char> path, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;

            if (PathInternal.IsEffectivelyEmpty(path))
            {
                return false;
            }

            var sb = new ValueStringBuilder(path.Length > InitialValueStringBuilderBufferLength ? InitialValueStringBuilderBufferLength : path.Length);

            bool result = false;

            if (!RedundantSegmentHelper.TryRemoveRedundantSegments(path, ref sb))
            {
                if (path.TryCopyTo(destination))
                {
                    charsWritten = path.Length;
                    result = true;
                    sb.Dispose();
                }
            }
            else
            {
                result = sb.TryCopyTo(destination, out charsWritten); // Disposes
            }

            return result;
        }
    }
}
