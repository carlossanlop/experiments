<details>

<summary>Proposal</summary>

```cs
public abstract class FileSystemInfo
{
    public void CreateAsSymbolicLink(string pathToTarget);
    FileSystemInfo? GetTargetInfo(bool returnFinalTarget = true);
}

//// Future additional APIs:
// public class DirectoryInfo
// {
//     public void CreateJunction(string targetPath);
// }

// public class FileInfo
// {
//     public void CreateHardLink(string targetPath);
// }
```

</details>

<details>

<summary>Usage cases</summary>

```cs
/////////////////////////
//// Symlink to a file

// link2a \
//         ---> link1 -> file.txt
// link2b /

var file = new FileInfo("/path/file.txt");
file.Create().Dispose();

var link1 = new FileInfo("/path/link1");
link1.CreateAsSymbolicLink(file.FullPath);

FileSystemInfo target1 = link1.GetTargetInfo();
Console.WriteLine(target1.FullPath); // /path/file.txt

var link2a = new FileInfo("/path/link2a");
link2a.CreateAsSymbolicLink(link1.FullPath);

// By default skips links in between and returns final target.
FileSystemInfo target2a = link2a.GetTargetInfo();
Console.WriteLine(target2a.FullPath); // /path/file.txt

var link2b = new FileInfo("/path/link2b");
link2b.CreateAsSymbolicLink(targetPath: link1.FullPath);

// Won't skip link1, will return it as the target.
FileSystemInfo target2b = link2b.GetTargetInfo(returnFinalTarget: false);
Console.WriteLine(target2b.FullPath); // /path/link1


/////////////////////////
//// Symlink to a directory

// link2a \
//         ---> link1 -> directory
// link2b /

var directory = new DirectoryInfo("/path/directory");
directory.Create();

// The symlink itself needs to be represented with a DirectoryInfo instance
// because Windows cares about the underlying type
var link1 = new DirectoryInfo("/path/link1");
link1.CreateAsSymbolicLink(targetPath: directory.FullPath); 

// No need to follow to final target, it's direct
FileSystemInfo target1 = link2b.GetTargetInfo();
Console.WriteLine(target1.FullPath); // /path/directory

var link2a = new DirectoryInfo("/path/link2a");
link2a.CreateAsSymbolicLink(link1.FullPath);

// Skips link1 and returns final target
FileSystemInfo target2a = link2a.GetTargetInfo();
Console.WriteLine(target2a.FullPath); // /path/directory

var link2b = new DirectoryInfo("/path/link2b");

// Won't skip link1, will return link1 as the target
link2b.CreateAsSymbolicLink(link1.FullPath);
FileSystemInfo target2b = link2b.GetTargetInfo(returnFinalTarget: false);
Console.WriteLine(target2b.FullPath); // /path/link1


/////////////////////////
//// Non-existent target

var link1 = new FileInfo("/path/link1");
// Should succeed to create symlink file, even though target does not exist
link1.CreateAsSymbolicLink("/non/existent/file.txt");
FileSystemInfo target1 = link1.GetTargetInfo();
Console.WriteLine(target1.FullPath); // Should print /non/existent/file.txt

var link2 = new FileInfo("/path/link2");
link2.CreateAsSymbolicLink(targetPath: link2.FullPath); // skips link1
// Follows symlinks and stops at file.txt, even if it does not exist
FileSystemInfo target2 = link2.GetTargetInfo();
Console.WriteLine(target2.FullPath); // Should print /non/existent/file.txt



/////////////////////////
//// Existing symlink

var directory = new DirectoryInfo("/path/directory");
directory.Create();

var link = new DirectoryInfo("/path/link");
Link.CreateSymbolicLink(targetPath: directory.FullPath);

// This DirectoryInfo wraps the symlink that was created above
// so we should return a valid TargetInfo when requested
var existingLink = new DirectoryInfo("/path/link");
FileSystemInfo existingTarget = existingLink.GetTargetInfo();
Console.WriteLine(existingTarget.FullPath); // Should print /path/directory


/////////////////////////
// Inconsistent symlink target and *Info type

var directory = new DirectoryInfo("/path/directory");
directory.Create();

// The user should've used DirectoryInfo to wrap the link to a directory
var link = new FileInfo("/path/link");
Link.CreateSymbolicLink(targetPath: directory.FullPath); // Should throw because target is a directory


/////////////////////////
// Circular reference

var link1 = new FileInfo("/path/link1");
link1.CreateAsSymbolicLink(targetPath: "/path/link2");
var link2 = new FileInfo("/path/link2");
link2.CreateAsSymbolicLink(targetPath: "/path/link3");
var link3 = new FileInfo("/path/link3");
link3.CreateAsSymbolicLink(targetPath: "/path/link1");

// Throws because we opted-in to follow symlinks and there is a cycle.
// and a circular reference is found on link3 to link1
FileSystemInfo target3 = link3.GetTargetInfo(followLinks: true);


/////////////////////////
// Recursive enumeration directory with symlinks

// directory
// - subdirectory1
//    - file.txt
//    - symlink1 -> file.txt
// - subdirectory2
//    - symlink2 -> symlink1

FileSystemEnumerable<FileSystemInfo>.FindTransform transform =
    (ref FileSystemEntry entry) => entry.ToFileSystemInfo();

EnumerationOptions options = new EnumerationOptions
{
    RecurseSubdirectories = true
};

var enumerable = new FileSystemEnumerable<FileSystemInfo>(@"/path/to/directory", transform, options)
{
    ShouldRecursePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
};

foreach (FileSystemInfo info in enumerable)
{
    // No need to add an API to signal that a FSI is Symbolic Link.
    // Warning: ReparsePoint is not exclusive of Symbolic Links.
    string path = info.Attributes.HasFlag(FileAttributes.ReparsePoint) ? 
        info.GetTargetInfo(followLinks: true).FullPath : // Follows symlink to final target
        info.FullPath;
    Console.WriteLine(path);
}
```

</details>

<details>

<summary>Alternative design</summary>

```cs

// New
public enum FileType
{
    Regular, // Alt-name: Hard-link. Used exclusively with FileInfo.
    SymbolicLink,
    Directory, // Used exclusively with a DirectoryInfo.

    //// Future enum values:

    // Junction, // Windows only
    // Pipe, // Unix
    // Block, // Unix
    // Character, // Unix
    // Socket, // Unix
}

public abstract class FileSystemInfo
{
    FileType FileType { get; }
    FileSystemInfo? GetTargetInfo(bool returnFinalTarget = true); // Final target should be default
}

public static class File
{
    static FileInfo CreateSymbolicLink(string path, string targetPath);

    //// Future additional APIs:
    // static FileInfo CreateHardLink(string path, string targetPath);
}

public static class Directory
{

    static DirectoryInfo CreateSymbolicLink(string path, string targetPath);

    //// Future additional APIs:
    // static DirectoryInfo CreateJunction(string path, string targetPath);
}
```

</details>

<details>

<summary>Alternative design usage cases</summary>

```cs
/////////////////////////
//// Symlink to a file

// link2a \
//         ---> link1 -> file.txt
// link2b /

var file = new FileInfo("/path/file.txt");
file.Create().Dispose();

FileInfo link1 = File.CreateSymbolicLink(path: "/path/link1", targetPath: file.FullPath) as FileInfo;

FileSystemInfo target1 = link1.GetTargetInfo();
Console.WriteLine(target1.FullPath); // /path/file.txt

FileInfo link2a = File.CreateSymbolicLink(path: "/path/link2a", targetPath: link1.FullPath);
// Skips link1 and returns final target
FileSystemInfo target2a = link2a.GetTargetInfo();
Console.WriteLine(target2a.FullPath); // /path/file.txt

FileInfo link2b = File.CreateSymbolicLink(path: "/path/link2b", targetPath: link1.FullPath);
// Won't skip link1, will return link1 as the target
FileSystemInfo target2b = link2b.GetTargetInfo(returnFinalTarget: false);
Console.WriteLine(target2b.FullPath); // /path/link1, instead of /path/file.txt


/////////////////////////
//// Symlink to a directory

// link2a \
//         ---> link1 -> directory
// link2b /

var directory = new DirectoryInfo("/path/directory");
directory.Create();

// The symlink itself needs to be represented with a DirectoryInfo instance
// because Windows cares about the underlying type
DirectoryInfo link1 = Directory.CreateSymbolicLink(path: "/path/link1", targetPath: directory.FullPath); 

FileSystemInfo target1 = link2b.GetTargetInfo();
Console.WriteLine(target1.FullPath); // /path/directory

DirectoryInfo link2a = Directory.CreateSymbolicLink(path: "/path/link2a", targetPath: link1.FullPath);
// Skips link1 and returns final target
FileSystemInfo target2a = link2a.GetTargetInfo();
Console.WriteLine(target2a.FullPath); // /path/directory

DirectoryInfo link2b = Directory.CreateSymbolicLink(path: "/path/link2b", targetPath: link1.FullPath);
// Won't skip link1, will return link1 as the target
FileSystemInfo target2b = link2b.GetTargetInfo(returnFinalTarget: false);
Console.WriteLine(target2b.FullPath); // /path/link1
```
</details>


---

Need to create new APIs that allow creating symbolic links, but can be easily expanded to create hard links, junctions, etc.

Need to add support for enumeration filtering of symbolic links. Ideally, only expand the FileSystemEnumerable APIs.

On Windows, there's differentiation between a link to a dir and a link to a file. On Unix, there isn't.

The APIs must create a symbolic link and represent it with the *Info instance, and have another API that provides a reference to the target.

Need to support following the symlinks up to its final target, when they are chained, and make sure no cycles are found.

Windows types of links

https://cects.com/overview-to-understanding-hard-links-junction-points-and-symbolic-links-in-windows/

- Hard link: a link to a local file/directory handle. Deleting the hard link deletes the target. If target is moved, hard link stays valid.
-  Junction: a legacy symbolic link that targets the absolute path of a local directory. Deleting the junction does not delete the directory. If target is moved, junction turns invalid. Drag and dropping the junction to another location, moves the actual directory to that location, without deleting the original directory (but will now be empty).
- Symbolic link: a link that targets the relative or absolute path of a local or remote file or directory. Deleting the symbolic link does not delete the target. Moving the target turns the symbolic link invalid.
- App execution aliases: a new special kind of symbolic link with a custom reparse point that allows targeting a Windows Store app via a "fake" executable that weighs 0 bytes, which is linked to the real file.
        https://www.tiraniddo.dev/2019/09/overview-of-windows-execution-aliases.html
        https://stackoverflow.com/questions/62474046/how-do-i-find-the-target-of-a-windows-app-execution-alias-in-c-win32-api


Windows links to folders:
- Junction
- Symbolic link

Windows links to files:
- Hard link
- Symbolic link
- App execution aliases

