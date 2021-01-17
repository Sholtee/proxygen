/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using static System.Environment;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;
    using Properties;

    [Generator]
    internal sealed class ProxyEmbedder: ISourceGenerator
    {
        internal static IEnumerable<INamedTypeSymbol> GetAOTGenerators(Compilation compilation) => compilation
            .Assembly
            .GetAttributes()
            .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, compilation.GetTypeByMetadataName(typeof(EmbedGeneratedTypeAttribute).FullName)))
            .Select(attr => attr.ConstructorArguments.Single().Value)
            .Cast<INamedTypeSymbol>();

        //
        // A SourceGenerator a leheto legkevesebb fuggoseget kell hivatkozza (mivel azokat mind hivatkozni kell
        // a Roslyn szamara is), ezert a primitiv naplozas.
        //

        internal static string? LogException(Exception ex, CancellationToken cancellation)
        {
            try
            {
                string logFile = Path.Combine(WorkingDirectories.LogDump, $"ProxyGen_{Guid.NewGuid()}.log");

                using StreamWriter log = File.CreateText(logFile);
                log.AutoFlush = true;

                for (Exception? current = ex; current is not null; current = current.InnerException)
                {
                    if (current != ex) log.Write($"{NewLine}->{NewLine}", cancellation: cancellation);
                    log.Write(current.ToString(), cancellation: cancellation);

                    foreach (object? key in current.Data.Keys) 
                    {
                        log.Write($"{NewLine + key}:{NewLine + current.Data[key]}", cancellation: cancellation);
                    }
                }

                return logFile;
            }
            catch 
            {
                return null;
            }
        }

        internal static void ReportDiagnosticAndLog(GeneratorExecutionContext context, Exception ex, Location location, CancellationToken cancellation) => ReportDiagnostic
        (
            context,
            "PGE01", 
            SGResources.TE_FAILED, 
            string.Format
            (
                SGResources.Culture, 
                SGResources.TE_FAILED_FULL, 
                ex.Message,
                LogException(ex, cancellation) ?? "NULL"
            ), 
            location, 
            DiagnosticSeverity.Warning
        );

        internal static void ReportDiagnostic(GeneratorExecutionContext context, string id, string msg, string fullMsg, Location location, DiagnosticSeverity severity) => context.ReportDiagnostic
        (
            Diagnostic.Create
            (
                new DiagnosticDescriptor(id, msg, fullMsg, SGResources.TE, severity, true),
                location
            )         
        );

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public static IList<ICodeFactory> CodeFactories { get; } = new List<ICodeFactory>
        {
            new ProxyCodeFactory(),
            new DuckCodeFactory()
        };

        public void Execute(GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;

            IEnumerable<INamedTypeSymbol> aotGenerators = GetAOTGenerators(compilation);

            //
            // Csak C#-t tamogatjuk
            //

            if (context.Compilation.Language != CSharpParseOptions.Default.Language)
            {
                if (aotGenerators.Any()) ReportDiagnostic
                (
                    context,
                    "PGE00",
                    SGResources.LNG_NOT_SUPPORTED,
                    SGResources.LNG_NOT_SUPPORTED,
                    Location.None,
                    DiagnosticSeverity.Warning
                );
                return;
            }
            
            foreach (INamedTypeSymbol generator in aotGenerators)
            {
                try
                {
                    generator.EnsureNotError();

                    ICodeFactory codeFactory = CodeFactories.SingleOrDefault(cf => cf.ShouldUse(generator)) ?? throw new InvalidOperationException
                    (
                        string.Format
                        (
                            SGResources.Culture,
                            SGResources.NOT_A_GENERATOR,
                            generator
                        )
                    );

                    foreach (SourceCode source in codeFactory.GetSourceCodes(generator, context))
                    {
                        context.AddSource
                        (
                            source.Hint,
                            source.Value
                        );

                        ReportDiagnostic
                        (
                            context,
                            "PGI00",
                            SGResources.SRC_EXTENDED,
                            string.Format
                            (
                                SGResources.Culture,
                                SGResources.SRC_EXTENDED_FULL,
                                source.Hint
                            ),
                            generator.Locations.Single(),
                            DiagnosticSeverity.Info
                        );
                    }
                }
                catch (InvalidSymbolException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    ReportDiagnosticAndLog
                    (
                        context, 
                        e, 
                        generator.Locations.Single(),
                        context.CancellationToken
                    );
                }
            }
        }
    }
}
