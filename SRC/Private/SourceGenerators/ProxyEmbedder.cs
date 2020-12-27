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

    [Generator]
    internal class ProxyEmbedder: ISourceGenerator
    {
        private static IReadOnlyList<Type> TypeGenerators { get; } = typeof(ProxyEmbedder)
            .Assembly
            .GetTypes()
            .Where(t => t.BaseType?.IsGenericType == true && t.BaseType.GetGenericTypeDefinition() == typeof(TypeGenerator<>))
            .ToArray();

        internal static IEnumerable<INamedTypeSymbol> GetAOTGenerators(Compilation compilation) 
        {
            foreach(AttributeData attr in compilation.Assembly.GetAttributes().Where(attr => Is(attr.AttributeClass, typeof(EmbedGeneratedTypeAttribute))))
            {
                if (attr.ConstructorArguments.Single().Value is INamedTypeSymbol arg) 
                {   
                    INamedTypeSymbol genericTypeDefinition = arg.OriginalDefinition;

                    if (TypeGenerators.Any(generator => Is(genericTypeDefinition, generator)))
                        yield return arg;
                }
            }

            bool Is(ISymbol? s, Type t) => SymbolEqualityComparer.Default.Equals(s, compilation.GetTypeByMetadataName(t.FullName));
        }

        internal static Diagnostic CreateDiagnosticAndLog(Exception ex, Location location) 
        {
            string? logFile = null;

            try
            {
                logFile = Path.Combine(Path.GetTempPath(), $"ProxyGen-{Guid.NewGuid()}.log");

                using var writer = new Utf8JsonWriter(File.OpenWrite(logFile));

                JsonSerializer.Serialize(writer, ex, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IgnoreReadOnlyProperties = false
                });

                writer.Flush();
            }
            catch { }

            return Diagnostic.Create
            (
                new DiagnosticDescriptor("PG00", "Type embedding failed", $"Reason: {ex.Message} - Details stored in: {logFile ?? "NULL"}", "Type Embedding", DiagnosticSeverity.Warning, true),
                location
            );
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (INamedTypeSymbol generator in GetAOTGenerators(context.Compilation))
            {
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
