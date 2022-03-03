/********************************************************************************
* DuckCodeFactory.cs                                                            *
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

    internal sealed class DuckCodeFactory : ICodeFactory
    {
        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        #pragma warning restore CA2255
        public static void Init() => ProxyEmbedder.CodeFactories.Add(new DuckCodeFactory());

        public bool ShouldUse(INamedTypeSymbol generator) => generator.GetQualifiedMetadataName() == typeof(DuckGenerator<,>).FullName;

        public IEnumerable<SourceCode> GetSourceCodes(INamedTypeSymbol generator, GeneratorExecutionContext context)
        {
            ITypeSymbol
                iface = generator.TypeArguments[0],
                target = generator.TypeArguments[1];

            Compilation compilation = context.Compilation;

            SourceCode result;

            try
            {
                result = new DuckSyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(iface, compilation),
                    SymbolTypeInfo.CreateFrom(target, compilation),
                    compilation.AssemblyName!,
                    OutputType.Unit,
                    SymbolAssemblyInfo.CreateFrom(generator.ContainingAssembly, compilation),

                    //
                    // Ha nem kell dump-olni a referenciakat akkor felesleges oket osszegyujteni
                    //

                    !string.IsNullOrEmpty(WorkingDirectories.Instance.SourceDump) ? new ReferenceCollector() : null
                ).GetSourceCode(context.CancellationToken);
            }
            catch (Exception e) 
            {
                e.Data[nameof(iface)] = iface.GetDebugString();
                e.Data[nameof(target)] = target.GetDebugString();

                throw;
            }

            //
            // "yield" nem szerepelhet "try" blokkban
            //

            yield return result;
        }
    }
}
