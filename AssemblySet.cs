// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.GenApiDiff;
public class AssemblySet
{
    public string Name { get; }
    public List<FileInfo> Assemblies { get; }

    internal AssemblySet(string name, List<FileInfo> assemblies)
    {
        Name = name;
        Assemblies = assemblies;
    }
}
