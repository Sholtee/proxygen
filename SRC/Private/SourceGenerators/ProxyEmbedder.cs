/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;
    using Generators;
    using Properties;

    [Generator]
    internal class ProxyEmbedder: ISourceGenerator
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

        public void Execute(GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;

            foreach (INamedTypeSymbol generator in GetAOTGenerators(compilation))
            {
                try
                {
                    string? generatorFullName = generator.GetQualifiedMetadataName();

                    IUnitSyntaxFactory unitSyntaxFactory = generatorFullName switch 
                    {
                        _ when generatorFullName == typeof(ProxyGenerator<,>).FullName => new ProxySyntaxFactory
                        (
                            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
                            SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
                            OutputType.Unit
                        ),
                        _ when generatorFullName == typeof(DuckGenerator<,>).FullName => new DuckSyntaxFactory
                        (
                            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
                            SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
                            compilation.AssemblyName!,
                            OutputType.Unit
                        ),
                        _ => throw new InvalidOperationException
                        (
                            string.Format
                            (
                                SGResources.Culture, 
                                SGResources.NOT_A_GENERATOR, 
                                generator
                            )
                        )
                    };

                    unitSyntaxFactory.Build(context.CancellationToken);

                    context.AddSource
                    (
                        $"{unitSyntaxFactory.DefinedClasses.Single()}.cs", 
                        unitSyntaxFactory.Unit!.NormalizeWhitespace(eol: Environment.NewLine).ToFullString()
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
                                SGResources.NOT_A_GENERATOR, 
                                unitSyntaxFactory.DefinedClasses.Single()
                            ),
                            generator.Locations.Single(),
                            DiagnosticSeverity.Info
                        )
                    );
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
