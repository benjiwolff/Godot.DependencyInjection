using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            var existingClassDeclarationSyntax = (ClassDeclarationSyntax)existingConstructor.Parent!;
            var semanticModel = compilation.GetSemanticModel(existingClassDeclarationSyntax.SyntaxTree);

            var stringBuilder = new StringBuilder();
            if (@class.NamespaceOrNull() is { } @namespace)
            {
                stringBuilder.AppendLine(
                    SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
                        .NormalizeWhitespace()
                        .ToFullString());
            }

            var constructorDeclarationSyntax = SyntaxFactory.ConstructorDeclaration(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                identifier: SyntaxFactory.Identifier(@class.Name),
                parameterList: SyntaxFactory.ParameterList(),
                initializer: SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.ThisConstructorInitializer,
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory
                            .SeparatedList(
                                existingConstructor.ParameterList.Parameters.Select(
                                    p =>
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.DefaultExpression(
                                                SyntaxFactory.ParseTypeName(
                                                    semanticModel.GetTypeInfo(p.Type!).Type!.ToDisplayString(
                                                        SymbolDisplayFormat.FullyQualifiedFormat)))))))),
                body: SyntaxFactory.Block(),
                semicolonToken: SyntaxFactory.Token(SyntaxKind.None));

            var classDeclaration = ExtendPartialClass(
                existingClassDeclarationSyntax,
                new[] { constructorDeclarationSyntax });

            for (var parent = existingClassDeclarationSyntax.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is TypeDeclarationSyntax outerType)
                {
                    classDeclaration = ExtendPartialClass(outerType, new[] { classDeclaration });
                }
            }

            stringBuilder.AppendLine(classDeclaration.NormalizeWhitespace().ToFullString());

            context.AddSource(
                @class.Name + ".g.cs",
                stringBuilder.ToString());
        }
    }

    private static TypeDeclarationSyntax ExtendPartialClass(
        TypeDeclarationSyntax outerType,
        IEnumerable<MemberDeclarationSyntax> memberDeclarationSyntaxes)
    {
        return outerType.RemoveNodes(
                outerType.ChildNodes().Where(n => n is MemberDeclarationSyntax or BaseListSyntax),
                SyntaxRemoveOptions.KeepNoTrivia)!
            .WithMembers(SyntaxFactory.List(memberDeclarationSyntaxes));
    }
}