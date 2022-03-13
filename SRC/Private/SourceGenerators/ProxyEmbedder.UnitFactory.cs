/********************************************************************************
* ProxyEmbedder.UnitFactory.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Generators;
    using Properties;

    internal sealed partial class ProxyEmbedder
    {
        private static ReferenceCollector? CreateReferenceCollector() =>
            //
            // Ha nem kell dump-olni a referenciakat akkor felesleges oket osszegyujteni
            //

            !string.IsNullOrEmpty(WorkingDirectories.Instance.SourceDump) ? new ReferenceCollector() : null;

        private static ProxyUnitSyntaxFactory CreateMainUnit(INamedTypeSymbol generator, Compilation compilation)
        {
            string qualifiedName = generator.GetQualifiedMetadataName()!;

            return generator switch
            {
                _ when qualifiedName == typeof(DuckGenerator<,>).FullName => new DuckSyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
                    SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
                    compilation.Assembly.Name,
                    OutputType.Unit,
                    SymbolAssemblyInfo.CreateFrom(generator.ContainingAssembly, compilation),
                    CreateReferenceCollector()
                ),
                _ when qualifiedName == typeof(ProxyGenerator<,>).FullName => new ProxySyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
                    SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
                    compilation.Assembly.Name,
                    OutputType.Unit,
                    CreateReferenceCollector()
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
        }

        private static IEnumerable<UnitSyntaxFactoryBase> CreateChunks(Compilation compilation)
        {
            //
            // Ha nem kell dump-olni a referenciakat akkor felesleges oket osszegyujteni
            //

            ReferenceCollector? referenceCollector = !string.IsNullOrEmpty(WorkingDirectories.Instance.SourceDump) ? new() : null;

            INamedTypeSymbol? miaSym = compilation.GetTypeByMetadataName(typeof(ModuleInitializerAttribute).FullName);

            if (miaSym is not null)
            {
                //
                // ModuleInitializerAttribute-t mi magunk is deklaralhatunk, ezert a bonyolult vizsgalat.
                //

                if (miaSym.DeclaredAccessibility is Accessibility.Public)
                    yield break;

                if (miaSym.DeclaredAccessibility is Accessibility.Internal && (SymbolEqualityComparer.Equals(miaSym.ContainingAssembly, compilation.Assembly) || miaSym.ContainingAssembly.GivesAccessTo(compilation.Assembly)))
                    yield break;
            }

            yield return new ModuleInitializerSyntaxFactory(OutputType.Unit, CreateReferenceCollector());
        }
    }
}
