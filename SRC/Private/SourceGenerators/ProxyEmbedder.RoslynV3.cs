/********************************************************************************
* ProxyEmbedder.RoslynV3.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;

    internal class ProxyEmbedder_RoslynV3 : ProxyEmbedderBase, ISourceGenerator
    {
        internal static IEnumerable<INamedTypeSymbol> GetAOTGenerators(Compilation compilation)
        {
            INamedTypeSymbol egta = compilation.GetTypeByMetadataName(typeof(EmbedGeneratedTypeAttribute).FullName)!;

            #pragma warning disable RS1024 // Compare symbols correctly
            HashSet<INamedTypeSymbol> returnedGenerators = new(SymbolEqualityComparer);
            #pragma warning restore RS1024

            foreach (AttributeData attr in compilation.Assembly.GetAttributes())
            {
                if (SymbolEqualityComparer.Equals(attr.AttributeClass, egta))
                {
                    Debug.Assert(attr.ConstructorArguments.Length is 1);

                    INamedTypeSymbol generator = (INamedTypeSymbol) attr.ConstructorArguments[0].Value!;
                    if (returnedGenerators.Add(generator))
                        yield return generator;
                }
            }
        }

        void ISourceGenerator.Initialize(GeneratorInitializationContext context)
        {
        }

        void ISourceGenerator.Execute(GeneratorExecutionContext context) => Execute
        (
            context.Compilation,
            context
                .AnalyzerConfigOptions
                .GlobalOptions,
            new List<INamedTypeSymbol>
            (
                GetAOTGenerators(context.Compilation)
            ),
            context.ReportDiagnostic,
            src => context.AddSource(src.Hint, src.Value),
            context.CancellationToken
        );
    }
}
