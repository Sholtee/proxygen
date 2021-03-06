﻿/********************************************************************************
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
    internal static class ITypeSymbolExtensions
    {
        public static bool IsInterface(this ITypeSymbol src) => src.TypeKind == TypeKind.Interface;

        private static readonly IReadOnlyList<TypeKind> ClassTypes = new[] 
        {
            TypeKind.Class,
            TypeKind.Array,
            TypeKind.Delegate,
            TypeKind.Pointer
        };

        public static bool IsClass(this ITypeSymbol src) => ClassTypes.Contains(src.TypeKind);

        private static readonly IReadOnlyList<TypeKind> SealedTypes = new[]
        {
            TypeKind.Array
        };

        public static bool IsFinal(this ITypeSymbol src) => src.IsSealed || src.IsStatic || SealedTypes.Contains(src.TypeKind);

        public static string GetFriendlyName(this ITypeSymbol src) => src switch
        {
            _ when src.IsTupleType || src.IsNativeIntegerType => $"{src.ContainingNamespace}.{src.Name}", // ne "(T Item1, TT item2)" formaban legyen
            _ when src is INamedTypeSymbol named && named.IsBoundNullable() => named.ConstructedFrom.GetFriendlyName(),
            _ when src is IPointerTypeSymbol pointer => pointer.PointedAtType.GetFriendlyName(),
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

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol src)
        {
            for (ITypeSymbol? baseType = src.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                yield return baseType;
            }
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

                foreach (var descr in m.Parameters.Select(p => new { TypeHash = p.Type.GetUniqueHashCode(), ParameterKind = p.GetParameterKind() }))
                {
                    hk.Add(descr);
                }

                return new
                {
                    m.Name,
                    m.TypeArguments.Length,
                    ParamzHash = hk.ToHashCode()
                };
            },
            includeStatic
        );

        public static int GetUniqueHashCode(this ITypeSymbol src) => src switch 
        {
            //
            // symbolof(int32*) == symbolof(int[]). Ebbol fakadoan pl symbolof(List<int*>) == symbolof(List<int[]>) 
            // Lasd: PointersAndArrays_ShouldBeConsideredEqual teszt
            //

            IArrayTypeSymbol array => new 
            { 
                Extra = typeof(IArrayTypeSymbol), 
                TypeHash = array.ElementType.GetUniqueHashCode() 
            }.GetHashCode(),
            IPointerTypeSymbol pointer => new 
            { 
                Extra = typeof(IPointerTypeSymbol), 
                TypeHash = pointer.PointedAtType.GetUniqueHashCode() 
            }.GetHashCode(),

            //
            // symbolof(List<T>) == symbolof(T), lehet en vagyok a fasz de ennek tenyleg igy kene lennie
            // Lasd: GenericParameterAndItsDeclaringGeneric_ShouldBeConsideredEqual teszt
            //

            _ when src.IsGenericParameter() => src.GetGenericParameterIndex()!.Value,

            _ => src.GetHashCode()
        };

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

            //
            // Tombnek es mutatonak nincs tartalmazo szerelvenye.
            //

            IAssemblySymbol? containingAsm = src.GetElementType(recurse: true)?.ContainingAssembly ?? src.ContainingAssembly;
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

            StringBuilder sb = new();

            INamespaceSymbol? ns = elementType?.ContainingNamespace ?? src.ContainingNamespace;  // tombnek, mutatonak nincs tartalmazo nevtere forditaskor

            if (ns is not null && !ns.IsGlobalNamespace)
            {
                sb.Append(ns);
                sb.Append(Type.Delimiter);
            }

            foreach (ITypeSymbol enclosingType in src.GetParents().Reverse())
            {
                sb.Append($"{GetName(enclosingType)}+");
            }

            sb.Append($"{GetName(elementType ?? src)}");

            return sb.ToString();

            static string GetName(ITypeSymbol type) 
            {
                string name = !string.IsNullOrEmpty(type.Name)
                    ? type.Name // tupple es nullable eseten is helyes nevet ad vissza
                    : type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly)); // "atlagos" tipusoknal a Name ures

                return type is INamedTypeSymbol named && named.IsGenericType()
                    ? $"{name}`{named.Arity}"
                    : name;
            }
        }

        public static int? GetGenericParameterIndex(this ITypeSymbol src) 
        {
            if (!src.IsGenericParameter())
                return null;

            IEqualityComparer<ISymbol> comparer = SymbolEqualityComparer.Default;

            return src switch
            {
                //
                // class ClassA<T>.Foo(T para)
                //

                _ when src.ContainingSymbol is INamedTypeSymbol srcContainer =>
                    srcContainer.TypeArguments.IndexOf(src, comparer),

                //
                // class ClassA.Foo<T>(T para)
                //

                _ when src.ContainingSymbol is IMethodSymbol srcMethod =>
                    srcMethod.TypeArguments.IndexOf(src, comparer) * -1, // ha a parameter metoduson van definialva akkor negativ szam

                _ => null
            };
        }

        public static IEnumerable<ITypeSymbol> GetAllInterfaces(this ITypeSymbol src) 
        {
            //
            // AllInterfaces-ben egy generikus interface szerepelhet tobbszor ha a tipus argumentumok csak a Nullable
            // ertekukben ternek el:
            //
            // interface IA: IB, IC<string> {}, interface IB: IC<string?> -> ekkor IC<string> ketszer fog szerepelni
            //

            return src.AllInterfaces.Select(WithoutNullableAnnotations).Distinct(SymbolEqualityComparer.Default).Cast<ITypeSymbol>();

            static ITypeSymbol WithoutNullableAnnotations(INamedTypeSymbol type)
            {
                if (type.IsGenericType())
                {
                    INamedTypeSymbol genericDef = type.OriginalDefinition;
                    type = genericDef.Construct
                    (
                        type
                            .TypeArguments
                            .Select(ta => !ta.IsValueType // int? -> Nullable<int>, object? -> [Nullable] object
                                ? ta.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                                : ta)
                            .ToArray()
                    );
                }

                return type;
            }
        }

        public static string GetDebugString(this ITypeSymbol src, string? eol = null) 
        {
            var fmt = new SymbolDisplayFormat
            (
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeRef,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

            eol ??= Environment.NewLine;

            var sb = new StringBuilder().Append(src.ToDisplayString(fmt));

            IEnumerable<ITypeSymbol> bases = src.GetBaseTypes().Concat(src.AllInterfaces);

            if (bases.Any())
                sb.Append($": {string.Join(", ", bases.Select(@base => @base.ToDisplayString(fmt)))}");

            sb.Append($"{eol}{{");

            foreach (IMethodSymbol method in src.ListMethods(includeStatic: true).Where(m => !m.IsSpecial()))
                sb.Append($"{eol}  {method.ToDisplayString(fmt)};");

            foreach (IPropertySymbol property in src.ListProperties(includeStatic: true))
                sb.Append($"{eol}  {property.ToDisplayString(fmt)}");

            foreach (IEventSymbol evt in src.ListEvents(includeStatic: true))
                sb.Append($"{eol}  {evt.ToDisplayString(fmt)}");

            sb.Append($"{eol}}}");

            return sb.ToString();
        }
    }
}
