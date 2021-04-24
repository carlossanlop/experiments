
Need to create new APIs that allow creating symbolic links, but can be easily expanded to create hard links, junctions, etc.

Need to add support for enumeration filtering of symbolic links. Ideally, only expand the FileSystemEnumerable APIs.

On Windows, there's differentiation between a link to a dir and a link to a file. On Unix, there isn't.

The APIs must create a symbolic link and represent it with the *Info instance, and have another API that provides a reference to the target.

Need to support following the symlinks up to its final target, when they are chained, and make sure no cycles are found.

Windows types of links

https://cects.com/overview-to-understanding-hard-links-junction-points-and-symbolic-links-in-windows/

- *.lnk shortcut: a link to an absolute path of a local or remote file or directory. Deleting the link does not delete the target. If the target is moved, the shortcut turns invalid.
- Hard link: a link to a local file/directory handle. Deleting the hard link deletes the target. If target is moved, hard link stays valid.
- Junction: a legacy symbolic link that targets the absolute path of a local directory. Deleting the junction does not delete the directory. If target is moved, junction turns invalid. Drag and dropping the junction to another location, moves the actual directory to that location, without deleting the original directory (but will now be empty).
- Symbolic link: a link that targets the relative or absolute path of a local or remote file or directory. Deleting the symbolic link does not delete the target. Moving the target turns the symbolic link invalid.
- App execution aliases: a new special kind of symbolic link with a custom reparse point that allows targeting a Windows Store app via a "fake" executable that weighs 0 bytes, which is linked to the real file.
        https://www.tiraniddo.dev/2019/09/overview-of-windows-execution-aliases.html
        https://stackoverflow.com/questions/62474046/how-do-i-find-the-target-of-a-windows-app-execution-alias-in-c-win32-api


Windows links to folders:
- *.lnk shortcut
- Junction
- Symbolic link

Windows links to files:
- *.lnk shortcut
- Hard link
- Symbolic link
- App execution aliases

```cs
using System;

public abstract class FileSystemInfo
{
    public bool IsSymbolicLink { get; }
    public void CreateSymbolicLink(string targetPath) { }
}

public class FileInfo : FileSystemInfo
{
    public FileInfo(string filePath, FileSystemInfoFlags flags) { }
    // An info that points to the symlink target, or null if this info is not a symlink
    public FileInfo? TargetInfo { get; }
}

public class DirectoryInfo : FileSystemInfo
{
    // An info that points to the symlink target, or null if this info is not a symlink
    public DirectoryInfo? TargetInfo { get; }
}

[Flags]
public enum FileSystemInfoFlags
{
    // The *Info instance will contain information about the final target of symbolic links
    FollowSymbolicLinks = 0x1
    // Other options in the future
}

class Program
{
    static void Main()
    {
        // Usage case 1: File symbolic links

        // link2a \
        //         ---> link1 -> file.txt
        // link2b /

        var file = new FileInfo("/path/file.txt");
        file.Create().Dispose();

        var link1 = new FileInfo("/path/link1"); // no need to follow symlink to final target, it's direct
        link1.CreateSymbolicLink(targetPath: file.FullPath);
        Console.WriteLine(link1.TargetInfo.FullPath); // /path/file.txt

        var link2a = new FileInfo("/path/link2a", FileSystemInfoFlags.FollowSymbolicLinks); // skips link1 and returns file.txt as the target
        link2a.CreateSymbolicLink(targetPath: link1.FullPath);
        Console.WriteLine(link2a.TargetInfo.FullPath); // /path/file.txt

        var link2b = new FileInfo("/path/link2b"); // Won't skip link1, will return link1 as the target
        link2b.CreateSymbolicLink(targetPath: link1.FullPath);
        Console.WriteLine(link2b.TargetInfo.FullPath); // /path/link1 , instead of /path/file.txt


        // Usage case 2: Directory symbolic links

        // link2 -> link1 -> directory

        var directory = new DirectoryInfo("/path/directory");
        directory.Create();

        var link1 = new DirectoryInfo("/path/link1"); // the link itself is a DirectoryInfo...
        link1.CreateSymbolicLink(targetPath: directory.FullPath); // so that CreateSymbolicLink knows it needs to create a link to a directory (Windows), doesn't care in Unix
        Console.WriteLine(link1.TargetInfo.FullPath); // /path/directory

        var link2 = new DirectoryInfo("/path/link2a", FileSystemInfoFlags.FollowSymbolicLinks); // skips link1 and returns file.txt as the target
        link2.CreateSymbolicLink(targetPath: link1.FullPath);
        Console.WriteLine(link2.TargetInfo.FullPath); // /path/directory


        // Usage case 3: Non-existent target

        var file = new FileInfo("/non/existent/file.txt");

        var link1 = new FileInfo("/path/link1");
        link1.CreateSymbolicLink(targetPath: file.FullPath); // Should succeed to create symlink file, even though target does not exist
        Console.WriteLine(link1.TargetInfo.FullPath); // Should print /non/existent/file.txt

        var link2 = new FileInfo("/path/link2", FileSystemInfoFlags.FollowSymbolicLinks);
        link2.CreateSymbolicLink(targetPath: link2.FullPath); // skips link1
        Console.WriteLine(link2.TargetInfo.FullPath); // Should print /non/existent/file.txt
        // Stops at file.txt because there was nothing else to follow


        // Usage case 4: Create info for existing symlink

        var directory = new DirectoryInfo("/path/directory");
        directory.Create();

        var link = new DirectoryInfo("/path/link");
        link.CreateSymbolicLink(targetPath: directory.FullPath);

        var existingLink = new DirectoryInfo("/path/link"); // It was created above
        Console.WriteLine(existingLink.TargetInfo.FullPath); // Should print /path/directory


        // Usage case 5: Inconsistent symlink target and *Info type

        var directory = new DirectoryInfo("/path/directory");
        directory.Create();

        var link = new FileInfo("/path/link", FileSystemInfoFlags.FollowSymbolicLinks); // Should've been a DirectoryInfo
        link.CreateSymbolicLink(targetPath: directory.FullPath); // should throw because target is a directory


        // Usage case 6: Circular reference

        var link1 = new FileInfo("/path/link1", FileSystemInfoFlags.FollowSymbolicLinks);
        link1.CreateSymbolicLink(targetPath: "/path/link2");
        var link2 = new FileInfo("/path/link2", FileSystemInfoFlags.FollowSymbolicLinks);
        link2.CreateSymbolicLink(targetPath: "/path/link3");
        var link3 = new FileInfo("/path/link3", FileSystemInfoFlags.FollowSymbolicLinks);
        link3.CreateSymbolicLink(targetPath: "/path/link1"); // Should throw due to circular reference found on link3 target


        // Usage case 7: Recursive enumeration of the contents of a directory symlink already works well with FileSystemEnumerable

        string directory = @"D:\symlinktofolder"; // Symlink that points to another folder with many subfolders
        FileSystemEnumerable<FileSystemInfo>.FindTransform transform = (ref FileSystemEntry entry) => entry.ToFileSystemInfo();
        EnumerationOptions options = new EnumerationOptions { RecurseSubdirectories = true };
        FileSystemEnumerable<FileSystemInfo> enumerable = new(directory, transform, options) { ShouldRecursePredicate = (ref FileSystemEntry entry) => entry.IsDirectory };

        foreach (FileSystemInfo info in enumerable)
        {
            Console.WriteLine(info); // This will print all the subolders inside the target symlink
        }

    }
}

// Future designs: Junctions
public class DirectoryInfo
{
   // Windows only - Junctions only apply to directories
   public bool IsJunction { get; }

   // Windows only - Junctions only apply to directories
   public void CreateJunction(string targetPath) { }
}

// Future designs: App Execution Aliases 
// IO_REPARSE_TAG_APPEXECLINK
// These are symbolic links with a custom reparse point
public class FileInfo
{
   public bool IsExecutionAlias { get; }
} 
```