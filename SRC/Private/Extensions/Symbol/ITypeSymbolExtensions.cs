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
    using Properties;

    internal static class ITypeSymbolExtensions
    {
        public static bool IsInterface(this ITypeSymbol src) => src.TypeKind is TypeKind.Interface;

        private static readonly IReadOnlyList<TypeKind> ClassTypes =
        [
            TypeKind.Class,
            TypeKind.Array,
            TypeKind.Delegate,
            TypeKind.Pointer
        ];

        public static bool IsClass(this ITypeSymbol src) => ClassTypes.Contains(src.TypeKind);

        private static readonly IReadOnlyList<TypeKind> SealedTypes =
        [
            TypeKind.Array
        ];

        public static bool IsFinal(this ITypeSymbol src) => src.IsSealed || src.IsStatic || SealedTypes.Contains(src.TypeKind);

        public static ITypeSymbol? GetEnclosingType(this ITypeSymbol src) => (src.GetElementType(recurse: true) ?? src).ContainingType;

        public static string GetFriendlyName(this ITypeSymbol src) => src switch
        {
            //
            // nint => System.IntPtr, (T Item1, TT item2) => System.Tuple<T, TT>
            //

            { IsTupleType: true } or { IsNativeIntegerType: true } => src.ContainingNamespace.ToString() + Type.Delimiter + src.Name,

            INamedTypeSymbol named when named.IsBoundNullable() => named.ConstructedFrom.GetFriendlyName(),

            IPointerTypeSymbol pointer => pointer.PointedAtType.GetFriendlyName(),

            //
            // delegate*<T, TT> => TRetVal(T, TT)
            //

            IFunctionPointerTypeSymbol functionPointer =>
                $"{functionPointer.Signature.ReturnType.GetFriendlyName()}({string.Join(", ", functionPointer.Signature.Parameters.Select(static p => p.Type.GetFriendlyName()))})",

            //
            // nint[,] => System.IntPtr[,]
            //

            IArrayTypeSymbol array =>
                $"{array.ElementType.GetFriendlyName()}[{new string(Enumerable.Repeat(',', array.Rank - 1).ToArray())}]",

            _ => src.ToDisplayString
            (
                new SymbolDisplayFormat
                (
                    typeQualificationStyle: src.IsNested()
                        ? SymbolDisplayTypeQualificationStyle.NameOnly
                        : SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
                )
            )
        };

        public static bool IsBoundNullable(this INamedTypeSymbol src) =>
            src.ConstructedFrom?.SpecialType is SpecialType.System_Nullable_T &&
            !SymbolEqualityComparer.Default.Equals(src.ConstructedFrom, src);

        public static bool IsGenericType(this ITypeSymbol src) => src is INamedTypeSymbol named && named.TypeArguments.Any();

        public static IEnumerable<ITypeSymbol> GetParents(this ITypeSymbol src) 
        {
            src = src.GetElementType(recurse: true) ?? src; // Array has no containing type but array item does

            for (ITypeSymbol parent = src; (parent = parent!.ContainingType) is not null;)
            {
                yield return parent;
            }
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol src)
        {
            return src.IsInterface()
                ? src.GetAllInterfaces()
                : GetBaseTypes();

            IEnumerable<ITypeSymbol> GetBaseTypes()
            {
                for (ITypeSymbol? baseType = src.BaseType; baseType is not null; baseType = baseType.BaseType)
                {
                    yield return baseType;
                }
            }
        }

        public static IEnumerable<ITypeSymbol> GetHierarchy(this ITypeSymbol src)
        {
            yield return src;

            foreach (ITypeSymbol baseType in src.GetBaseTypes())
            {
                yield return baseType;
            }
        }

        public static IEnumerable<IMethodSymbol> ListMethods(this ITypeSymbol src, bool includeStatic = false, bool skipSpecial = true)
        {
            IEnumerable<IMethodSymbol> methods = src.ListMembersInternal<IMethodSymbol>
            (
                static m => m,
                static m => m.GetOverriddenMethod(),
                includeStatic
            );

            if (skipSpecial)
                methods = methods.Where(static m => !m.IsSpecial());

            return methods;
        }

        public static IEnumerable<IPropertySymbol> ListProperties(this ITypeSymbol src, bool includeStatic = false)
        {
            return src.ListMembersInternal<IPropertySymbol>
            (
                GetUnderlyingMethod,
                static p => GetUnderlyingMethod(p).GetOverriddenMethod(),
                includeStatic
            );

            //
            // Higher visibility has the precedence
            //

            static IMethodSymbol GetUnderlyingMethod(IPropertySymbol prop)
            {
                if (prop.GetMethod is null)
                    return prop.SetMethod!;

                if (prop.SetMethod is null)
                    return prop.GetMethod;

                return prop.GetMethod.GetAccessModifiers() > prop.SetMethod.GetAccessModifiers()
                    ? prop.GetMethod
                    : prop.SetMethod;
            }
        }

        public static IEnumerable<IEventSymbol> ListEvents(this ITypeSymbol src, bool includeStatic = false)
        {
            return src.ListMembersInternal<IEventSymbol>
            (
                GetUnderlyingMethod,
                static e => GetUnderlyingMethod(e).GetOverriddenMethod(),
                includeStatic
            );

            //
            // Higher visibility has the precedence
            //

            static IMethodSymbol GetUnderlyingMethod(IEventSymbol evt)
            {
                if (evt.AddMethod is null)
                    return evt.RemoveMethod!;

                if (evt.RemoveMethod is null)
                    return evt.AddMethod;

                return evt.AddMethod.GetAccessModifiers() > evt.RemoveMethod.GetAccessModifiers()
                    ? evt.AddMethod
                    : evt.RemoveMethod;
            }
        }

        private static IEnumerable<TMember> ListMembersInternal<TMember>
        (
            this ITypeSymbol src, 
            Func<TMember, IMethodSymbol> getUnderlyingMethod,
            Func<TMember, IMethodSymbol?> getOverriddenMethod,
            bool includeStatic
        ) where TMember : ISymbol
        {
            if (src.IsInterface())
                foreach (TMember member in GetMembers(src))
                    yield return member;
            else
            {
                #pragma warning disable RS1024
                HashSet<IMethodSymbol> overriddenMethods = new(SymbolEqualityComparer.Default);
                #pragma warning restore RS1024

                //
                // Order matters: we're processing the hierarchy towards the ancestor
                //

                foreach (TMember member in GetMembers(src))
                {
                    if (member.IsStatic && !includeStatic)
                        continue;

                    IMethodSymbol?
                        overriddenMethod = getOverriddenMethod(member),
                        underlyingMethod = getUnderlyingMethod(member);

                    if (overriddenMethod is not null)
                        overriddenMethods.Add(overriddenMethod);

                    if (overriddenMethods.Contains(underlyingMethod))
                        continue;

                    //
                    // If it was not yielded before (due to "new" or "override") and not private then we are fine.
                    //

                    if (underlyingMethod.GetAccessModifiers() > AccessModifiers.Private)
                        yield return member;
                }
            }

            static IEnumerable<TMember> GetMembers(ITypeSymbol src) => src
                .GetHierarchy()
                .SelectMany(static ts => ts.GetMembers())
                .OfType<TMember>();
        }

        public static IEnumerable<IMethodSymbol> GetConstructors(this ITypeSymbol src)
        {
            //
            // Don't use ListMembersInternal() here as we don't need ctors from the ancestors.
            //

            foreach (ISymbol m in src.GetMembers())
            {
                if (m is not IMethodSymbol ctor || ctor.MethodKind is not MethodKind.Constructor || ctor.IsImplicitlyDeclared)
                    continue;

                yield return ctor;
            }
        }

        public static ITypeSymbol? GetElementType(this ITypeSymbol src, bool recurse = false)
        {
            ITypeSymbol? prev = null;

            for (ITypeSymbol? current = src; (current = (current as IArrayTypeSymbol)?.ElementType ?? (current as IPointerTypeSymbol)?.PointedAtType) is not null;)
            {
                if (!recurse) return current;
                prev = current;
            }

            return prev;
        }

        public static string? GetAssemblyQualifiedName(this ITypeSymbol src)
        {
            //
            // Arrays and pointers have no containing assembly
            //

            IAssemblySymbol? containingAsm = src.GetElementType(recurse: true)?.ContainingAssembly ?? src.ContainingAssembly;
            if (containingAsm is null)
                return null;

            return $"{src.GetQualifiedMetadataName()}, {containingAsm.Identity}";
        }

        public static bool IsDelegate(this ITypeSymbol src) =>
            (src.GetElementType(recurse: true) ?? src).TypeKind is TypeKind.Delegate;

        //
        // Types (for instance arrays) derived from embedded types are no longer embedded. That's why this
        // GetElementType() magic.
        //

        public static bool IsNested(this ITypeSymbol src) => 
            (src.GetElementType(recurse: true)?.ContainingType ?? src.ContainingType) is not null && !src.IsGenericParameter();

        public static bool IsGenericParameter(this ITypeSymbol src) =>
            (src.GetElementType(recurse: true) ?? src).TypeKind is TypeKind.TypeParameter;

        public static string? GetQualifiedMetadataName(this ITypeSymbol src)
        {
            ITypeSymbol? elementType = src.GetElementType(recurse: true);
            if (elementType is IFunctionPointerTypeSymbol)
                return null;

            StringBuilder sb = new();

            INamespaceSymbol? ns = elementType?.ContainingNamespace ?? src.ContainingNamespace;  // Pointers and arrays have no containing namespace in compile time

            if (ns is not null && !ns.IsGlobalNamespace)
            {
                sb.Append(ns);
                sb.Append(Type.Delimiter);
            }

            foreach (ITypeSymbol parent in new Stack<ITypeSymbol>(src.GetParents()))
            {
                sb.Append($"{GetName(parent)}+");
            }

            sb.Append($"{GetName(elementType ?? src)}");

            return sb.ToString();

            static string GetName(ITypeSymbol type) 
            {
                string name = !string.IsNullOrEmpty(type.Name)
                    ? type.Name // supports tuples and nullables as well
                    : type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly));

                return type is INamedTypeSymbol named && named.IsGenericType()
                    ? $"{name}`{named.Arity}"
                    : name;
            }
        }

        public static IEnumerable<ITypeSymbol> GetAllInterfaces(this ITypeSymbol src) 
        {
            //
            // AllInterfaces may contain the same closed generic interface more times if the generic arguments are
            // differ in Nullable annotations only:
            //
            // interface IA: IB, IC<string> {}, interface IB: IC<string?> -> IC<string> will be returned twice
            //

            #pragma warning disable RS1024 // Compare symbols correctly
            HashSet<INamedTypeSymbol> returnedSymbols = new(SymbolEqualityComparer.Default);
            #pragma warning restore RS1024

            foreach (INamedTypeSymbol t in src.AllInterfaces)
            {
                INamedTypeSymbol iface = t;

                if (iface.IsGenericType())
                {
                    ITypeSymbol[] tas = iface
                        .TypeArguments
                        .Select
                        (
                            static ta => !ta.IsValueType
                                //
                                // Drop nullable annotation (int? -> Nullable<int>, object? -> [Nullable] object)
                                //

                                ? ta.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                                : ta
                        )
                        .ToArray();

                    iface = iface.OriginalDefinition.Construct(tas);
                }

                if (returnedSymbols.Add(iface))
                    yield return iface;
            }
        }

        public static AccessModifiers GetAccessModifiers(this ITypeSymbol src)
        {
            src = src.GetElementType(recurse: true) ?? src;

            AccessModifiers am = src.DeclaredAccessibility switch
            {
                Accessibility.Protected => AccessModifiers.Protected,
                Accessibility.Internal => AccessModifiers.Internal,
                Accessibility.ProtectedOrInternal => AccessModifiers.Protected | AccessModifiers.Internal,
                Accessibility.ProtectedAndInternal => AccessModifiers.Protected | AccessModifiers.Private,
                Accessibility.Public => AccessModifiers.Public,
                Accessibility.Private => AccessModifiers.Private,
                Accessibility.NotApplicable => AccessModifiers.Public, // TODO: FIXME
                _ => throw new InvalidOperationException(Resources.UNDETERMINED_ACCESS_MODIFIER)
            };

            if (src.IsGenericParameter())
                return am;

            switch (src)
            {
                case INamedTypeSymbol namedType:
                    foreach (ITypeSymbol ta in namedType.TypeArguments)
                    {
                        if (ta.IsGenericParameter())
                            continue;

                        UpdateAm(ref am, ta);
                    }
                    break;

                case IFunctionPointerTypeSymbol fn:
                    foreach (ITypeSymbol pt in fn.Signature.Parameters.Select(static p => p.Type))
                    {
                        UpdateAm(ref am, pt);
                    }
                    break;
            }

            if (src.ContainingType is not null)
                UpdateAm(ref am, src.ContainingType);

            return am;

            static void UpdateAm(ref AccessModifiers am, ITypeSymbol t)
            {
                AccessModifiers @new = t.GetAccessModifiers();
                if (@new < am)
                    am = @new;
            }
        }
    }
}
