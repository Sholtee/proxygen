/********************************************************************************
* ProxyCodeFactory.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Generators;

    internal sealed class ProxyCodeFactory : ICodeFactory
    {
        public string GeneratorFullName { get; } = typeof(ProxyGenerator<,>).FullName;

        public IEnumerable<SourceCode> GetSourceCodes(INamedTypeSymbol generator, GeneratorExecutionContext context)
        {
            ITypeSymbol
                iface = generator.TypeArguments[0],
                interceptor = generator.TypeArguments[1];

            Compilation compilation = context.Compilation;

            try
            {
                IUnitSyntaxFactory unitSyntaxFactory = new ProxySyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(iface, compilation),
                    SymbolTypeInfo.CreateFrom(interceptor, compilation),
                    OutputType.Unit
                );

                return new[] 
                {
                    unitSyntaxFactory.GetSourceCode($"{unitSyntaxFactory.DefinedClasses.Single()}.cs", context.CancellationToken)
                };
            }
            catch (Exception e) 
            {
                e.Data[nameof(iface)] = iface.GetDebugString();
                e.Data[nameof(interceptor)] = interceptor.GetDebugString();

                throw;
            }
        }
    }
}
