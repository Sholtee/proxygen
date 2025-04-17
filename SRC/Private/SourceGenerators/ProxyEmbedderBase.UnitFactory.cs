/********************************************************************************
* ProxyEmbedderBase.UnitFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract partial class ProxyEmbedderBase
    {
        private static IReadOnlyDictionary<string, ISupportsSourceGeneration> SourceFactories { get; } = GetSourceFactories();

        private static IReadOnlyDictionary<string, ISupportsSourceGeneration> GetSourceFactories()
        {
            Dictionary<string, ISupportsSourceGeneration> result = [];

            foreach (Type type in typeof(Generator).Assembly.GetExportedTypes())
            {
                SupportsSourceGenerationAttributeBase? ssg = type.GetCustomAttribute<SupportsSourceGenerationAttributeBase>();
                if (ssg is null)
                    continue;

                try
                {
                    result.Add(type.FullName, ssg);
                }
                catch {} // Don't throw in initialization phase
            }

            return result;
        }

        private static SyntaxFactoryContext CreateContext(CSharpCompilation compilation, Config config) => new SyntaxFactoryContext
        {
            OutputType = OutputType.Unit,
            LanguageVersion = compilation.LanguageVersion,
            AssemblyNameOverride = compilation.Assembly.Name,

            //
            // Collecting references required only when dumping the source
            //

            ReferenceCollector = config.SourceDump is not null
                ? new ReferenceCollector()
                : null,
            Config = config
        };

        protected static ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, Config config)
        {
            return SourceFactories.TryGetValue(generator.GetQualifiedMetadataName()!, out ISupportsSourceGeneration ssg)
                ? ssg.CreateMainUnit
                (
                    generator,
                    compilation,
                    CreateContext(compilation, config)
                )
                : throw new InvalidOperationException
                (
                    string.Format
                    (
                        SGResources.Culture,
                        SGResources.NOT_A_GENERATOR,
                        generator
                    )
                );
        }

        protected static IEnumerable<UnitSyntaxFactoryBase> CreateChunks(CSharpCompilation compilation, Config config)
        {
            INamedTypeSymbol? miaSym = compilation.GetTypeByMetadataName(typeof(ModuleInitializerAttribute).FullName);

            if (miaSym is not null)
            {
                //
                // ModuleInitializerAttribute might be defined by te user, too.
                //

                if (miaSym.DeclaredAccessibility is Accessibility.Public)
                    yield break;

                if 
                (
                    miaSym.DeclaredAccessibility is Accessibility.Internal &&     
                    (
                        SymbolEqualityComparer.Equals(miaSym.ContainingAssembly, compilation.Assembly) || 
                        miaSym.ContainingAssembly.GivesAccessTo(compilation.Assembly)
                    )
                ) 
                    yield break;
            }

            yield return new ModuleInitializerSyntaxFactory
            (
                CreateContext(compilation, config)
            );
        }
    }
}
