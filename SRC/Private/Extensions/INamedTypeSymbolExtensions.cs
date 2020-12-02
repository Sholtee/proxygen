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

        public static string GetFriendlyName(this INamedTypeSymbol src)
        {
            if (src.ContainingType is not null) return src.Name;

            return $"{src.ContainingNamespace}.{src.Name}";
        }

        public static bool IsGenericTypeDefinition(this INamedTypeSymbol src) => src.IsUnboundGenericType;
/*
        public static IEnumerable<ITypeSymbol> GetOwnGenericArguments(this INamedTypeSymbol src) => src.TypeArguments;
*/
    }
}
