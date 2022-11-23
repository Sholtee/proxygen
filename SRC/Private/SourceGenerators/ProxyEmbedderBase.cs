/********************************************************************************
* ProxyEmbedderBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyEmbedderBase
    {
        protected static readonly SymbolEqualityComparer SymbolEqualityComparer = SymbolEqualityComparer.Default;

        protected static void Execute(
            Compilation cmp,
            AnalyzerConfigOptions configOptions,
            IReadOnlyCollection<INamedTypeSymbol> aotGenerators,
            Action<Diagnostic> reportDiagnostic,
            Action<SourceCode> addSource,
            CancellationToken cancellation)
        {
#if DEBUG
            if (SourceGeneratorConfig.Instance.DebugGenerator && !Debugger.IsAttached)
                Debugger.Launch();
#endif
            //
            // Only C# 7.0+ is supported
            //

            if (cmp is not CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp7 } compilation)
            {
                reportDiagnostic
                (
                    Diagnostics.PGE00(Location.None)
                );
                return;
            }

            if (!aotGenerators.Some())
                return;

            IConfigReader configReader = new AnalyzerConfigReader(configOptions);

            WorkingDirectories.Setup(configReader);
            SourceGeneratorConfig.Setup(configReader);

            int extensionCount = 0;

            foreach (INamedTypeSymbol generator in aotGenerators)
            {
                Location location = generator.Locations[0];  // don't use Single() here

                try
                {
                    generator.EnsureNotError();

                    ExtendWith
                    (
                        CreateMainUnit(generator, compilation),
                        location
                    );

                    extensionCount++;
                }
                catch (InvalidSymbolException)
                {
                    //
                    // Jump to the next generator
                    //
                }
                #pragma warning disable CA1031 // We want to report all non symbol related exceptions.
                catch (Exception e)
                #pragma warning restore CA1031
                {
                    ReportError(e, location);
                }
            }

            //
            // Chunks are applied only if the source has been augmented.
            //

            if (extensionCount > 0)
            {
                try
                {
                    foreach (UnitSyntaxFactoryBase chunk in CreateChunks(compilation))
                    {
                        ExtendWith(chunk, Location.None);
                    }
                }
                #pragma warning disable CA1031 // We want to report all non symbol related exceptions.
                catch (Exception e)
                #pragma warning restore CA1031
                {
                    ReportError(e, Location.None);
                }
            }

            void ReportError(Exception ex, Location location) => reportDiagnostic
            (
                Diagnostics.PGE01
                (
                    location,
                    ex.Message,
                    LogException(ex, cancellation) ?? "NULL"
                )
            );

            void ExtendWith(UnitSyntaxFactoryBase syntaxFactory, Location location)
            {
                SourceCode source = syntaxFactory.GetSourceCode(cancellation); 

                addSource(source);

                reportDiagnostic
                (
                    Diagnostics.PGI00(location, source.Hint)
                );
            }
        }
    }
}
