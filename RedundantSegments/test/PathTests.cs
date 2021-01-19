
#nullable enable
using System;
using PathInternal = RedundantSegments.MyPathInternal;
using Path = RedundantSegments.MyPath;
using ValueStringBuilder = RedundantSegments.ValueStringBuilder;
using System.Diagnostics;
using Xunit;
using System.Collections.Generic;

namespace test
{
    public class PathTests_Windows : PathTestsBase
    {
        public static TheoryData<string, string, string> GetFullPath_Windows_FullyQualified => new TheoryData<string, string, string>
        {
            { @"C:\git\runtime",                         @"C:\git\runtime", @"C:\git\runtime" },
            { @"C:\git\runtime.\.\.\.\.\.",              @"C:\git\runtime", @"C:\git\runtime" },
            { @"C:\git\runtime\\\.",                     @"C:\git\runtime", @"C:\git\runtime" },
            { @"C:\git\runtime\..\runtime\.\..\runtime", @"C:\git\runtime", @"C:\git\runtime" },
            { @"C:\somedir\..",            @"C:\git\runtime", @"C:\" },
            { @"C:\",                      @"C:\git\runtime", @"C:\" },
            { @"..\..\..\..",              @"C:\git\runtime", @"C:\" },
            { @"C:\\\",                    @"C:\git\runtime", @"C:\" },
            { @"C:\..\..\",                @"C:\git\runtime", @"C:\" },
            { @"C:\..\git\..\.\",          @"C:\git\runtime", @"C:\" },
            { @"C:\git\runtime\..\..\..\", @"C:\git\runtime", @"C:\" },
            { @"C:\.\runtime\",            @"C:\git\runtime", @"C:\runtime\" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_Windows_FullyQualified))]
        public void GetFullPath_BasicExpansions_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_PathIsDevicePath => new TheoryData<string, string, string>
        {
            // Device Paths with \\?\ wont get normalized i.e. relative segments wont get removed.
            { @"\\?\C:\git\runtime.\.\.\.\.\.",              @"C:\git\runtime", @"\\?\C:\git\runtime.\.\.\.\.\." },
            { @"\\?\C:\git\runtime\\\.",                     @"C:\git\runtime", @"\\?\C:\git\runtime\\\." },
            { @"\\?\C:\git\runtime\..\runtime\.\..\runtime", @"C:\git\runtime", @"\\?\C:\git\runtime\..\runtime\.\..\runtime" },
            { @"\\?\\somedir\..", @"C:\git\runtime", @"\\?\\somedir\.." },
            { @"\\?\",            @"C:\git\runtime", @"\\?\" },
            { @"\\?\..\..\..\..", @"C:\git\runtime", @"\\?\..\..\..\.." },
            { @"\\?\\\\" ,        @"C:\git\runtime", @"\\?\\\\" },
            { @"\\?\C:\Foo." ,    @"C:\git\runtime", @"\\?\C:\Foo." },
            { @"\\?\C:\Foo " ,    @"C:\git\runtime", @"\\?\C:\Foo " },

            { @"\\.\C:\git\runtime.\.\.\.\.\.",              @"C:\git\runtime", @"\\.\C:\git\runtime" },
            { @"\\.\C:\git\runtime\\\.",                     @"C:\git\runtime", @"\\.\C:\git\runtime" },
            { @"\\.\C:\git\runtime\..\runtime\.\..\runtime", @"C:\git\runtime", @"\\.\C:\git\runtime" },
            { @"\\.\\somedir\..", @"C:\git\runtime", @"\\.\" },
            { @"\\.\",            @"C:\git\runtime", @"\\.\" },
            { @"\\.\..\..\..\..", @"C:\git\runtime", @"\\.\" },
            { @"\\.\",            @"C:\git\runtime", @"\\.\" },
            { @"\\.\C:\Foo." ,    @"C:\git\runtime", @"\\.\C:\Foo" },
            { @"\\.\C:\Foo " ,    @"C:\git\runtime", @"\\.\C:\Foo" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_Windows_PathIsDevicePath))]
        public void GetFullPath_BasicExpansions_Windows_PathIsDevicePath(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
            Assert.Equal(expected, Path.GetFullPath(path, @"\\.\" + basePath));

            // Paths starting with \\?\ are considered normalized and should not get modified
            Assert.Equal(expected, Path.GetFullPath(path, @"\\?\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_UNC => new TheoryData<string, string, string>
        {
            { @"foo",    @"",              @"foo" },
            { @"foo",    @"server1",       @"server1\foo" },
            { @"\foo",   @"server2",       @"server2\foo" },
            { @"foo",    @"server3\",      @"server3\foo" },
            { @"..\foo", @"server4",       @"server4\..\foo" },
            { @".\foo",  @"server5\share", @"server5\share\foo" },
            { @"..\foo", @"server6\share", @"server6\share\foo" },
            { @"\foo",   @"a\b\\",         @"a\b\foo" },
            { @"foo",      @"LOCALHOST\share8\test.txt.~SS", @"LOCALHOST\share8\test.txt.~SS\foo" },
            { @"foo",      @"LOCALHOST\share9",              @"LOCALHOST\share9\foo" },
            { @"foo",      @"LOCALHOST\shareA\dir",          @"LOCALHOST\shareA\dir\foo" },
            { @". \foo",   @"LOCALHOST\shareB\",             @"LOCALHOST\shareB\. \foo" },
            { @".. \foo",  @"LOCALHOST\shareC\",             @"LOCALHOST\shareC\.. \foo" },
            { @"    \foo", @"LOCALHOST\shareD\",             @"LOCALHOST\shareD\    \foo" },

            { "foo", @"LOCALHOST\  shareE\",           @"LOCALHOST\  shareE\foo" },
            { "foo", @"LOCALHOST\shareF\test.txt.~SS", @"LOCALHOST\shareF\test.txt.~SS\foo" },
            { "foo", @"LOCALHOST\shareG",              @"LOCALHOST\shareG\foo" },
            { "foo", @"LOCALHOST\shareH\dir",          @"LOCALHOST\shareH\dir\foo" },
            { "foo", @"LOCALHOST\shareK\",             @"LOCALHOST\shareK\foo" },
            { "foo", @"LOCALHOST\  shareL\",           @"LOCALHOST\  shareL\foo" },

            // Relative segments eating into the root
            { @".\..\foo\..\",               @"server\share", @"server\share\" },
            { @"..\foo\tmp\..\..\",          @"server\share", @"server\share\" },
            { @"..\..\..\foo",               @"server\share", @"server\share\foo" },
            { @"..\foo\..\..\tmp",           @"server\share", @"server\share\tmp" },
            { @"..\foo",                     @"server\share", @"server\share\foo" },
            { @"...\\foo",                   @"server\share", @"server\share\...\foo" },
            { @"...\..\.\foo",               @"server\share", @"server\share\foo" },
            { @"..\foo\tmp\..\..\..\..\..\", @"server\share", @"server\share\" },
            { @"..\..\..\..\foo",            @"server\share", @"server\share\foo" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_Windows_UNC))]
        public void GetFullPath_CommonUnc_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(@"\\" + expected, Path.GetFullPath(path, @"\\" + basePath));
            Assert.Equal(@"\\.\UNC\" + expected, Path.GetFullPath(path, @"\\.\UNC\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_UNC_ExtendedPrefix => new TheoryData<string, string, string>
        {
            { @"foo",    @"",              @"foo" },
            { @"foo",    @"server1",       @"server1\foo" },
            { @"\foo",   @"server2",       @"server2\foo" },
            { @"foo",    @"server3\",      @"server3\foo" },
            { @"..\foo", @"server4",       @"server4\..\foo" },
            { @".\foo",  @"server5\share", @"server5\share\.\foo" },
            { @"..\foo", @"server6\share", @"server6\share\..\foo" },
            { @"\foo",   @"a\b\\",         @"a\b\foo" },
            { @"foo",      @"LOCALHOST\share8\test.txt.~SS", @"LOCALHOST\share8\test.txt.~SS\foo" },
            { @"foo",      @"LOCALHOST\share9",              @"LOCALHOST\share9\foo" },
            { @"foo",      @"LOCALHOST\shareA\dir",          @"LOCALHOST\shareA\dir\foo" },
            { @". \foo",   @"LOCALHOST\shareB\",             @"LOCALHOST\shareB\. \foo" },
            { @".. \foo",  @"LOCALHOST\shareC\",             @"LOCALHOST\shareC\.. \foo" },
            { @"    \foo", @"LOCALHOST\shareD\",             @"LOCALHOST\shareD\    \foo" },

            { "foo", @"LOCALHOST\  shareE\",           @"LOCALHOST\  shareE\foo" },
            { "foo", @"LOCALHOST\shareF\test.txt.~SS", @"LOCALHOST\shareF\test.txt.~SS\foo" },
            { "foo", @"LOCALHOST\shareG",              @"LOCALHOST\shareG\foo" },
            { "foo", @"LOCALHOST\shareH\dir",          @"LOCALHOST\shareH\dir\foo" },
            { "foo", @"LOCALHOST\shareK\",             @"LOCALHOST\shareK\foo" },
            { "foo", @"LOCALHOST\  shareL\",           @"LOCALHOST\  shareL\foo" },

            // Relative segments eating into the root
            { @".\..\foo\..\",               @"server\share", @"server\share\.\..\foo\..\" },
            { @"..\foo\tmp\..\..\",          @"server\share", @"server\share\..\foo\tmp\..\..\" },
            { @"..\..\..\foo",               @"server\share", @"server\share\..\..\..\foo" },
            { @"..\foo\..\..\tmp",           @"server\share", @"server\share\..\foo\..\..\tmp" },
            { @"..\foo",                     @"server\share", @"server\share\..\foo" },
            { @"...\\foo",                   @"server\share", @"server\share\...\\foo" },
            { @"...\..\.\foo",               @"server\share", @"server\share\...\..\.\foo" },
            { @"..\foo\tmp\..\..\..\..\..\", @"server\share", @"server\share\..\foo\tmp\..\..\..\..\..\" },
            { @"..\..\..\..\foo",            @"server\share", @"server\share\..\..\..\..\foo" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_Windows_UNC_ExtendedPrefix))]
        public void GetFullPath_CommonUnc_Windows_ExtendedPrefix(string path, string basePath, string expected)
        {
            // Paths starting with \\?\ are considered normalized and should not get modified
            Assert.Equal(@"\\?\UNC\" + expected, Path.GetFullPath(path, @"\\?\UNC\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_CommonDevicePaths => new TheoryData<string, string, string>
        {
            // Device paths
            { @"foo",       @"C:\ ",    @"C:\ \foo" },
            { @" \ \foo",   @"C:\",     @"C:\ \ \foo" },
            { @" .\foo",    @"C:\",     @"C:\ .\foo" },
            { @" ..\foo",   @"C:\",     @"C:\ ..\foo" },
            { @"...\foo",   @"C:\",     @"C:\...\foo" },

            { @"foo",       @"C:\\",    @"C:\foo" },
            { @"foo.",      @"C:\\",    @"C:\foo." },
            { @"foo \git",  @"C:\\",    @"C:\foo \git" },
            { @"foo. \git", @"C:\\",    @"C:\foo. \git" },
            { @" foo \git", @"C:\\",    @"C:\ foo \git" },
            { @"foo ",      @"C:\\",    @"C:\foo " },
            { @"|\foo",     @"C:\",     @"C:\|\foo" },
            { @".\foo",     @"C:\",     @"C:\foo" },
            { @"..\foo",    @"C:\",     @"C:\foo" },

            { @"\Foo1\.\foo",   @"C:\",     @"C:\Foo1\foo" },
            { @"\Foo2\..\foo",  @"C:\",     @"C:\foo" },

            { @"foo",       @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"foo",       @"",            @"foo" },
            { @".\foo",     @"",            @".\foo" },
            { @"..\foo",    @"",            @"..\foo" },
            { @"C:",        @"",            @"C:\"},

            // Relative segments eating into the root
            { @"foo",               @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"..\..\foo\..\..\",  @"",            @"..\" },
            { @".\..\..\..\..\foo", @"",            @".\foo" },
            { @"..\foo\..\..\..\",  @"",            @"..\" },
            { @"\.\.\..\",          @"C:\",         @"C:\"},
            { @"..\..\..\foo",      @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"foo\..\..\",        @"",            @"foo\" },
            { @".\.\foo\..\",       @"",            @".\" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_Windows_CommonDevicePaths))]
        public void GetFullPath_CommonDevice_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(@"\\.\" + expected, Path.GetFullPath(path, @"\\.\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_Windows_CommonExtendedPaths => new TheoryData<string, string, string>
        {
            // Device paths
            { @"foo",       @"C:\ ",    @"C:\ \foo" },
            { @" \ \foo",   @"C:\",     @"C:\ \ \foo" },
            { @" .\foo",    @"C:\",     @"C:\ .\foo" },
            { @" ..\foo",   @"C:\",     @"C:\ ..\foo" },
            { @"...\foo",   @"C:\",     @"C:\...\foo" },

            { @"foo",       @"C:\\",    @"C:\\foo" },
            { @"foo.",      @"C:\\",    @"C:\\foo." },
            { @"foo \git",  @"C:\\",    @"C:\\foo \git" },
            { @"foo. \git", @"C:\\",    @"C:\\foo. \git" },
            { @" foo \git", @"C:\\",    @"C:\\ foo \git" },
            { @"foo ",      @"C:\\",    @"C:\\foo " },
            { @"|\foo",     @"C:\",     @"C:\|\foo" },
            { @".\foo",     @"C:\",     @"C:\.\foo" },
            { @"..\foo",    @"C:\",     @"C:\..\foo" },

            { @"\Foo1\.\foo",   @"C:\",     @"C:\Foo1\.\foo" },
            { @"\Foo2\..\foo",  @"C:\",     @"C:\Foo2\..\foo" },

            { @"foo",       @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"foo",       @"",            @"foo" },
            { @".\foo",     @"",            @".\foo" },
            { @"..\foo",    @"",            @"..\foo" },
            { @"C:",        @"",            @"C:\"},

            // Relative segments eating into the root
            { @"foo",               @"GLOBALROOT\", @"GLOBALROOT\foo" },
            { @"..\..\foo\..\..\",  @"",            @"..\..\foo\..\..\" },
            { @".\..\..\..\..\foo", @"",            @".\..\..\..\..\foo" },
            { @"..\foo\..\..\..\",  @"",            @"..\foo\..\..\..\" },
            { @"\.\.\..\",          @"C:\",         @"C:\.\.\..\"},
            { @"..\..\..\foo",      @"GLOBALROOT\", @"GLOBALROOT\..\..\..\foo" },
            { @"foo\..\..\",        @"",            @"foo\..\..\" },
            { @".\.\foo\..\",       @"",            @".\.\foo\..\" },
        };

        [Theory,
           MemberData(nameof(GetFullPath_Windows_CommonExtendedPaths))]
        public void GetFullPath_CommonExtended_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(@"\\?\" + expected, Path.GetFullPath(path, @"\\?\" + basePath));
        }

        public static TheoryData<string, string, string> GetFullPath_CommonRootedWindowsData => new TheoryData<string, string, string>
        {
            { "",   @"C:\git\runtime", @"C:\git\runtime" },
            { "..", @"C:\git\runtime", @"C:\git" },

            // Current drive rooted
            { @"\tmp\bar",    @"C:\git\runtime", @"C:\tmp\bar" },
            { @"\.\bar",      @"C:\git\runtime", @"C:\bar" },
            { @"\tmp\..",     @"C:\git\runtime", @"C:\" },
            { @"\tmp\bar\..", @"C:\git\runtime", @"C:\tmp" },
            { @"\tmp\bar\..", @"C:\git\runtime", @"C:\tmp" },
            { @"\",           @"C:\git\runtime", @"C:\" },

            { @"..\..\tmp\bar",       @"C:\git\runtime", @"C:\tmp\bar" },
            { @"..\..\.\bar",         @"C:\git\runtime", @"C:\bar" },
            { @"..\..\..\..\tmp\..",  @"C:\git\runtime", @"C:\" },
            { @"\tmp\..\bar..\..\..", @"C:\git\runtime", @"C:\" },
            { @"\tmp\..\bar\..",      @"C:\git\runtime", @"C:\" },
            { @"\.\.\..\..\",         @"C:\git\runtime", @"C:\" },

            // Specific drive rooted
            { @"C:tmp\foo\..", @"C:\git\runtime", @"C:\git\runtime\tmp" },
            { @"C:tmp\foo\.",  @"C:\git\runtime", @"C:\git\runtime\tmp\foo" },
            { @"C:tmp\foo\..", @"C:\git\runtime", @"C:\git\runtime\tmp" },
            { @"C:tmp", @"C:\git\runtime", @"C:\git\runtime\tmp" },
            { @"C:",    @"C:\git\runtime", @"C:\git\runtime" },
            { @"C",     @"C:\git\runtime", @"C:\git\runtime\C" },

            { @"Z:tmp\foo\..", @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:tmp\foo\.",  @"C:\git\runtime", @"Z:\tmp\foo" },
            { @"Z:tmp\foo\..", @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:tmp", @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:",    @"C:\git\runtime", @"Z:\" },
            { @"Z",     @"C:\git\runtime", @"C:\git\runtime\Z" },

            // Relative segments eating into the root
            { @"C:..\..\..\tmp\foo\..", @"C:\git\runtime", @"C:\tmp" },
            { @"C:tmp\..\..\foo\.",     @"C:\git\runtime", @"C:\git\foo" },
            { @"C:..\..\tmp\foo\..",    @"C:\git\runtime", @"C:\tmp" },
            { @"C:tmp\..\", @"C:\git\runtime", @"C:\git\runtime\" },
            { @"C:",        @"C:\git\runtime", @"C:\git\runtime" },
            { @"C",         @"C:\git\runtime", @"C:\git\runtime\C" },

            { @"C:tmp\..\..\..\..\foo\..", @"C:\git\runtime", @"C:\" },
            { @"C:tmp\..\..\foo\.",        @"C:\",            @"C:\foo" },
            { @"C:..\..\tmp\..\foo\..",    @"C:\",            @"C:\" },
            { @"C:tmp\..\",                @"C:\",            @"C:\" },

            { @"Z:tmp\foo\..", @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:tmp\foo\.",  @"C:\git\runtime", @"Z:\tmp\foo" },
            { @"Z:tmp\foo\..", @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:tmp",        @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:",           @"C:\git\runtime", @"Z:\" },
            { @"Z",            @"C:\git\runtime", @"C:\git\runtime\Z" },

            { @"Z:..\..\..\tmp\foo\..", @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:tmp\..\..\foo\.",     @"C:\git\runtime", @"Z:\foo" },
            { @"Z:..\..\tmp\foo\..",    @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:tmp\..\",             @"C:\git\runtime", @"Z:\" },
            { @"Z:",     @"C:\git\runtime", @"Z:\" },
            { @"Z",      @"C:\git\runtime", @"C:\git\runtime\Z" },

            { @"Z:tmp\..\..\..\..\foo\..", @"C:\git\runtime", @"Z:\" },
            { @"Z:tmp\..\..\foo\.",        @"C:\",            @"Z:\foo" },
            { @"Z:..\..\tmp\..\foo\..",   @"C:\",             @"Z:\" },
            { @"Z:tmp\..\",               @"C:\",             @"Z:\" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_CommonRootedWindowsData))]
        public void GetFullPath_CommonUnRooted_Windows(string path, string basePath, string expected)
        {
            Assert.Equal(expected, Path.GetFullPath(path, basePath));
            Assert.Equal(@"\\.\" + expected, Path.GetFullPath(path, @"\\.\" + basePath));
        }
        public static TheoryData<string, string, string> GetFullPath_CommonRootedWindowsData_ExtendedPrefix => new TheoryData<string, string, string>
        {
            { "",   @"C:\git\runtime", @"C:\git\runtime" },
            { "..", @"C:\git\runtime", @"C:\git\runtime\.." },

            // Current drive rooted
            { @"\tmp\bar",    @"C:\git\runtime", @"C:\tmp\bar" },
            { @"\.\bar",      @"C:\git\runtime", @"C:\.\bar" },
            { @"\tmp\..",     @"C:\git\runtime", @"C:\tmp\.." },
            { @"\tmp\bar\..", @"C:\git\runtime", @"C:\tmp\bar\.." },
            { @"\tmp\bar\..", @"C:\git\runtime", @"C:\tmp\bar\.." },
            { @"\",           @"C:\git\runtime", @"C:\" },

            { @"..\..\tmp\bar",       @"C:\git\runtime", @"C:\git\runtime\..\..\tmp\bar" },
            { @"..\..\.\bar",         @"C:\git\runtime", @"C:\git\runtime\..\..\.\bar" },
            { @"..\..\..\..\tmp\..",  @"C:\git\runtime", @"C:\git\runtime\..\..\..\..\tmp\.." },
            { @"\tmp\..\bar..\..\..", @"C:\git\runtime", @"C:\tmp\..\bar..\..\.." },
            { @"\tmp\..\bar\..",      @"C:\git\runtime", @"C:\tmp\..\bar\.." },
            { @"\.\.\..\..\",         @"C:\git\runtime", @"C:\.\.\..\..\" },

            // Specific drive rooted
            { @"C:tmp\foo\..",  @"C:\git\runtime", @"C:\git\runtime\tmp\foo\.." },
            { @"C:tmp\foo\.",   @"C:\git\runtime", @"C:\git\runtime\tmp\foo\." },
            { @"C:tmp\foo\..",  @"C:\git\runtime", @"C:\git\runtime\tmp\foo\.." },
            { @"C:tmp",         @"C:\git\runtime", @"C:\git\runtime\tmp" },
            { @"C:",            @"C:\git\runtime", @"C:\git\runtime" },
            { @"C",             @"C:\git\runtime", @"C:\git\runtime\C" },

            { @"Z:tmp\foo\..",  @"C:\git\runtime", @"Z:\tmp\foo\.." },
            { @"Z:tmp\foo\.",   @"C:\git\runtime", @"Z:\tmp\foo\." },
            { @"Z:tmp\foo\..",  @"C:\git\runtime", @"Z:\tmp\foo\.." },
            { @"Z:tmp",         @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:",            @"C:\git\runtime", @"Z:\" },
            { @"Z",             @"C:\git\runtime", @"C:\git\runtime\Z" },

            // Relative segments eating into the root
            { @"C:..\..\..\tmp\foo\..", @"C:\git\runtime", @"C:\git\runtime\..\..\..\tmp\foo\.." },
            { @"C:tmp\..\..\foo\.",     @"C:\git\runtime", @"C:\git\runtime\tmp\..\..\foo\." },
            { @"C:..\..\tmp\foo\..",    @"C:\git\runtime", @"C:\git\runtime\..\..\tmp\foo\.." },
            { @"C:tmp\..\",             @"C:\git\runtime", @"C:\git\runtime\tmp\..\" },
            { @"C:",                    @"C:\git\runtime", @"C:\git\runtime" },
            { @"C",                     @"C:\git\runtime", @"C:\git\runtime\C" },

            { @"C:tmp\..\..\..\..\foo\..", @"C:\git\runtime", @"C:\git\runtime\tmp\..\..\..\..\foo\.." },
            { @"C:tmp\..\..\foo\.",        @"C:\",            @"C:\tmp\..\..\foo\." },
            { @"C:..\..\tmp\..\foo\..",    @"C:\",            @"C:\..\..\tmp\..\foo\.." },
            { @"C:tmp\..\",                @"C:\",            @"C:\tmp\..\" },

            { @"Z:tmp\foo\..", @"C:\git\runtime", @"Z:\tmp\foo\.." },
            { @"Z:tmp\foo\.",  @"C:\git\runtime", @"Z:\tmp\foo\." },
            { @"Z:tmp\foo\..", @"C:\git\runtime", @"Z:\tmp\foo\.." },
            { @"Z:tmp",        @"C:\git\runtime", @"Z:\tmp" },
            { @"Z:",           @"C:\git\runtime", @"Z:\" },
            { @"Z",            @"C:\git\runtime", @"C:\git\runtime\Z" },

            { @"Z:..\..\..\tmp\foo\..", @"C:\git\runtime", @"Z:\..\..\..\tmp\foo\.." },
            { @"Z:tmp\..\..\foo\.",     @"C:\git\runtime", @"Z:\tmp\..\..\foo\." },
            { @"Z:..\..\tmp\foo\..",    @"C:\git\runtime", @"Z:\..\..\tmp\foo\.." },
            { @"Z:tmp\..\",             @"C:\git\runtime", @"Z:\tmp\..\" },
            { @"Z:",                    @"C:\git\runtime", @"Z:\" },
            { @"Z",                     @"C:\git\runtime", @"C:\git\runtime\Z" },

            { @"Z:tmp\..\..\..\..\foo\..",  @"C:\git\runtime", @"Z:\tmp\..\..\..\..\foo\.." },
            { @"Z:tmp\..\..\foo\.",         @"C:\",            @"Z:\tmp\..\..\foo\." },
            { @"Z:..\..\tmp\..\foo\..",     @"C:\",            @"Z:\..\..\tmp\..\foo\.." },
            { @"Z:tmp\..\",                 @"C:\",            @"Z:\tmp\..\" },
        };

        [Theory,
            MemberData(nameof(GetFullPath_CommonRootedWindowsData_ExtendedPrefix))]
        public void GetFullPath_CommonUnRooted_Windows_ExtendedPrefix(string path, string basePath, string expected)
        {
            // Paths starting with \\?\ are considered normalized and should not get modified
            Assert.Equal(@"\\?\" + expected, Path.GetFullPath(path, @"\\?\" + basePath));
        }
    }
}
