# Issues

### API for symbolic links
https://github.com/dotnet/runtime/issues/24271

### FileSystemEntry.Attributes property is not correct on Unix
https://github.com/dotnet/runtime/issues/37301

### Expose reparse point tags
https://github.com/dotnet/runtime/issues/1908

### FileSystemWatcher does not raise events when target directory is symlink (on linux)
https://github.com/dotnet/runtime/issues/25078
Fixing this would unblock: https://github.com/dotnet/runtime/issues/36091#issuecomment-704828472

### Mac SDK: DirectoryInfo.EnumerateDirectories and DirectoryInfo.EnumerateFiles Method does not handle symbolic link very well
https://github.com/dotnet/runtime/issues/34363

### Cannot change LastWriteTime or LastAccessTime of a symlink
https://github.com/dotnet/runtime/issues/38824
Keep this in mind when fixing this: https://github.com/dotnet/runtime/issues/39132

### NTFS volume on Linux - junction does not work properly
https://github.com/dotnet/runtime/issues/20874

### broken symlinks and non-executable files not ignored in FindProgramInPath()
https://github.com/dotnet/runtime/issues/29940

### A way to tell if two filepaths point to the same file
https://github.com/dotnet/runtime/issues/17873
