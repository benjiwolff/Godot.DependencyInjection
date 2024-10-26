using Microsoft.CodeAnalysis;

namespace Godot.DependencyInjection.Generators;

public static class SymbolExtensions
{
    public static string? NamespaceOrNull(this ISymbol symbol)
        => symbol.ContainingNamespace.IsGlobalNamespace ? null : string.Join(".", symbol.ContainingNamespace.ConstituentNamespaces);
}