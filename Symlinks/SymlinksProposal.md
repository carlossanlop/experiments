# Summary

The ability to interact with symbolic links in .NET is currently limited to determining that a file has the `ReparsePoint` attribute, but we do not yet offer APIs for creating symbolic links, or for accessing the linked file or directory.

# Proposed APIs

```cs
public abstract class FileSystemInfo
{
    public void CreateAsSymbolicLink(string pathToTarget);
    FileSystemInfo? GetTargetInfo(bool returnFinalTarget = true); // Final target should be default
}
```

## Descriptions

### Creating a symbolic link

```cs
void CreateAsSymbolicLink(string pathToTarget)
```

This method should be declared in the abstract class `FileSystemInfo`, so that the deriving classes `FileInfo` and `DirectoryInfo` implement their specific versions.

We decided to take this approach for the following reasons:

- We need to support the most restrictive OS (Windows), without affecting the least restrictive (Unix): Windows has two types of symbolic links (files and directories), and Unix only one type (independent of what it is pointing to).
- Since C# is a strongly typed language, it makes sense to know in advance if a symbolic link we will create, will point to either a file or a directory.
- The `FileInfo` and `DirectoryInfo` constructors can be called passing a path to a file or folder that does not yet exist, but we would be able to create a symbolic link to an inexistent file/folder and the creation should succeed (even if the symlink is invalid).
- The parameter name clearly indicates we need to pass the path of the existing file or folder the symbolic link should point to.
- The method does not need to return anything, which is analogous to [`DirectoryInfo.Create`](https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/System.IO.FileSystem/src/System/IO/DirectoryInfo.cs#L93) and [`FileInfo.Create`](https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/System.IO.FileSystem/src/System/IO/FileInfo.cs#L102).

### Retrieving the target

```cs
FileSystemInfo? GetTargetInfo(bool returnFinalTarget = true); // Final target should be default
```

Similarly to the creation method, we think this method should live in the abstract class `FileSystemInfo`, and the deriving classes `FileInfo` and `DirectoryInfo` would implement their specific versions.

We decided to take this approach for the following reasons:

- One of the main requests in this discussion is to give the user the ability to retrieve the symbolic link target.
- Having a boolean parameter `returnFinalTarget` ensures the user can decide if they want to retrieve the object the current symbolic link is pointing to, or in the case of a chain of symbolic links, the final target of the chain. We think the latter is the most common scenario, which is why the default value of the parameter is `true`.
- The method returns a nullable `FileSystemInfo` because only `FileInfo` and `DirectoryInfo` instances wrapping a symbolic link should return a valid instance.
- The user can verify if the current `FileSystemInfo` instance is a symbolic link, by calling `FileSystemInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)`.
- Having the method in the abstract class will make sure enumeration with `FileSystemEnumerable` works with the public method `FileSystemEntry.ToFileSystemInfo()`, which would normally be used in the `FindTransform` method (see use case below).
- This method would resolve the desired target by making the actual disk calls that verify if the file exists, or if there are cycles in the symbolic links.

### Future expansions

Although they are outside of the scope of this API proposal, we wanted to make sure we could easily expand `FileSystemInfo` to support the creation of junctions and hard links:

```cs
public class DirectoryInfo
{
    // Can reuse `GetTargetInfo` to retrieve the junction target
    public void CreateJunction(string pathToTarget);
}

public class FileInfo
{
    public void CreateHardLink(string pathToTarget);
}
```


## Usage cases

<details>

<summary>Expand me</summary>

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

---

## Alternative/additional designs

As initially proposed in this discussion, we also made sure to consider the expansion of the existing `File` and `Directory` static classes, with some differences.

These additional APIs could be considered alternative or additional to the proposed above.

```cs
public static class File
{
    static FileInfo CreateSymbolicLink(string path, string pathToTarget);
}

public static class Directory
{

    static DirectoryInfo CreateSymbolicLink(string path, string pathToTarget);
}

public abstract class FileSystemInfo
{
    // Same API described in the main proposal
    FileSystemInfo? GetTargetInfo(bool returnFinalTarget = true);
}
```

## Descriptions

### Creating a file symbolic link

```cs
static FileInfo CreateSymbolicLink(string path, string pathToTarget);
```

The structure is similar to other [creation methods](https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/System.IO.FileSystem/src/System/IO/File.cs) in the `File` class.

Just like in the main proposal, we need to make sure to properly support the most restrictive OS, Windows, which differentiates between a file symlink and a directory symlink.

### Creating a directory symbolic link

```cs
static DirectoryInfo CreateSymbolicLink(string path, string pathToTarget);
```

Similar to the previous case, and has parity to the [creation methods](https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/System.IO.FileSystem/src/System/IO/Directory.cs) in the static `Directory` class.

### Future expansions

Similarly to the main proposal, we made sure to keep in mind the potential future addition of junction and hard link creation support.

```cs
public static class File
{
    // Future
    static FileInfo CreateHardLink(string path, string pathToTarget);
}

public static class Directory
{
    // Future
    // The user could consume `FileSystemInfo.GetTargetInfo` to retrieve the junction target
    static DirectoryInfo CreateJunction(string path, string pathToTarget);
}
```

## Alternative design usage cases

<details>

<summary>Expand me</summary>

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







