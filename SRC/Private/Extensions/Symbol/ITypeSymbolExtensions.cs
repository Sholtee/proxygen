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
                new SymbolDisplayFormat
                (
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
                )
            ),
            _ => src.ToDisplayString
            (
                new SymbolDisplayFormat
                (
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
                )
            )
        };

        public static bool IsBoundNullable(this INamedTypeSymbol src) => src.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T && !SymbolEqualityComparer.Default.Equals(src.ConstructedFrom, src); // !src.IsGenericTypeDefinition() baszik itt mukodni

        public static bool IsGenericType(this ITypeSymbol src) => src is INamedTypeSymbol named && named.TypeArguments.Any();

        public static bool IsGenericTypeDefinition(this INamedTypeSymbol src) => src.TypeArguments.Any(ta => ta.IsGenericParameter()); //src.IsUnboundGenericType;
/*
        public static IEnumerable<ITypeSymbol> GetOwnGenericArguments(this INamedTypeSymbol src) => src.TypeArguments;
*/
        public static IEnumerable<ITypeSymbol> GetParents(this ITypeSymbol src) 
        {
            src = src.GetElementType(recurse: true) ?? src; // tombnek pl nincs tartalmazo tipusa, mig a tomb elemnek van

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

        public static IEnumerable<IMethodSymbol> ListMethods(this ITypeSymbol src, bool includeStatic = false) => src.ListMembersInternal<IMethodSymbol>
        (
            m => m.GetAccessModifiers(),

            //
            // Metodus visszaterese lenyegtelen, csak a nev, parameter tipusa es atadasa kell
            //

            m =>
            {
                var hk = new HashCode();

                foreach (var descr in m.Parameters.Select(p => new { p.Type, ParameterKind = p.GetParameterKind() }))
                {
                    hk.Add(descr);
                }

                return new
                {
                    m.Name,
                    ParamzHash = hk.ToHashCode()
                };
            },
            includeStatic
        );

        public static IEnumerable<IPropertySymbol> ListProperties(this ITypeSymbol src, bool includeStatic = false) => src.ListMembersInternal<IPropertySymbol>
        (
            //
            // A nagyobb lathatosagut tekintjuk mervadonak
            //

            p => (AccessModifiers) Math.Max((int) (p.GetMethod?.GetAccessModifiers() ?? AccessModifiers.Unknown), (int) (p.SetMethod?.GetAccessModifiers() ?? AccessModifiers.Unknown)),
            p => p.Name, // nem kell StrippedName()
            includeStatic
        );

        public static IEnumerable<IEventSymbol> ListEvents(this ITypeSymbol src, bool includeStatic = false) => src.ListMembersInternal<IEventSymbol>
        (
            e => (e.AddMethod ?? e.RemoveMethod)?.GetAccessModifiers() ?? AccessModifiers.Unknown,
            e => e.Name, // nem kell StrippedName()
            includeStatic
        );

        private static IEnumerable<TMember> ListMembersInternal<TMember>(
            this ITypeSymbol src, 
            Func<TMember, AccessModifiers> getVisibility,
            Func<TMember, object> getDescriptor,
            bool includeStatic) where TMember : ISymbol
        {
            if (src.IsInterface())
                return src.AllInterfaces.Append(src).SelectMany(GetMembers);

            var returnedMembers = new HashSet<object>();

            Func<TMember, bool> filter = m => getVisibility(m) > AccessModifiers.Private && returnedMembers.Add
            (
                getDescriptor(m)
            );

            if (!includeStatic)
                filter = filter.And(m => !m.IsStatic);

            //
            // Sorrend szamit: a leszarmazottaktol haladunk az os fele
            //
           
            return new[] { src }.Concat(src.GetBaseTypes()).SelectMany(GetMembers).Where(filter);

            static IEnumerable<TMember> GetMembers(ITypeSymbol t) => t
                .GetMembers()
                .OfType<TMember>();
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

        //
        // GetElementType()-os csoda azert kell mert beagyazott tipusbol kepzett (pl) tomb
        // mar nem beagyazott tipus.
        //

        public static bool IsNested(this ITypeSymbol src) => 
            (src.GetElementType(recurse: true)?.ContainingType ?? src.ContainingType) is not null && !src.IsGenericParameter();

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

            if (elementType?.IsGenericParameter() == true || (elementType is INamedTypeSymbol namedElement && namedElement.IsGenericTypeDefinition())) 
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

                return type is INamedTypeSymbol named && named.IsGenericType()
                    ? $"{name}`{named.Arity}"
                    : name;
            }
        }

        public static bool EqualsTo(this ITypeSymbol src, ITypeSymbol that) 
        {
            if (src.IsGenericParameter() != that.IsGenericParameter())
                return false;

            if (!GetByRefAttributes(src).Equals(GetByRefAttributes(that)))
                return false;

            //
            // Itt mar mindkettonek v van v nincs elem tipusa
            //

            ITypeSymbol? elA = src.GetElementType();

            if (elA is not null)
            {
                ITypeSymbol elB = that.GetElementType()!;
                return elA.EqualsTo(elB);
            }

            if (!src.IsGenericParameter())
                return SymbolEqualityComparer.Default.Equals(src, that);

            IEqualityComparer<ISymbol> comparer = SymbolEqualityComparer.Default;

            return src switch 
            {
                //
                // class ClassA<T>.Foo(T para) == class ClassB<TT>.Foo(TT para)
                //

                _ when src.ContainingSymbol is INamedTypeSymbol srcContainer && that.ContainingSymbol is INamedTypeSymbol thatContainer =>
                    srcContainer.TypeArguments.IndexOf(src, comparer) == thatContainer.TypeArguments.IndexOf(that, comparer),

                //
                // class ClassA.Foo<T>(T para) == class ClassB.Foo<TT>(TT para)
                //

                _ when src.ContainingSymbol is IMethodSymbol srcMethod && that.ContainingSymbol is IMethodSymbol thatMethod =>
                    srcMethod.TypeArguments.IndexOf(src, comparer) == thatMethod.TypeArguments.IndexOf(that, comparer),

                _ => false
            };

            static object GetByRefAttributes(ITypeSymbol t) => new
            {
                IsPointer = t is IPointerTypeSymbol,
                IsArray = t is IArrayTypeSymbol
            };
        }
    }
}
