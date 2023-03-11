/********************************************************************************
* ProxyEmbedder.RoslynV3.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if LEGACY_COMPILER
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;

    #pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
    [Generator(LanguageNames.CSharp)]
    #pragma warning restore CS3016
    internal sealed class ProxyEmbedder : ProxyEmbedderBase, ISourceGenerator
    {
        internal static IEnumerable<INamedTypeSymbol> GetAOTGenerators(Compilation compilation)
        {
            INamedTypeSymbol egta = compilation.GetTypeByMetadataName(typeof(EmbedGeneratedTypeAttribute).FullName)!;

            #pragma warning disable RS1024 // Compare symbols correctly
            HashSet<INamedTypeSymbol> returnedGenerators = new(SymbolEqualityComparer);
            #pragma warning restore RS1024

            foreach (AttributeData attr in compilation.Assembly.GetAttributes())
            {
                if (!SymbolEqualityComparer.Equals(attr.AttributeClass, egta))
                    continue;

                if (attr.ConstructorArguments.Length is not 1 || attr.ConstructorArguments[0].Value is not INamedTypeSymbol generator)
                    continue;

                if (returnedGenerators.Add(generator))
                    yield return generator;
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
#endif
