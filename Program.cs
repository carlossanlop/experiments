// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.GenApiDiff;

internal class Program
{
    private static async Task<int> Main()
    {
        string[] args = new string[] {
            "--outfile", @"C:\Users\calope\source\repos\tmp\output\myoutput.md",
            "--oldset",  @"C:\Users\calope\source\repos\tmp\Microsoft.NETCore.App.Before\ref\net8.0",
            "--newset",  @"C:\Users\calope\source\repos\tmp\Microsoft.NETCore.App.After\ref\net8.0",
            "-osn",      "Old",
            "-nsn",      "New"
        };
        Configuration c = await Configuration.CreateAsync(args);

        if (c.ReturnValue != 0)
        {
            Console.WriteLine("Failed!");
            return c.ReturnValue;
        }

        Console.WriteLine($"OutFile: {c.OutFile.FullName}");
        Console.WriteLine($"Total old: {c.OldSet.Assemblies.Count}");
        Console.WriteLine($"Total new: {c.NewSet.Assemblies.Count}");
        Console.WriteLine($"Old name: {c.OldSet.Name}");
        Console.WriteLine($"New name: {c.NewSet.Name}");

        MarkdownDiffExporter exporter = new(c);
        exporter.Export();

        return 0;

        //string beforeText = await File.ReadAllTextAsync(@"C:\Users\calope\source\repos\tmp\console.before.txt");
        //string afterText = await File.ReadAllTextAsync(@"C:\Users\calope\source\repos\tmp\console.after.txt");

        //SyntaxTree beforeSyntaxTree = CSharpSyntaxTree.ParseText(beforeText);
        //SyntaxTree afterSyntaxTree = CSharpSyntaxTree.ParseText(afterText);

        //CompilationUnitSyntax beforeRoot = beforeSyntaxTree.GetRoot() as CompilationUnitSyntax ?? throw new Exception();
        //CompilationUnitSyntax afterRoot = afterSyntaxTree.GetRoot() as CompilationUnitSyntax ?? throw new Exception();

        //IEnumerable<NamespaceDeclarationSyntax> beforeNamespaces = beforeRoot.Members.OfType<NamespaceDeclarationSyntax>();
        //IEnumerable<NamespaceDeclarationSyntax> afterNamespaces = afterRoot.Members.OfType<NamespaceDeclarationSyntax>();

        //DiffConfigurationOptions options = GetDiffOptions();

        //AssemblySet oldAssemblies = AssemblySet.FromPaths(OldSetName, OldSet);
        //AssemblySet newAssemblies = AssemblySet.FromPaths(NewSetName, NewSet);

        //DiffConfiguration diffConfiguration = new DiffConfiguration(oldAssemblies, newAssemblies, options);

        //DiffDocument diffDocument = DiffEngine.BuildDiffDocument(diffConfiguration);
        //var exporter = new MarkdownDiffExporter(diffDocument, "outfile.md", includeTableOfContents: true, createFilePerNamespace: true);
        //exporter.Export();
    }
}
