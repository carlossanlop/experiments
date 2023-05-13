// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.GenApiDiff;

public sealed class DiffDocument
{
    private readonly Configuration _c;
    public ReadOnlyCollection<DiffLine> Lines { get; private set; } = null!;
    public ReadOnlyCollection<DiffApiDefinition> ApiDefinitions { get; private set; } = null!;

    public DiffDocument(Configuration c)
    {
        _c = c;
    }


    public DiffDocument Build()
    {
        DiffRecorder recorder = new();
        using StreamWriter writer = File.CreateText(_c.OutFile.FullName);
        writer.Write();

        IEnumerable<DiffToken> tokens = recorder.Tokens;
        ApiDefinitions = new ReadOnlyCollection<DiffApiDefinition>(writer.ApiDefinitions.ToArray());
        Lines = GetLines(tokens);
    }

    private ReadOnlyCollection<DiffLine> GetLines(IEnumerable<DiffToken> tokens)
    {
        List<DiffLine> lines = new();
        List<DiffToken> currentLineTokens = new();

        foreach (DiffToken diffToken in tokens)
        {
            if (diffToken.Kind != DiffTokenKind.LineBreak)
            {
                currentLineTokens.Add(diffToken);
            }
            else
            {
                DiffLineKind kind = GetDiffLineKind(currentLineTokens);
                DiffLine line = new(kind, currentLineTokens);
                lines.Add(line);
                currentLineTokens.Clear();
            }
        }

        // HACH: Fixup lines that only have closing brace
        return FixupCloseBraces(lines);
    }

    private ReadOnlyCollection<DiffLine> FixupCloseBraces(IList<DiffLine> lines)
    {
        Stack<DiffLine> startLineStack = new();

        List<DiffLine> fixedLines = new();
        foreach (DiffLine diffLine in lines)
        {
            int braceDelta = GetBraceDelta(diffLine);
            DiffLine result = diffLine;

            for (int i = braceDelta; i > 0; i--)
                startLineStack.Push(diffLine);

            for (int i = braceDelta; i < 0 && startLineStack.Count > 0; i++)
            {
                DiffLine startLine = startLineStack.Pop();
                DiffLineKind fixedLineKind = startLine.Kind;
                if (result.Kind != fixedLineKind)
                    result = new DiffLine(fixedLineKind, diffLine.Tokens);
            }

            fixedLines.Add(result);
        }

        return new ReadOnlyCollection<DiffLine>(fixedLines);
    }

    private int GetBraceDelta(DiffLine diffLine)
    {
        int openBraces = 0;
        foreach (DiffToken? symbol in diffLine.Tokens.Where(t => t.Kind == DiffTokenKind.Symbol))
        {
            switch (symbol.Text)
            {
                case "{":
                    openBraces++;
                    break;
                case "}":
                    openBraces--;
                    break;
            }
        }

        return openBraces;
    }

    private DiffLineKind GetDiffLineKind(IEnumerable<DiffToken> currentLineTokens)
    {
        IEnumerable<DiffToken> relevantTokens = currentLineTokens.Where(t => t.Kind != DiffTokenKind.Indent &&
                                                          t.Kind != DiffTokenKind.Whitespace &&
                                                          t.Kind != DiffTokenKind.LineBreak);

        bool hasSame = HasStyle(relevantTokens, DiffStyle.None);
        bool hasAdditions = HasStyle(relevantTokens, DiffStyle.Added);
        bool hasRemovals = HasStyle(relevantTokens, DiffStyle.Removed);
        bool hasIncompatibility = HasStyle(relevantTokens, DiffStyle.NotCompatible);

        if (hasSame && (hasAdditions || hasRemovals) || hasIncompatibility)
            return DiffLineKind.Changed;

        if (hasAdditions)
            return DiffLineKind.Added;

        if (hasRemovals)
            return DiffLineKind.Removed;

        return DiffLineKind.Same;
    }

    private static bool HasStyle(IEnumerable<DiffToken> tokens, DiffStyle diffStyle)
    {
        return tokens.Where(t => t.HasStyle(diffStyle)).Any();
    }
}
