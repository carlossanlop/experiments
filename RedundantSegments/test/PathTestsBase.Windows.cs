using Xunit;

namespace test
{
    public partial class PathTestsBase
    {
        // TODO: Include \\.\ as well
        public static TheoryData<string, string> TestData_GetPathRoot_DevicePaths => new TheoryData<string, string>
        {
            { @"\\?\UNC\test\unc\path\to\something", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\test\unc" },
            { @"\\?\UNC\test\unc", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\test\unc" },
            { @"\\?\UNC\a\b1", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\a\b1" },
            { @"\\?\UNC\a\b2\", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\UNC" : @"\\?\UNC\a\b2" },
            { @"\\?\C:\foo\bar.txt", PathFeatures.IsUsingLegacyPathNormalization() ? @"\\?\C:" : @"\\?\C:\" }
        };
    }
}