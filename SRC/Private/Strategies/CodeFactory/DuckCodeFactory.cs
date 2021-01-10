/********************************************************************************
* DuckCodeFactory.cs                                                            *
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

    internal sealed class DuckCodeFactory : ICodeFactory
    {
        public bool ShouldUse(INamedTypeSymbol generator) => generator.GetQualifiedMetadataName() == typeof(DuckGenerator<,>).FullName;

        public IEnumerable<SourceCode> GetSourceCodes(INamedTypeSymbol generator, GeneratorExecutionContext context)
        {
            ITypeSymbol
                iface = generator.TypeArguments[0],
                target = generator.TypeArguments[1];

            Compilation compilation = context.Compilation;

            try
            {
                IUnitSyntaxFactory unitSyntaxFactory = new DuckSyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(iface, compilation),
                    SymbolTypeInfo.CreateFrom(target, compilation),
                    compilation.AssemblyName!,
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
                e.Data[nameof(target)] = target.GetDebugString();

                throw;
            }
        }
    }
}
