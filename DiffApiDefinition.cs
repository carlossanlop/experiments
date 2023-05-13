// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.GenApiDiff;

public sealed class DiffApiDefinition
{
    public string Name { get; private set; }
    public IDefinition Definition { get; private set; }
    public IDefinition Left { get; private set; }
    public IDefinition Right { get; private set; }
    public int StartLine { get; internal set; }
    public int EndLine { get; internal set; }
    public DifferenceType Difference { get; private set; }
    public ReadOnlyCollection<DiffApiDefinition> Children { get; private set; }

    public DiffApiDefinition(IDefinition left, IDefinition right, DifferenceType difference, IList<DiffApiDefinition> children)
    {
        IDefinition representative = left ?? right;
        Name = GetName(representative);
        Definition = representative;
        Left = left;
        Right = right;
        Difference = difference;
        Children = new ReadOnlyCollection<DiffApiDefinition>(children);
    }

    public override string ToString()
    {
        return Difference.ToString().Substring(0, 1) + " " + Definition.UniqueId();
    }

    private static string GetName(object obj)
    {
        if (obj is IAssembly assembly)
            return GetName(assembly);

        if (obj is INamespaceDefinition namespaceDefinition)
            return GetName(namespaceDefinition);

        if (obj is ITypeMemberReference typeMemberReference)
            return GetName(typeMemberReference);

        if (obj is ITypeReference typeReference)
            return GetName(typeReference);

        throw new NotImplementedException("Unknown CCI object type: " + obj.GetType());
    }

    private static string GetName(IAssembly assembly) => assembly.Name.Value;

    private static string GetName(INamespaceDefinition namespaceName)
    {
        string? name = namespaceName.ToString();
        return string.IsNullOrEmpty(name)
        ? "-"
        : name;
    }

    private static string GetName(ITypeReference typeReference) => TypeHelper.GetTypeName(typeReference, NameFormattingOptions.TypeParameters |
                                                     NameFormattingOptions.OmitContainingNamespace);

    private static string GetName(ITypeMemberReference typeMemberReference)
    {
        string memberSignature = MemberHelper.GetMemberSignature(typeMemberReference, NameFormattingOptions.Signature |
                                                                                   NameFormattingOptions.OmitContainingType |
                                                                                   NameFormattingOptions.OmitContainingNamespace |
                                                                                   NameFormattingOptions.PreserveSpecialNames);

        string returnTypeName = GetReturnTypeName(typeMemberReference);
        return returnTypeName == null
                   ? memberSignature
                   : memberSignature + " : " + returnTypeName;
    }

    private static string GetReturnTypeName(ITypeMemberReference typeMemberReference)
    {
        ITypeDefinitionMember typeDefinitionMember = typeMemberReference.ResolvedTypeDefinitionMember;
        if (typeDefinitionMember is IFieldDefinition fieldDefinition)
            return GetName(fieldDefinition.Type);

        if (typeDefinitionMember is IPropertyDefinition propertyDefinition)
            return GetName(propertyDefinition.Type);

        if (typeDefinitionMember is IMethodDefinition methodDefinition &&
            !methodDefinition.IsConstructor && !methodDefinition.IsStaticConstructor)
            return GetName(methodDefinition.Type);

        return null;
    }
}

public static class TypeHelper
{
    public static string GetTypeName(ITypeReference r, NameFormattingOptions o) { return null!; }
}

public static class MemberHelper
{
    public static string GetMemberSignature(ITypeMemberReference r, NameFormattingOptions o) { return null!; }
}

[Flags]
public enum NameFormattingOptions
{
    None = 0x0,
    Signature = 0x1,
    OmitContainingType = 0x2,
    OmitContainingNamespace = 0x4,
    PreserveSpecialNames = 0x8,
    TypeParameters = 0x10
}

public interface ITypeDefinitionMember
{

}

public interface IDefinition
{
    public string GetCSharpDeclaration();
    public int UniqueId();
}

public interface IName
{
    public string Value { get; }
}

public interface IAssembly
{
    public IName Name { get; }
}
public interface INamespaceDefinition { }
public interface ITypeMemberReference {
public ITypeDefinitionMember ResolvedTypeDefinitionMember { get; }
}
public interface ITypeReference { }
public interface IFieldDefinition
{
    public Type Type { get; }

}

public interface IMethodDefinition {
    public Type Type { get; }
    public bool IsConstructor { get; }
    public bool IsStaticConstructor { get; }
}
public interface IPropertyDefinition {
    public Type Type { get; }
}
