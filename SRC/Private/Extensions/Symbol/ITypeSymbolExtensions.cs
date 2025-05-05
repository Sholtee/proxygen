/********************************************************************************
* ITypeSymbolExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    /// <summary>
    /// Defines helper methods for the <see cref="ITypeSymbol"/> interface.
    /// </summary>
    internal static class ITypeSymbolExtensions
    {
        /// <summary>
        /// Returns true if the given <see cref="ITypeSymbol"/> identifies an interface.
        /// </summary>
        public static bool IsInterface(this ITypeSymbol src) => src.TypeKind is TypeKind.Interface;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IReadOnlyList<TypeKind> FClassTypes =
        [
            TypeKind.Class,
            TypeKind.Array,
            TypeKind.Delegate,
            TypeKind.Pointer
        ];

        /// <summary>
        /// Returns true if the given <see cref="ITypeSymbol"/> identifies a class.
        /// </summary>
        public static bool IsClass(this ITypeSymbol src) => FClassTypes.Contains(src.TypeKind);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IReadOnlyList<TypeKind> FSealedTypes =
        [
            TypeKind.Array
        ];

        /// <summary>
        /// Returns true if the given <see cref="ITypeSymbol"/> identifies a sealed type.
        /// </summary>
        public static bool IsFinal(this ITypeSymbol src) => src.IsSealed || src.IsStatic || FSealedTypes.Contains(src.TypeKind);

        /// <summary>
        /// Gets the enclosing type if the given <see cref="ITypeSymbol"/> identifies a nested type.
        /// </summary>
        public static ITypeSymbol? GetEnclosingType(this ITypeSymbol src) => (src.GetInnerMostElementType() ?? src).ContainingType;

        /// <summary>
        /// Gets the friendly name of the type identified by the given <see cref="ITypeSymbol"/>. Friendly name doesn't contain references for generic arguments, pointer features or the enclosing type.
        /// </summary>
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

        /// <summary>
        /// Returns true if the given type is a specialized nullable:
        /// <code>
        /// Nullable&lt;int&gt;
        /// </code>
        /// </summary>
        public static bool IsBoundNullable(this INamedTypeSymbol src) =>
            src.ConstructedFrom?.SpecialType is SpecialType.System_Nullable_T &&
            !SymbolEqualityComparer.Default.Equals(src.ConstructedFrom, src);

        /// <summary>
        /// Returns true if the given <see cref="ITypeSymbol"/> identifies a generic type.
        /// </summary>
        public static bool IsGenericType(this ITypeSymbol src) => src is INamedTypeSymbol named && named.TypeArguments.Any();

        /// <summary>
        /// Returns the enclosing types.
        /// </summary>
        public static IEnumerable<ITypeSymbol> GetParents(this ITypeSymbol src) 
        {
            src = src.GetInnerMostElementType() ?? src; // Array has no containing type but array item does

            for (ITypeSymbol parent = src; (parent = parent!.ContainingType) is not null;)
                yield return parent;
        }

        /// <summary>
        /// Returns the base types.
        /// </summary>
        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol src)
        {
            if (src.IsInterface())
                foreach (ITypeSymbol iface in src.GetAllInterfaces())
                    yield return iface;
            else
                for (ITypeSymbol? baseType = src.BaseType; baseType is not null; baseType = baseType.BaseType)
                    yield return baseType;
        }

        /// <summary>
        /// Returns the class or interface hierarchy starting from the current type.
        /// </summary>
        public static IEnumerable<ITypeSymbol> GetHierarchy(this ITypeSymbol src)
        {
            yield return src;

            foreach (ITypeSymbol baseType in src.GetBaseTypes())
                yield return baseType;
        }

        /// <summary>
        /// Enumerates the methods defined on the type identified by the given <see cref="ITypeSymbol"/>.
        /// </summary>
        public static IEnumerable<IMethodSymbol> ListMethods(this ITypeSymbol src, bool includeStatic = false) => src
            .ListMembersInternal<IMethodSymbol>
            (
                static m => m,
                includeStatic
            )
            .Where(static m => !m.IsSpecial());

        /// <summary>
        /// Enumerates the properties defined on the type identified by the given <see cref="ITypeSymbol"/>.
        /// </summary>
        public static IEnumerable<IPropertySymbol> ListProperties(this ITypeSymbol src, bool includeStatic = false)
        {
            return src.ListMembersInternal<IPropertySymbol>
            (
                static prop =>
                {
                    if (prop.GetMethod is null)
                        return prop.SetMethod!;

                    if (prop.SetMethod is null)
                        return prop.GetMethod;

                    //
                    // Higher visibility has the precedence
                    //

                    return prop.GetMethod.GetAccessModifiers() > prop.SetMethod.GetAccessModifiers()
                        ? prop.GetMethod
                        : prop.SetMethod;
                },
                includeStatic
            );
        }

        /// <summary>
        /// Enumerates the events defined on the type identified by the given <see cref="ITypeSymbol"/>.
        /// </summary>
        public static IEnumerable<IEventSymbol> ListEvents(this ITypeSymbol src, bool includeStatic = false) => src.ListMembersInternal<IEventSymbol>
        (
            //
            // Events always have Add & Remove method declared
            //

            static e => e.AddMethod!,
            includeStatic
        );

        /// <summary>
        /// The core member enumerator. It searches the whole hierarchy.
        /// </summary>
        private static IEnumerable<TMember> ListMembersInternal<TMember>
        (
            this ITypeSymbol src, 
            Func<TMember, IMethodSymbol> getUnderlyingMethod,
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

                    IMethodSymbol underlyingMethod = getUnderlyingMethod(member);

                    //
                    // When we encounter a virtual method, return only the last override
                    //

                    if (underlyingMethod.GetOverriddenMethod() is IMethodSymbol overriddenMethod && !overriddenMethods.Add(overriddenMethod))
                        continue;

                    if (overriddenMethods.Contains(underlyingMethod))
                        continue;

                    //
                    // We don't want to return private members
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

        /// <summary>
        /// Returns the constructors declared by user code on the type identified by the given <see cref="ITypeSymbol"/>.
        /// </summary>
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

        /// <summary>
        /// Resolves the given pointer type by returning the element type. For instance "int" in case of "int[]" 
        /// </summary>
        public static ITypeSymbol? GetElementType(this ITypeSymbol src) => src switch
        {
            IArrayTypeSymbol array => array.ElementType,
            IPointerTypeSymbol pointer => pointer.PointedAtType,
            _ => null
        };

        /// <summary>
        /// Resolves the given pointer type by returning the inner most element type. For instance "int" in case of "int*[]" 
        /// </summary>
        public static ITypeSymbol? GetInnerMostElementType(this ITypeSymbol src)
        {
            ITypeSymbol? prev = null;

            for (ITypeSymbol? current = src; (current = current!.GetElementType()) is not null;)
                prev = current;

            return prev;
        }

        /// <summary>
        /// Gets the assembly qualified name of the type identified by the given <see cref="ITypeSymbol"/>
        /// </summary>
        public static string? GetAssemblyQualifiedName(this ITypeSymbol src)
        {
            //
            // Arrays and pointers have no containing assembly
            //

            IAssemblySymbol? containingAsm = src.GetInnerMostElementType()?.ContainingAssembly ?? src.ContainingAssembly;
            if (containingAsm is null)
                return null;

            return $"{src.GetQualifiedMetadataName()}, {containingAsm.Identity}";
        }

        /// <summary>
        /// Returns true if the given <see cref="ITypeSymbol"/> identifies a delegate.
        /// </summary>
        public static bool IsDelegate(this ITypeSymbol src) =>
            (src.GetInnerMostElementType() ?? src).TypeKind is TypeKind.Delegate;

        /// <summary>
        /// Returns true if the type identified by the given <see cref="ITypeSymbol"/> is nested.
        /// </summary>
        public static bool IsNested(this ITypeSymbol src) => 
            (src.GetInnerMostElementType()?.ContainingType ?? src.ContainingType) is not null && !src.IsGenericParameter();

        /// <summary>
        /// Returns true if the type identified by the given <see cref="ITypeSymbol"/> is a generic parameter.
        /// </summary>
        public static bool IsGenericParameter(this ITypeSymbol src) =>
            (src.GetInnerMostElementType() ?? src).TypeKind is TypeKind.TypeParameter;

        /// <summary>
        /// Gets the qualified metadata name for the given type. For instance "Namespace.Type+NestedType"
        /// </summary>
        public static string? GetQualifiedMetadataName(this ITypeSymbol src)
        {
            ITypeSymbol? elementType = src.GetInnerMostElementType();
            if (elementType is IFunctionPointerTypeSymbol)
                return null;

            StringBuilder sb = new();

            INamespaceSymbol? ns = elementType?.ContainingNamespace ?? src.ContainingNamespace;  // Pointers and arrays have no containing namespace in compile time

            if (ns is not null && !ns.IsGlobalNamespace)
            {
                sb.Append(ns);
                sb.Append(Type.Delimiter);
            }

            foreach (ITypeSymbol parent in src.GetParents().Reverse())
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

        /// <summary>
        /// Returns all the interfaces, that were implemented or inherited by the given type.
        /// </summary>
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
                INamedTypeSymbol iface = t;  // we cannot assign to foreach variable

                if (iface.IsGenericType()) iface = iface.OriginalDefinition.Construct
                (
                    [
                        ..iface.TypeArguments.Select
                        (
                            static ta => !ta.IsValueType
                                //
                                // Drop nullable annotation (int? -> Nullable<int>, object? -> [Nullable] object)
                                //

                                ? ta.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                                : ta
                        )
                    ]
                );

                if (returnedSymbols.Add(iface))
                    yield return iface;
            }
        }

        /// <summary>
        /// Associates <see cref="AccessModifiers"/> to the type identified by the given <see cref="ITypeSymbol"/>.
        /// </summary>
        public static AccessModifiers GetAccessModifiers(this ITypeSymbol src)
        {
            src = src.GetInnerMostElementType() ?? src;

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
                    foreach (ITypeSymbol ta in namedType.TypeArguments.Where(static ta => !ta.IsGenericParameter()))
                        UpdateAm(ref am, ta);
                    break;

                case IFunctionPointerTypeSymbol fn:
                    foreach (ITypeSymbol pt in fn.Signature.Parameters.Select(static p => p.Type))
                        UpdateAm(ref am, pt);
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
