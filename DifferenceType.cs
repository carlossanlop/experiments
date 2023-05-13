// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.GenApiDiff;

public enum DifferenceType
{
    Unchanged = 0x1,
    Added     = 0x2,
    Removed   = 0x4,
    Changed   = 0x8
}
