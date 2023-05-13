// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.GenApiDiff;

public sealed class MarkdownDiffExporter
{
    private readonly Configuration _c;
    private readonly DiffDocument _document;

    public MarkdownDiffExporter(Configuration c)
    {
        _c = c;
        _document = new(c);
        _document.Build();
    }

    public void Export()
    {
        using StreamWriter writer = new(_c.OutFile.FullName);
        WriteHeader(writer);
        WriteTableOfContents(writer);
        
        WriteDiffForNamespaces();
    }

    private void WriteHeader(StreamWriter writer)
    {
        writer.WriteLine("# API Difference {0} vs {1}", _c.OldSet.Name, _c.NewSet.Name);
        writer.WriteLine();
        writer.WriteLine("API listing follows standard diff formatting.");
        writer.WriteLine("Lines preceded by a '+' are additions and a '-' indicates removal.");
        writer.WriteLine();
    }

    private void WriteTableOfContents(StreamWriter writer)
    {
        foreach (DiffApiDefinition topLevelApi in _document.ApiDefinitions)
        {
            string linkTitle = topLevelApi.Name;
            string linkTarget = Path.GetFileName(GetFileNameForNamespace(topLevelApi.Name));
            writer.WriteLine("* [{0}]({1})", linkTitle, linkTarget);
        }
        writer.WriteLine();
    }

    private void WriteDiffForNamespaces()
    {
        foreach (DiffApiDefinition topLevelApi in _document.ApiDefinitions)
        {
            string fileName = GetFileNameForNamespace(topLevelApi.Name);
            using StreamWriter writer = new(fileName);
            WriteDiffForNamespace(writer, topLevelApi);
        }
    }

    private void WriteDiffForNamespace(StreamWriter writer, DiffApiDefinition topLevelApi)
    {
        writer.WriteLine("# " + topLevelApi.Name);
        writer.WriteLine();
        WriteDiff(writer, topLevelApi);
        writer.WriteLine();
    }

    private static void WriteDiff(StreamWriter writer, DiffApiDefinition topLevelApi)
    {
        writer.WriteLine("``` diff");
        WriteDiff(writer, topLevelApi, 0);
        writer.WriteLine("```");
    }

    private static void WriteDiff(StreamWriter writer, DiffApiDefinition api, int level)
    {
        bool hasChildren = api.Children.Count != 0;

        string indent = new(' ', level * 4);
        string suffix = hasChildren ? " {" : string.Empty;
        DifferenceType diff = api.Difference;

        if (diff == DifferenceType.Changed)
        {
            // Let's see whether the syntax actually changed. For some cases the syntax might not
            // diff, for example, when attribute declarations have changed.

            string left = api.Left.GetCSharpDeclaration();
            string right = api.Right.GetCSharpDeclaration();

            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                diff = DifferenceType.Unchanged;
        }

        switch (diff)
        {
            case DifferenceType.Added:
                WriteDiff(writer, "+", indent, suffix, api.Right);
                break;
            case DifferenceType.Removed:
                WriteDiff(writer, "-", indent, suffix, api.Left);
                break;
            case DifferenceType.Changed:
                WriteDiff(writer, "-", indent, suffix, api.Left);
                WriteDiff(writer, "+", indent, suffix, api.Right);
                break;
            default:
                WriteDiff(writer, " ", indent, suffix, api.Definition);
                break;
        }

        if (hasChildren)
        {
            foreach (DiffApiDefinition child in api.Children)
            {
                WriteDiff(writer, child, level + 1);
            }

            string diffMarker = diff == DifferenceType.Added
                                ? "+"
                                : diff == DifferenceType.Removed
                                    ? "-"
                                    : " ";

            writer.Write(diffMarker);
            writer.Write(indent);
            writer.WriteLine("}");
        }
    }

    private static void WriteDiff(StreamWriter writer, string marker, string indent, string suffix, IDefinition api)
    {
        IEnumerable<string> lines = GetCSharpDecalarationLines(api);
        bool isFirst = true;

        foreach (string line in lines)
        {
            if (isFirst)
                isFirst = false;
            else
                writer.WriteLine();

            writer.Write(marker);
            writer.Write(indent);
            writer.Write(line);
        }

        writer.WriteLine(suffix);
    }

    private static IEnumerable<string> GetCSharpDecalarationLines(IDefinition api)
    {
        string text = api.GetCSharpDeclaration();
        using StringReader reader = new(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }

    private string GetFileNameForNamespace(string namespaceName)
    {
        string directory = Path.GetDirectoryName(_c.OutFile.FullName) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(_c.OutFile.FullName);
        string extension = Path.GetExtension(_c.OutFile.FullName);
        return Path.Combine(directory, fileName + "_" + namespaceName + extension);
    }
}
