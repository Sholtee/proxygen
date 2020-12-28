/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;
    using Attributes;
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

                using var writer = new Utf8JsonWriter(File.OpenWrite(logFile));

                JsonSerializer.Serialize(writer, ex, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IgnoreReadOnlyProperties = false
                });

                writer.Flush();
            }
            catch { }

            return CreateDiagnostic ("PG01", SGResources.TE_FAILED, string.Format(SGResources.Culture, SGResources.TE_FAILED_FULL, ex.Message, logFile ?? "NULL"), location);
        }

        internal static Diagnostic CreateDiagnostic(string id, string msg, string fullMsg, Location location) => Diagnostic.Create
        (
            new DiagnosticDescriptor(id, msg, fullMsg, SGResources.TE, DiagnosticSeverity.Warning, true),
            location
        );

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (INamedTypeSymbol generator in GetAOTGenerators(context.Compilation))
            {
                if (!generator.OriginalDefinition.GetBaseTypes().Any(bt => bt.GetQualifiedMetadataName() == typeof(TypeGenerator<>).FullName))
                {
                    context.ReportDiagnostic
                    (
                        CreateDiagnostic("PG00", SGResources.NOT_A_GENERATOR, string.Format(SGResources.Culture, SGResources.NOT_A_GENERATOR_FULL, generator), generator.Locations.Single())
                    );
                    continue;
                }

                try
                {
                    // TODO
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
