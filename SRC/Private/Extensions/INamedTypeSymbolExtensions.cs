/********************************************************************************
* INamedTypeSymbolExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal static class INamedTypeSymbolExtensions
    {
        public static bool IsInterface(this INamedTypeSymbol src) => src.TypeKind == TypeKind.Interface;
    }
}
