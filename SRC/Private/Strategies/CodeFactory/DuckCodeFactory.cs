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

            //
            // A ProxyGen szerelvenyenek mindenkepp hivatkozva kell lennie
            //

            IAssemblySymbol? self = compilation.GetAssemblyByLocation(typeof(DuckCodeFactory).Assembly.Location);
            if (self is null)
                yield break;

            SourceCode result;

            try
            {
                IUnitSyntaxFactory unitSyntaxFactory = new DuckSyntaxFactory
                (
                    SymbolTypeInfo.CreateFrom(iface, compilation),
                    SymbolTypeInfo.CreateFrom(target, compilation),
                    SymbolAssemblyInfo.CreateFrom(self, compilation),
                    compilation.AssemblyName!,
                    OutputType.Unit
                );

                result = unitSyntaxFactory.GetSourceCode($"{unitSyntaxFactory.DefinedClasses.Single()}.cs", context.CancellationToken);
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
