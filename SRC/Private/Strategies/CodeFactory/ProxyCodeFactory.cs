/********************************************************************************
* ProxyCodeFactory.cs                                                           *
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

    internal sealed class ProxyCodeFactory : ICodeFactory
    {
        [ModuleInitializer]
        public static void Init() => ProxyEmbedder.CodeFactories.Add(new ProxyCodeFactory());

        public bool ShouldUse(INamedTypeSymbol generator) => generator.GetQualifiedMetadataName() == typeof(ProxyGenerator<,>).FullName;

        public IEnumerable<SourceCode> GetSourceCodes(INamedTypeSymbol generator, GeneratorExecutionContext context)
        {
            ITypeSymbol
                iface = generator.TypeArguments[0],
                interceptor = generator.TypeArguments[1];

            Compilation compilation = context.Compilation;

            SourceCode result;

            try
            {
                IUnitSyntaxFactory unitSyntaxFactory = new ProxySyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(iface, compilation),
                    SymbolTypeInfo.CreateFrom(interceptor, compilation),
                    compilation.AssemblyName!,
                    OutputType.Unit,
                    SymbolTypeInfo.CreateFrom(generator, compilation)
                );

                result = unitSyntaxFactory.GetSourceCode(context.CancellationToken);
            }
            catch (Exception e) 
            {
                e.Data[nameof(iface)] = iface.GetDebugString();
                e.Data[nameof(interceptor)] = interceptor.GetDebugString();

                throw;
            }

            //
            // "yield" nem szerepelhet "try" blokkban
            //

            yield return result;
        }
    }
}
