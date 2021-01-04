/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;
    using Properties;

    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Generator]
    public class ProxyEmbedder: ISourceGenerator
    {
        internal static IEnumerable<INamedTypeSymbol> GetAOTGenerators(Compilation compilation) => compilation
            .Assembly
            .GetAttributes()
            .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, compilation.GetTypeByMetadataName(typeof(EmbedGeneratedTypeAttribute).FullName)))
            .Select(attr => attr.ConstructorArguments.Single().Value)
            .Cast<INamedTypeSymbol>();

        internal static Diagnostic CreateDiagnosticAndLog(Exception ex, Location location) 
        {
            string? logFile = null;

            try
            {
                logFile = Path.Combine(Path.GetTempPath(), $"ProxyGen_{Guid.NewGuid()}.log");

                using var log = new StreamWriter(File.OpenWrite(logFile));

                //
                // A SourceGenerator a leheto legkevesebb fuggoseget kell hivatkozza (mivel azokat mind hivatkozni kell
                // a Roslyn szamara is), ezert a primitiv naplozas.
                //

                for (Exception? current = ex; current is not null; current = current.InnerException)
                {
                    if (current != ex) log.Write($"{Environment.NewLine}->{Environment.NewLine}");
                    log.Write(current.ToString());

                    foreach (object? key in current.Data.Keys) 
                    {
                        log.Write($"{Environment.NewLine}{key}:{Environment.NewLine}{current.Data[key]}");
                    }
                }

                log.Flush();
            }
            catch { }

            return CreateDiagnostic
            (
                "PGE00", 
                SGResources.TE_FAILED, 
                string.Format
                (
                    SGResources.Culture, 
                    SGResources.TE_FAILED_FULL, 
                    ex.Message, 
                    logFile ?? "NULL"
                ), 
                location, 
                DiagnosticSeverity.Warning
            );
        }

        internal static Diagnostic CreateDiagnostic(string id, string msg, string fullMsg, Location location, DiagnosticSeverity severity) => Diagnostic.Create
        (
            new DiagnosticDescriptor(id, msg, fullMsg, SGResources.TE, severity, true),
            location
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
            //
            // Csak C#-t tamogatjuk
            //

            if (context.ParseOptions.Language != CSharpParseOptions.Default.Language)
                return;

            Compilation compilation = context.Compilation;

            foreach (INamedTypeSymbol generator in GetAOTGenerators(compilation))
            {
                try
                {
                    string? generatorFullName = generator.GetQualifiedMetadataName();

                    ICodeFactory codeFactory = CodeFactories.SingleOrDefault(cf => cf.GeneratorFullName == generatorFullName) ?? throw new InvalidOperationException
                    (
                        string.Format
                        (
                            SGResources.Culture, 
                            SGResources.NOT_A_GENERATOR, 
                            generator
                        )
                    );

                    foreach ((string Hint, string Value) in codeFactory.GetSourceCodes(generator, context))
                    {
                        context.AddSource
                        (
                            Hint,
                            Value
                        );

                        context.ReportDiagnostic
                        (
                            CreateDiagnostic
                            (
                                "PGI00",
                                SGResources.SRC_EXTENDED,
                                string.Format
                                (
                                    SGResources.Culture,
                                    SGResources.SRC_EXTENDED_FULL,
                                    Hint
                                ),
                                generator.Locations.Single(),
                                DiagnosticSeverity.Info
                            )
                        );
                    }
                }
                catch (Exception e) 
                {
                    context.ReportDiagnostic
                    (
                        CreateDiagnosticAndLog(e, generator.Locations.Single())
                    );
                }
            }
        }
    }
}
