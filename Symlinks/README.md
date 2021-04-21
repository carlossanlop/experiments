# Symbolic links 6.0

- [Epic](#epic)
- [Issues](#issues)
  * [P0](#P0)
    * [APIs for symbolic links](#apis-for-symbolic-links)
    * [Unix: FileSystemEntry.Attributes property incorrect](#filesystementry.attributes-property-incorrect-on-unix)
    * [Unix: FileSystemWatcher does not raise events when target directory is symlink](#25078)
  * [P1](#P1)
    * [Cannot change LastWriteTime or LastAccessTime of a symlink](#38824)
    * [APIs for canonical paths](#23871)
    * [APIs to get actual file casing in path](#14321)
    * [Tell if two filepaths point to the same file](#17873)
    * [Expose reparse point tags](#1908)
  * [P2](#P2)
    * [Mac: DirectoryInfo.EnumerateDirectories and DirectoryInfo.EnumerateFiles Method does not handle symbolic link very well](#34363)
    * [Linux: Junction does not work properly in NTFS volume](#20874)
    * [Unix: Broken symlinks and non-executable files not ignored in FindProgramInPath()](#29940)

# Epic

TODO: Create issue

# Issues


## P0

<h3 id="24271">APIs for symbolic links</h3>

https://github.com/dotnet/runtime/issues/24271

Main issue. Currently blocking partners and individuals.

The request is to add APIs that allow creating a symbolic link (file or directory) and, when a `FileInfo` or `DirectoryInfo` points to one, we also make sure to expose the real _target_ of the symbolic link.

The last API proposal by JeremyKuhne looks like this:

```cs
public class FileSystemInfo
{
    public bool IsSymbolicLink { get; }

    // The string value of the target for symbolic links
    public string SymbolicLinkTarget { get; }

    public void CreateSymbolicLink(string linkPath);
    public static FileSystemInfo CreateFromHandle(SafeFileHandle handle);
}

public class FileInfo
{
    public FileInfo(string fileName, FileSystemInfoFlags flags);

    // this pointer or FileInfo on the target if this info is a SymbolicLink
    public FileInfo TargetInfo { get; }
}

public class DirectoryInfo
{
    public DirectoryInfo(string fileName, FileSystemInfoFlags flags);

    // this pointer or DirectoryInfo on the target if this info is a SymbolicLink
    public DirectoryInfo TargetInfo { get; }
}

[Flags]
public enum FileSystemInfoFlags
{
    // The class will contain information about the final target of symbolic links
    FollowSymbolicLinks = 0x1;

    // Other options in future 
}
```

<h3 id="37301">Unix: FileSystemEntry.Attributes property incorrect</h3>

https://github.com/dotnet/runtime/issues/37301

Currently blocking [PowerShell PR](https://github.com/PowerShell/PowerShell/pull/12834).

On Unix, we have two places where we check for the attributes of a file: `FileSystemEntry` and `FileStatus`. They have duplicate code that can be merged into one.

Once merged, we can retrieve all flags: `ReadOnly`, `ReparsePoint`, `Directory`, `Hidden`.

I was working on this but found it difficult to get it fixed without affecting performance considerably, due to the additional `stat`|`lstat` calls required to retrieve the flags.

Once this gets fixed, other issues below can be fixed in a much simpler way.


<h3 id="25078">Unix: FileSystemWatcher does not raise events when target directory is symlink</h3>

https://github.com/dotnet/runtime/issues/25078

This issue is blocking internal partner.

Currently blocking: [#36091](#36091) (as described in [this comment](https://github.com/dotnet/runtime/issues/36091#issuecomment-704828472))

When a directory monitored by a `FileSystemWatcher` is a symbolic link, nothing happens.

There's a concern that we could enter an infinite loop if we decide to follow symlinks, so it's important to find a way to stop loops.

---

## P1

<h3 id="38824">Cannot change LastWriteTime or LastAccessTime of a symlink</h3>

https://github.com/dotnet/runtime/issues/38824

The `Last*Time` attributes can be changed on a normal file, but not on a symbolic link file.

`hamarb123` is fixing it with [this PR](https://github.com/dotnet/runtime/pull/49555), which also addresses issue [#39132](https://github.com/dotnet/runtime/issues/39132) (not symlink related).



<h3 id="23871">APIs for canonical paths</h3>

https://github.com/dotnet/runtime/issues/23871

Description of a canonical path:
- Is a fully qualified path path
- Uses only `DirectorySeparatorChar` as a directory separator
- Has no trailing directory separator (unless it is a/the root)
- Contains no navigation elements (. and ..) and no empty path elements (ex. /foo//bar)
- Contains no symbolic links (unless otherwise specified)
- Has its root in a canonical form (ex. on Windows, drive letters will be upper case), and
- Adopts the actual casing of the file and directory names if the file and directory names are case-insensitive

Current proposal looks like:
```cs
public static class Path
{
    public static string GetCanonicalPath(string path);  // Resolves symbolic links
    public static string GetCanonicalPath(string path, bool preserveSymbolicLinks);
}
```

But there's an opportunity to combine this proposal with [#14321](#14321) and [#17873](#17873) below.



<h3 id="14321">APIs to get actual file casing in path</h3>

https://github.com/dotnet/runtime/issues/14321

Since NTFS is case-insensitive, a `File` or `FileInfo` can be created using the wrong casing, but will successfully resolve to the actual file. The `FullName` does not return the correct casing.

The proposal currently looks like:

```cs
public static class Path
{
    public static string GetRealPath(string path);
    public static string GetRealPath(ReadOnlySpan<char> path);
    public static string GetRealPath(ReadOnlyMemory<char> path);
}
```
Which would do nothing on case-sensitive file systems like Unix (would just return the actual path).

PowerShell had to resort to a workaround: https://github.com/PowerShell/PowerShell/issues/13190

StackOverflow:
- [C# Getting actual file name (with proper casing) on Windows with .NET](https://stackoverflow.com/questions/325931/getting-actual-file-name-with-proper-casing-on-windows-with-net/326153#326153)
- [C++ Getting actual file name (with proper casing) on Windows](https://stackoverflow.com/questions/74451/getting-actual-file-name-with-proper-casing-on-windows/81493#81493)



<h3 id="17873">Tell if two filepaths point to the same file</h3>

https://github.com/dotnet/runtime/issues/17873

Summary: Need a cross-platform way to determine if two hard links point to the same file handle (Windows) or file descriptor (Unix).

The current proposal looks like:

```cs
public static class Directory
{
    // canonical path but on Directory
    public static string GetRealPath(string path);
    // resolves symlink and performs equality check
    public static bool HaveSameTarget(string path1, string path2);
}

public static class File
{
    // canonical path but on File
    public static string GetRealPath(string path);
    // resolves symlink and performs equality check
    public static bool HaveSameTarget(string path1, string path2);
}
```

But `HaveSameTarget` might look better inside `Path` as an independent, static method.

On Windows, a combination of the P/Invokes to `GetFileInformationByHandle` and `CreateFile` could solve the issue (see the StackOverflow link below).

On Linux, a call to [`realpath`](https://man7.org/linux/man-pages/man3/realpath.3.html) might be necessary to resolve all symbolic links (retrieve canonical paths).

Keep in mind that retrieving canonical paths is another API request: [#23871](#23871).

Other languages have resolved it already:

- Java https://docs.oracle.com/javase/7/docs/api/java/nio/file/Files.html#isSameFile(java.nio.file.Path,%20java.nio.file.Path)
- PHP http://php.net/manual/en/function.realpath.php
- Perl http://perldoc.perl.org/Cwd.html
- Python https://docs.python.org/2/library/os.path.html#os.path.realpath
- Ruby http://apidock.com/ruby/Pathname/realpath
- node.js https://nodejs.org/api/fs.html#fs_fs_realpath_path_options_callback

StackOverflow:
- [Best way to determine if two path reference to same file in C#](https://stackoverflow.com/a/31815059/8816314)



<h3 id="1908">Expose reparse point tags</h3>

https://github.com/dotnet/runtime/issues/1908

When we detect a file as a reparse point, it could be a symbolic link, or a OneDrive cloud file, or an AppX file.

The request is to do enumeration or deletion more carefully, and detecting which of the 3 types the reparse point is. For example, a OneDrive cloud file will unexpectedly get downloaded.

---

## P2

Issues that can be marked as future, or could potentially be indirectly fixed by addressing issues from above.


<h3 id="34363">Mac: DirectoryInfo.EnumerateDirectories and DirectoryInfo.EnumerateFiles Method does not handle symbolic link very well</h3>

https://github.com/dotnet/runtime/issues/34363

On Mac only, symbolic links can cause infinite recursion when iterating through subdirectories and files. This needs to be confirmed. No repro code or callstack was offered.

This could potentially go away after solving the higher pri issues.



<h3 id="20874">Linux: Junction does not work properly in NTFS volume</h3>

https://github.com/dotnet/runtime/issues/20874

Closely related to [#1908](#1908) and both can potentially be fixed together.

ReparsePoint is a file attribute that we are currently not including in DirectoryInfo.Attributes, causing files to not show up correctly when querying an NTFS filesystem from Linux.



<h3 id="29940">Unix: Broken symlinks and non-executable files not ignored in FindProgramInPath()</h3>

https://github.com/dotnet/runtime/issues/29940

On Unix, when attempting to execute a process, if a broken symlink is found when searching for a binary among the potential candidates in $PATH, an exception is thrown. Other solutions would ignore the broken symlink and would continue searching for a valid candidate for that executable.

The user gave a good description of the problem and the root cause.
