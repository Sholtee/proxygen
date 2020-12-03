/********************************************************************************
* INamedTypeSymbolExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class INamedTypeSymbolExtensions
    {
        public static bool IsInterface(this INamedTypeSymbol src) => src.TypeKind == TypeKind.Interface;

        public static string GetFriendlyName(this INamedTypeSymbol src) => src.ToDisplayString
        (
            new SymbolDisplayFormat(typeQualificationStyle: src.ContainingType is not null 
                ? SymbolDisplayTypeQualificationStyle.NameOnly 
                : SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)
        );

        public static bool IsGenericTypeDefinition(this INamedTypeSymbol src) => src.IsUnboundGenericType;
/*
        public static IEnumerable<ITypeSymbol> GetOwnGenericArguments(this INamedTypeSymbol src) => src.TypeArguments;
*/
        public static IEnumerable<INamedTypeSymbol> GetParents(this INamedTypeSymbol src) 
        {
            for (INamedTypeSymbol parent = src; (parent = parent.ContainingType) != null;)
            {
                yield return parent;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetEnclosingTypes(this INamedTypeSymbol src) => src.GetParents().Reverse();

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this INamedTypeSymbol src)
        {
            for (INamedTypeSymbol? baseType = src.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                yield return baseType;
            }
        }

        public static IEnumerable<TMember> ListMembers<TMember>(this INamedTypeSymbol src, bool includeNonPublic = false, bool includeStatic = false) where TMember : ISymbol
        {
            if (src.IsInterface())
                return src.AllInterfaces.Append(src).SelectMany(GetMembers);

            //
            // Nem publikus es statikus tagok nem tartozhatnak interface-ekhez
            //

            Func<TMember, bool> filter = m => !m.IsOverride;

            if (!includeNonPublic)
                filter = filter.And(m => m.DeclaredAccessibility == Accessibility.Public);

            if (!includeStatic)
                filter = filter.And(m => !m.IsStatic);

            return src.GetBaseTypes().Append(src).SelectMany(GetMembers).Where(filter);

            static IEnumerable<TMember> GetMembers(INamedTypeSymbol t) => t
                .GetMembers()
                .OfType<TMember>();
        }

        public static IEnumerable<IMethodSymbol> GetPublicConstructors(this INamedTypeSymbol src)
        {
            IEnumerable<IMethodSymbol> constructors = src
                .InstanceConstructors
                .Where(ctor => ctor.DeclaredAccessibility == Accessibility.Public);

            if (!constructors.Any())
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NO_PUBLIC_CTOR, src.Name));

            return constructors;
        }

        public static INamedTypeSymbol GetElementType(this INamedTypeSymbol src) => (INamedTypeSymbol) ((src as IArrayTypeSymbol)?.ElementType ?? (src as IPointerTypeSymbol)?.PointedAtType ?? src);

        public static string? GetAssemblyQualifiedName(this INamedTypeSymbol src) 
        {
            if (src.ContainingAssembly is null) return null;

            return $"{src.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))}, {src.ContainingAssembly.Identity}";
        }
    }
}
