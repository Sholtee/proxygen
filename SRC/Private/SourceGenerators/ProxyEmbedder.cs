/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using static System.Environment;

namespace Solti.Utils.Proxy.Internals
{
    using Attributes;

    #pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
    [Generator(LanguageNames.CSharp)]
    #pragma warning restore CS3016
    internal sealed partial class ProxyEmbedder : ISourceGenerator
    {
        private static readonly SymbolEqualityComparer SymbolEqualityComparer  = SymbolEqualityComparer.Default;

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

        //
        // A SourceGenerator a leheto legkevesebb fuggoseget kell hivatkozza (mivel azokat mind hivatkozni kell
        // a Roslyn szamara is), ezert a primitiv naplozas.
        //

        internal static string? LogException(Exception ex, in CancellationToken cancellation)
        {
            string? logDump = WorkingDirectories.Instance.LogDump;
            if (logDump is not null)
            {
                try
                {
                    Directory.CreateDirectory(logDump);

                    string logFile = Path.Combine(logDump, $"ProxyGen_{Guid.NewGuid()}.log");

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
                #pragma warning disable CA1031 // This method should never throw.
                catch {}
                #pragma warning restore CA1031
            }
            return null;
        }

        internal static void ReportError(in GeneratorExecutionContext context, Exception ex, Location location) => context.ReportDiagnostic
        (
            Diagnostics.PGE01(location, ex.Message, LogException(ex, context.CancellationToken) ?? "NULL")
        );

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //
            // Csak C# 7.0+ tamogatjuk
            //

            if (context.Compilation is not CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp7 } compilation)
            {
                context.ReportDiagnostic
                (
                    Diagnostics.PGE00(Location.None)
                );
                return;
            }

            IConfigReader configReader = new AnalyzerConfigReader(context);

            WorkingDirectories.Setup(configReader);
            SourceGeneratorConfig.Setup(configReader);

            #if DEBUG
            if (SourceGeneratorConfig.Instance.DebugGenerator)
                Debugger.Launch();
            #endif

            IEnumerable<INamedTypeSymbol> aotGenerators = GetAOTGenerators(compilation);
            if (!aotGenerators.Some())
                return;

            int extensionCount = 0;

            foreach (INamedTypeSymbol generator in aotGenerators)
            {
                try
                {
                    generator.EnsureNotError();

                    ExtendWith
                    (
                        CreateMainUnit(generator, compilation),
                        generator.Locations.Single()!
                    );

                    extensionCount++;
                }
                catch (InvalidSymbolException)
                {
                    //
                    // Ugras a kovetkezo generatorra. Megjegyzendo h nem csak a "generatorSymbol" lehet hibas u h
                    // ez a catch jo helyen van itt.
                    //
                }
                #pragma warning disable CA1031 // We want to report all non symbol related exceptions.
                catch (Exception e)
                #pragma warning restore CA1031
                {
                    ReportError(context, e, generator.Locations.Single()!);
                }
            }

            //
            // Csak ha van is ertelme akkor adjuk hozza a Chunk-okat
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
                    ReportError(context, e, Location.None);
                    return;
                }
            }

            void ExtendWith(UnitSyntaxFactoryBase unit, Location location)
            {
                SourceCode source = unit.GetSourceCode(context.CancellationToken);

                context.AddSource
                (
                    source.Hint,
                    source.Value
                );

                context.ReportDiagnostic
                (
                    Diagnostics.PGI00(location, source.Hint)
                );
            }
        }
    }
}
