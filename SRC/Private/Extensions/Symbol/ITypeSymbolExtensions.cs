/********************************************************************************
* ITypeSymbolExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class ITypeSymbolExtensions
    {
        public static bool IsInterface(this ITypeSymbol src) => src.TypeKind == TypeKind.Interface;

        public static string GetFriendlyName(this ITypeSymbol src) => src switch
        {
            _ when src.IsTupleType => $"{src.ContainingNamespace}.{src.Name}", // ne "(T Item1, TT item2)" formaban legyen
            _ when src is INamedTypeSymbol named && named.IsBoundNullable() => named.ConstructedFrom.GetFriendlyName(),
            _ when src.IsNested() => src.ToDisplayString
            (
                new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly)
            ),
            _ => src.ToDisplayString
            (
                new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)
            )
        };

        public static bool IsBoundNullable(this INamedTypeSymbol src) => src.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T && !SymbolEqualityComparer.Default.Equals(src.ConstructedFrom, src); // !src.IsGenericTypeDefinition() baszik itt mukodni

        public static bool IsGenericTypeDefinition(this INamedTypeSymbol src) => src.IsUnboundGenericType;
/*
        public static IEnumerable<ITypeSymbol> GetOwnGenericArguments(this INamedTypeSymbol src) => src.TypeArguments;
*/
        public static IEnumerable<ITypeSymbol> GetParents(this ITypeSymbol src) 
        {
            for (ITypeSymbol parent = src; (parent = parent.ContainingType) != null;)
            {
                yield return parent;
            }
        }

        public static IEnumerable<ITypeSymbol> GetEnclosingTypes(this ITypeSymbol src) => src.GetParents().Reverse();

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol src)
        {
            for (ITypeSymbol? baseType = src.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                yield return baseType;
            }
        }

        public static IEnumerable<TMember> ListMembers<TMember>(this ITypeSymbol src, bool includeNonPublic = false, bool includeStatic = false) where TMember : ISymbol
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

            static IEnumerable<TMember> GetMembers(ITypeSymbol t) => t
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

        public static ITypeSymbol? GetElementType(this ITypeSymbol src, bool recurse = false)
        {
            ITypeSymbol? prev = null;

            for (ITypeSymbol? current = src; (current = (current as IArrayTypeSymbol)?.ElementType ?? (current as IPointerTypeSymbol)?.PointedAtType) != null;)
            {
                if (!recurse) return current;
                prev = current;
            }

            return prev;
        }

        public static string? GetAssemblyQualifiedName(this ITypeSymbol src)
        {
            string? metadataName = src.GetQualifiedMetadataName();
            if (metadataName is null) return null;

            IAssemblySymbol? containingAsm = src.GetElementType(recurse: true)?.ContainingAssembly /* tombnek nincs tartalmazo szerelvenye forditaskor */ ?? src.ContainingAssembly;
            if (containingAsm is null) return null;

            return $"{metadataName}, {containingAsm.Identity}";
        }

        public static bool IsGenericArgument(this ITypeSymbol src) => src.ContainingType?.TypeArguments.Contains(src, SymbolEqualityComparer.Default) == true;

        public static bool IsNested(this ITypeSymbol src) => src.ContainingType is not null && !src.IsGenericParameter();

        public static bool IsGenericParameter(this ITypeSymbol src)
        {
            src = src.GetElementType(recurse: true) ?? src;

            return
                src.ContainingType is not null &&
                src.BaseType is null &&
                src.SpecialType is not SpecialType.System_Object &&
                !src.IsInterface();
        }

        public static string? GetQualifiedMetadataName(this ITypeSymbol src)
        {
            ITypeSymbol? elementType = src.GetElementType(recurse: true);

            //
            // Specialis eset h reflexiohoz hasonloan mukodjunk:
            //
            // class Generic<T>
            // {
            //    void Foo(List<T>[] para) {}
            // }
            //
            // Ez esetben "para" tipus-neve NULL kell legyen
            //

            if (elementType?.IsGenericParameter() == true || (elementType is INamedTypeSymbol namedElement && namedElement.TypeArguments.Any(IsGenericParameter))) 
                return null;

            var sb = new StringBuilder();

            INamespaceSymbol? ns = elementType?.ContainingNamespace ?? src.ContainingNamespace;  // tombnek, mutatonak nincs tartalmazo nevtere forditaskor

            if (ns is not null && !ns.IsGlobalNamespace)
            {
                sb.Append(ns);
                sb.Append(Type.Delimiter);
            }

            foreach (ITypeSymbol enclosingType in src.GetEnclosingTypes())
            {
                sb.Append($"{GetName(enclosingType)}+");
            }

            sb.Append($"{GetName(src)}");

            return sb.ToString();

            static string GetName(ITypeSymbol type) 
            {
                string name = !string.IsNullOrEmpty(type.Name)
                    ? type.Name // tupple es nullable eseten is helyes nevet ad vissza
                    : type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly)); // "atlagos" tipusoknal a nev ures

                return type is INamedTypeSymbol named && named.TypeArguments.Any() // ne IsGenericType legyen
                    ? $"{name}`{named.Arity}"
                    : name;
            }
        }
    }
}
