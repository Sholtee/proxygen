﻿/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        internal static string? LogException(Exception ex, in CancellationToken cancellation)
        {
            try
            {
                Directory.CreateDirectory(WorkingDirectories.Instance.LogDump);

                string logFile = Path.Combine(WorkingDirectories.Instance.LogDump, $"ProxyGen_{Guid.NewGuid()}.log");

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

        internal static void ReportError(in GeneratorExecutionContext context, Exception ex, Location location) => context.ReportDiagnostic
        (
            Diagnostics.PGE01(location, ex.Message, LogException(ex, context.CancellationToken) ?? "NULL")
        );

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        //
        // xXxCodeFactory-k toltik fel...
        //

        public static ICollection<ICodeFactory> CodeFactories { get; } = new HashSet<ICodeFactory>();

        public void Execute(GeneratorExecutionContext context)
        {
            IConfigReader configReader = new AnalyzerConfigReader(context);
#if DEBUG
            if (configReader.ReadValue("DebugGenerator")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            {
                Debugger.Launch();
            }
#endif
            Compilation compilation = context.Compilation;

            IEnumerable<INamedTypeSymbol> aotGenerators = GetAOTGenerators(compilation);

            //
            // Csak C#-t tamogatjuk
            //

            if (compilation.Language != CSharpParseOptions.Default.Language)
            {
                //
                // Viszont visszajelzes csak akkor kell ha a kod hasznalna is a generatort
                //

                if (aotGenerators.Any()) context.ReportDiagnostic
                (
                    Diagnostics.PGE00(Location.None)
                );
                return;
            }

            WorkingDirectories.Setup(configReader);
            
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

                        context.ReportDiagnostic
                        (
                            Diagnostics.PGI00(generator.Locations.Single(), source.Hint)
                        );
                    }
                }
                catch (InvalidSymbolException)
                {
                    //
                    // Ugras a kovetkezo generatorra
                    //
                }
                catch (Exception e)
                {
                    ReportError
                    (
                        context, 
                        e, 
                        generator.Locations.Single()
                    );
                }
            }
        }
    }
}
