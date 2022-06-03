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
            if (node is not AttributeSyntax attr)
                return false;

            switch (attr.Name)
            {
                case SimpleNameSyntax sn:
                    return IsEmbedGeneratedTypeAttribute(sn.Identifier);
                case QualifiedNameSyntax qn:
                    return IsEmbedGeneratedTypeAttribute(qn.Right.Identifier);
                default:
                    return false;
            }

            static bool IsEmbedGeneratedTypeAttribute(SyntaxToken token) => token.Text is "EmbedGeneratedTypeAttribute" or "EmbedGeneratedType";
        }

        private static INamedTypeSymbol? ExtractGenerator(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            AttributeSyntax attr = (AttributeSyntax) context.Node;
            
            if (attr.ArgumentList?.Arguments.Count is not 1)
                return null;

            if (attr.ArgumentList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOf)
                return null;

            return context
                .SemanticModel
                .GetTypeInfo(typeOf.Type, cancellationToken).Type as INamedTypeSymbol;
        }

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<INamedTypeSymbol> aotGenerators = context
                .SyntaxProvider
                .CreateSyntaxProvider(IsEmbedGeneratedTypeAttribute, ExtractGenerator)
                .Where(static gen => gen is not null)!;

            IncrementalValueProvider<(Compilation, (AnalyzerConfigOptionsProvider, ImmutableArray<INamedTypeSymbol>))> aotGeneratorsAndCompilation = context
                .CompilationProvider
                .Combine
                (
                    context
                        .AnalyzerConfigOptionsProvider
                        .Combine
                        (
                            aotGenerators.Collect()
                        )
                );

            context.RegisterSourceOutput
            (
                aotGeneratorsAndCompilation,
                static (spc, src) =>
                {
                    (Compilation Compilation, (AnalyzerConfigOptionsProvider Opts, ImmutableArray<INamedTypeSymbol> AotGenerators)) = src;

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
