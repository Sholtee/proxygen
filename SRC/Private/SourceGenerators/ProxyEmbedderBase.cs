/********************************************************************************
* ProxyEmbedderBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyEmbedderBase
    {
        protected static SymbolEqualityComparer SymbolEqualityComparer { get; } = SymbolEqualityComparer.Default;

        protected static void Execute
        (
            Compilation cmp,
            AnalyzerConfigOptions configOptions,
            IReadOnlyCollection<INamedTypeSymbol> aotGenerators,
            Action<Diagnostic> reportDiagnostic,
            Action<SourceCode> addSource,
            CancellationToken cancellation
        )
        {
            AnalyzerConfig config = new(configOptions);
#if DEBUG
            if (config.DebugGenerator && !Debugger.IsAttached)
                Debugger.Launch();
#endif
            //
            // Only C# 9.0+ is supported
            //

            if (cmp is not CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp9 } compilation)
            {
                reportDiagnostic
                (
                    Diagnostics.PGE00(Location.None)
                );
                return;
            }

            if (!aotGenerators.Any())
                return;

            LoggerFactory loggerFactory = new(config);

            using ILogger logger = loggerFactory.CreateLogger($"SourceGenerator-{cmp.AssemblyName}-{Guid.NewGuid():N}");

            SyntaxFactoryContext context = new()
            {
                OutputType = OutputType.Unit,
                LanguageVersion = compilation.LanguageVersion,
                AssemblyNameOverride = compilation.Assembly.Name,

                //
                // In analyzer mode, collecting references required only when dumping the source
                //

                ReferenceCollector = config.SourceDump is not null
                    ? new ReferenceCollector()
                    : null,
                LoggerFactory = loggerFactory
            };

            int extensionCount = 0;

            foreach (INamedTypeSymbol generator in aotGenerators)
            {
                Location location = generator.Locations[0];  // don't use Single() here

                logger.Log(LogLevel.Info, "PREM-200", $"Found generator ({generator.Name}) on location: {location}");

                try
                {
                    generator.EnsureNotError();

                    using ProxyUnitSyntaxFactoryBase mainUnit = CreateMainUnit(generator, compilation, context);

                    ExtendWith(mainUnit, location);

                    extensionCount++;
                }
                catch (InvalidSymbolException)
                {
                    //
                    // Jump to the next generator
                    //

                    logger.Log(LogLevel.Info, "PREM-201", $"Invalid generator symbol on location: {location}. Skipping");
                }
                catch (Exception e)
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
                    foreach (UnitSyntaxFactoryBase chunk in CreateChunks(compilation, context).AsVolatile())
                    {
                        ExtendWith(chunk, Location.None);
                    }
                }
                catch (Exception e)
                {
                    ReportError(e, Location.None);
                }
            }

            void ReportError(Exception ex, Location location) => reportDiagnostic
            (
                Diagnostics.PGE01(location, ex.Message)
            );

            void ExtendWith(UnitSyntaxFactoryBase syntaxFactory, Location location)
            {
                SourceCode source = new
                (
                    $"{syntaxFactory.ExposedClass}.cs",
                    syntaxFactory.ResolveUnit(null!, cancellation)
                );

                addSource(source);

                reportDiagnostic
                (
                    Diagnostics.PGI00(location, source.Hint)
                );
            }
        }
    }
}
