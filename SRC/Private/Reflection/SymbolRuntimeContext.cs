/********************************************************************************
* SymbolRuntimeContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolRuntimeContext : IRuntimeContext
    {
        public Compilation Compilation { get; }

        private SymbolRuntimeContext(Compilation compilation) => Compilation = compilation;

        public static IRuntimeContext CreateFrom(Compilation compilation) => new SymbolRuntimeContext(compilation);

        public ITypeInfo? GetTypeByQualifiedName(string name)
        {
            ITypeSymbol? type = Compilation.GetTypeByMetadataName(name);
            return type is not null
                ? SymbolTypeInfo.CreateFrom(type, Compilation)
                : null;
        }
    }
}
