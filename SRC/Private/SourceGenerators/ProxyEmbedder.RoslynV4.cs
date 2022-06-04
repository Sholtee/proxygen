/********************************************************************************
* ProxyEmbedder.RoslynV4.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    internal class ProxyEmbedder_RoslynV4 : ProxyEmbedderBase, IIncrementalGenerator
    {
        private static bool IsEmbedGeneratedTypeAttribute(SyntaxNode node, CancellationToken cancellation)
        {
            return (node as AttributeSyntax)?.Name switch
            {
                SimpleNameSyntax sn => IsEmbedGeneratedTypeAttribute(sn.Identifier),
                QualifiedNameSyntax qn => IsEmbedGeneratedTypeAttribute(qn.Right.Identifier),
                _ => false,
            };

            static bool IsEmbedGeneratedTypeAttribute(SyntaxToken token) => token.Text is "EmbedGeneratedTypeAttribute" or "EmbedGeneratedType";
        }

        private static INamedTypeSymbol? ExtractGenerator(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            AttributeSyntax attr = (AttributeSyntax) context.Node;
            
            if (attr.ArgumentList?.Arguments.Count is not 1)
                return null;

            if (attr.ArgumentList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOf)
                return null;

            if (context.SemanticModel.GetSymbolInfo(typeOf.Type, cancellationToken).Symbol is not INamedTypeSymbol namedType)
                return null;

            return namedType;
        }

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<INamedTypeSymbol> aotGenerators = context
                .SyntaxProvider
                .CreateSyntaxProvider(IsEmbedGeneratedTypeAttribute, ExtractGenerator)
                .WithComparer(SymbolEqualityComparer)
                .Where(static gen => gen is not null)!;

            IncrementalValueProvider<(Compilation, AnalyzerConfigOptionsProvider)> compilationAndOptions = context
                .CompilationProvider
                .Combine(context.AnalyzerConfigOptionsProvider);

            IncrementalValueProvider<((Compilation, AnalyzerConfigOptionsProvider), ImmutableArray<INamedTypeSymbol>)> aotGeneratorsAndCompilation = compilationAndOptions
                .Combine
                (
                    aotGenerators.Collect()
                );

            context.RegisterSourceOutput
            (
                aotGeneratorsAndCompilation,
                static (spc, src) =>
                {
                    ((Compilation Compilation, AnalyzerConfigOptionsProvider Opts), ImmutableArray<INamedTypeSymbol> AotGenerators) = src;

                    Execute
                    (
                        Compilation,
                        Opts.GlobalOptions,
                        AotGenerators,
                        spc.ReportDiagnostic,
                        code => spc.AddSource
                        (
                            code.Hint,
                            code.Value
                        ),
                        spc.CancellationToken
                    );
                }
            );
        }
    }
}
