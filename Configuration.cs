// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.GenApiDiff;

public class Configuration
{
    private static readonly DifferenceType[] s_defaultDifferenceTypes =
        new[]{ DifferenceType.Added, DifferenceType.Changed, DifferenceType.Removed };

    private Configuration()
    {
    }

    public int ReturnValue { get; private set; }
    public FileInfo OutFile { get; private set; } = null!;
    public AssemblySet OldSet { get; private set; } = null!;
    public AssemblySet NewSet { get; private set; } = null!;
    public DifferenceType DiffTypes { get; private set; }
    public bool DiffAttributes { get; private set; }
    public bool AlwaysDiffMembers { get; private set; }
    public bool HighlightBaseMembers { get; private set; }

    public static async Task<Configuration> CreateAsync(string[] args)
    {
        Command command = new("GenApiDiff");

        Option<FileInfo> outFileOption = new(
            name: "--outfile",
            description: "Output file path." /* TODO: Improve description */ );

        Option<DirectoryInfo> oldSetOption = new(
            name: "--oldset",
            description: "A directory path that contains a set of assemblies to use as baseline for the comparison.");

        Option<DirectoryInfo> newSetOption = new(
            name: "--newset",
            description: "A directory path that contains a set of assemblies to compare against the old set.");

        Option<string?> oldSetNameOption = new(
            name: "--oldsetname",
            description: "The friendly name to print for the old set. If not specified, the old set folder name is used.",
            getDefaultValue: () => null);

        Option<string?> newSetNameOption = new(
            name: "--newsetname",
            description: "The friendly name to print for the new set. If not specified, the new set folder name is used.",
            getDefaultValue: () => null);

        Option<DifferenceType[]> differenceTypeOption = new(
            name: "--differencetypes",
            description: "The types of API differences to show.",
            getDefaultValue: () => s_defaultDifferenceTypes);

        Argument<bool> diffAttributesArgument = new(
            name: "--diffattributes",
            description: "Show attribute differences.",
            getDefaultValue: () => true);

        Argument<bool> alwaysDiffMembersArgument = new(
            name: "--alwaysdiffmembers",
            description: "If an entire type is added or removed, show all its members too.",
            getDefaultValue: () => true);

        Argument<bool> highlightBaseMembersArgument = new(
            name: "--highlightbasemembers",
            description: "Show interface implementations and base member overrides.",
            getDefaultValue: () => true);

        // Aliases

        outFileOption.AddAlias("-o");
        oldSetOption.AddAlias("-os");
        newSetOption.AddAlias("-ns");
        oldSetNameOption.AddAlias("-osn");
        newSetNameOption.AddAlias("-nsn");
        differenceTypeOption.AddAlias("-dt");

        // Validators

        outFileOption.LegalFilePathsOnly();
        outFileOption.AddValidator(AcceptNonExistingFileOnly<FileInfo>);

        oldSetOption.LegalFilePathsOnly();
        oldSetOption.AddValidator(AcceptExistingDirectoryOnly<DirectoryInfo>);

        newSetOption.LegalFilePathsOnly();
        newSetOption.AddValidator(AcceptExistingDirectoryOnly<DirectoryInfo>);

        differenceTypeOption.Arity = ArgumentArity.OneOrMore;

        // Insert arguments and options to command

        command.AddOption(outFileOption);
        command.AddOption(oldSetOption);
        command.AddOption(newSetOption);
        command.AddOption(oldSetNameOption);
        command.AddOption(newSetNameOption);
        command.AddOption(differenceTypeOption);

        command.AddArgument(diffAttributesArgument);
        command.AddArgument(alwaysDiffMembersArgument);
        command.AddArgument(highlightBaseMembersArgument);

        Configuration options = new();
        command.SetHandler((context) =>
        {
            ParseResult result = context.ParseResult;

            FileInfo outfile = result.GetValueForOption(outFileOption) ?? throw new ArgumentNullException(outFileOption.Name);
            options.OutFile = outfile;

            DirectoryInfo oldSet = result.GetValueForOption(oldSetOption) ?? throw new ArgumentNullException(oldSetOption.Name);
            List<FileInfo> oldAssemblies = GetAssembliesFromDirectory(oldSet);
            string oldSetName = result.GetValueForOption(oldSetNameOption) ?? oldSet.FullName;
            options.OldSet = new AssemblySet(oldSetName, oldAssemblies);

            DirectoryInfo newSet = result.GetValueForOption(newSetOption) ?? throw new ArgumentNullException(newSetOption.Name);
            List<FileInfo> newAssemblies = GetAssembliesFromDirectory(newSet);
            string newSetName = result.GetValueForOption(newSetNameOption) ?? newSet.FullName;
            options.NewSet = new AssemblySet(newSetName, newAssemblies);

            DifferenceType[] difftypes = result.GetValueForOption(differenceTypeOption) ?? throw new ArgumentNullException(differenceTypeOption.Name);
            options.DiffTypes = GetDiffTypes(difftypes);

            options.DiffAttributes = result.GetValueForArgument(diffAttributesArgument);
            options.AlwaysDiffMembers = result.GetValueForArgument(alwaysDiffMembersArgument);
            options.HighlightBaseMembers = result.GetValueForArgument(highlightBaseMembersArgument);
        });

        options.ReturnValue = await command.InvokeAsync(args);

        return options;
    }

    private static DifferenceType GetDiffTypes(DifferenceType[] diffTypes)
    {
        DifferenceType asFlags = 0;
        foreach (DifferenceType diffType in diffTypes)
        {
            asFlags |= diffType;
        }
        return asFlags;
    }

    private static List<FileInfo> GetAssembliesFromDirectory(DirectoryInfo directory)
    {
        EnumerationOptions options = new() { RecurseSubdirectories = false };

        FileSystemEnumerable<FileInfo> enumerable =
            new(directory.FullName, TransformEntry, options) { ShouldIncludePredicate = ShouldInclude };

        return enumerable.ToList();

        static FileInfo TransformEntry(ref FileSystemEntry entry) => (FileInfo)entry.ToFileSystemInfo();
        static bool ShouldInclude(ref FileSystemEntry entry) => Path.GetExtension(entry.ToFullPath())?.ToLowerInvariant() == ".dll";
    }

    private static void AcceptNonExistingFileOnly<FileInfo>(SymbolResult result)
    {
        string filePath = result.Tokens.Single().Value;
        if (File.Exists(filePath))
        {
            result.ErrorMessage = $"Output file already exists: {filePath}";
        }
    }

    private static void AcceptExistingDirectoryOnly<DirectoryInfo>(SymbolResult result)
    {
        string directoryPath = result.Tokens.Single().Value;
        if (!Directory.Exists(directoryPath))
        {
            result.ErrorMessage = $"Directory does not exist: {directoryPath}";
        }
    }
}
