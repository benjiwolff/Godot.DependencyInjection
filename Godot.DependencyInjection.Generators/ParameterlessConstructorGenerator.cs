using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Godot.DependencyInjection.Generators;

[Generator]
public class ParameterlessConstructorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => node is ConstructorDeclarationSyntax { Parent: ClassDeclarationSyntax },
            (ctx, _) => (ConstructorDeclarationSyntax)ctx.Node);
        var combinedProvider = context.CompilationProvider.Combine(syntaxProvider.Collect());
        context.RegisterImplementationSourceOutput(
            combinedProvider,
            (ctx, provider) => GenerateParameterlessConstructors(ctx, provider.Left, provider.Right));
    }

    private static void GenerateParameterlessConstructors(
        SourceProductionContext context,
        Compilation compilation,
        IImmutableList<ConstructorDeclarationSyntax> constructorDeclarationSyntaxes)
    {
        var constructorLookup = constructorDeclarationSyntaxes
            .ToLookup<ConstructorDeclarationSyntax, ITypeSymbol, ConstructorDeclarationSyntax>(
                c => compilation.GetSemanticModel(c.SyntaxTree)
                    .GetDeclaredSymbol(
                        (ClassDeclarationSyntax)c.Parent!,
                        context.CancellationToken)!,
                c => c,
                SymbolEqualityComparer.Default);

        var godotScripts = constructorLookup.Select(g => g.Key)
            .Where(
                c => compilation.ClassifyConversion(c, compilation.GetTypeByMetadataName("Godot.Node")!)
                    .IsImplicit);

        var classesThatMissParameterlessConstructor = godotScripts.Where(
            g => constructorLookup[g].ToArray() is var constructors &&
                 constructors.Any() &&
                 constructors.All(c => c.ParameterList.Parameters.Count > 0));

        foreach (var @class in classesThatMissParameterlessConstructor)
        {
            var existingConstructor = constructorLookup[@class].First();
            var stringBuilder = new StringBuilder();
            if (@class.NamespaceOrNull() is { } @namespace)
            {
                stringBuilder.AppendLine(
                    SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
                        .ToFullString());
            }

            stringBuilder.AppendLine(
                SyntaxFactory.ClassDeclaration(@class.Name)
                    .AddMembers(
                        SyntaxFactory.ConstructorDeclaration(
                            SyntaxFactory.List<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(),
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(),
                                SyntaxKind.IdentifierName,
                                @class.Name,
                                @class.Name,
                                SyntaxFactory.TriviaList()),
                            SyntaxFactory.ParameterList(),
                            SyntaxFactory.ConstructorInitializer(
                                SyntaxKind.ThisConstructorInitializer,
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory
                                        .SeparatedList(
                                            existingConstructor.ParameterList.Parameters.Select(
                                                p =>
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.DefaultExpression(p.Type!)))))),
                            (BlockSyntax?)null,
                            SyntaxFactory.Token(SyntaxKind.None)))
                    .ToFullString());

            context.AddSource(
                @class.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "g.cs",
                stringBuilder.ToString());
        }
    }
}