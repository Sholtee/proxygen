/********************************************************************************
* IMethodSymbolExtensions.cs                                                    *
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

    internal static class IMethodSymbolExtensions
    {
        public static AccessModifiers GetAccessModifiers(this IMethodSymbol src) => src.DeclaredAccessibility switch
        {
            Accessibility.Protected => AccessModifiers.Protected,
            Accessibility.Internal => AccessModifiers.Internal,
            Accessibility.ProtectedOrInternal => AccessModifiers.Protected | AccessModifiers.Internal,
            Accessibility.ProtectedAndInternal => AccessModifiers.Protected | AccessModifiers.Private,
            Accessibility.Public => AccessModifiers.Public,
            Accessibility.Private when src.GetImplementedInterfaceMethods().Any() => AccessModifiers.Explicit,
            Accessibility.Private => AccessModifiers.Private,
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        public static IEnumerable<INamedTypeSymbol> GetDeclaringInterfaces(this IMethodSymbol src) => src.ContainingType.IsInterface()
            ? Array.Empty<INamedTypeSymbol>()
            : src
                .GetImplementedInterfaceMethods()
                .Select(m => m.ContainingType);

        public static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(this IMethodSymbol src)
        {
            INamedTypeSymbol containingType = src.ContainingType;

            return containingType
                .AllInterfaces
                .SelectMany(@interface => @interface
                    .GetMembers()
                    .OfType<IMethodSymbol>())
                .Where(interfaceMethod => 
                    SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMethod), src));
        }

        private static readonly IReadOnlyList<MethodKind> SpecialMethods = new[]
        {
            MethodKind.Constructor, MethodKind.StaticConstructor,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.UserDefinedOperator,
            MethodKind.Conversion // explicit, implicit
        };

        public static bool IsSpecial(this IMethodSymbol src) // slow
        {
            if (src.MethodKind == MethodKind.ExplicitInterfaceImplementation) // nem vagom a MethodKind mi a faszert nem lehet bitmaszk
                src = src.GetImplementedInterfaceMethods().Single();

            return SpecialMethods.Contains(src.MethodKind);
        }

        private static readonly IReadOnlyList<MethodKind> ClassMethods = new[]
        {
            MethodKind.Ordinary,
            MethodKind.ExplicitInterfaceImplementation,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.UserDefinedOperator,
            MethodKind.Conversion // explicit, implicit
        };

        public static bool IsClassMethod(this IMethodSymbol src) => ClassMethods.Contains(src.MethodKind);

        public static bool SignatureEquals(this IMethodSymbol src, IMethodSymbol that, bool ignoreVisibility = false) 
        {
            if (src.IsSpecial() || that.IsSpecial())
                return false;

            //
            // T ClassA<T>.Foo() != T ClassB<TT, T>.Foo()
            //

            if (src.ContainingType?.Arity != that.ContainingType?.Arity)
                return false;

            if (!GetMethodBasicAttributes(src).Equals(GetMethodBasicAttributes(that)))
                return false;

            if (!src.ReturnType.EqualsTo(that.ReturnType))
                return false;

            if (src.Parameters.Length != that.Parameters.Length)
                return false;

            for (int i = 0; i < src.Parameters.Length; i++)
                if (!src.Parameters[i].EqualsTo(that.Parameters[i]))
                    return false;

            return true;

            object GetMethodBasicAttributes(IMethodSymbol m) => new
            {
                m.Name,
                m.IsStatic,
                m.MethodKind,
                m.ReturnsByRef,
                m.ReturnsByRefReadonly,
                m.RefKind,
                m.Arity,
                Accessibility = !ignoreVisibility 
                    ? m.DeclaredAccessibility 
                    : (Accessibility?) null
            };
        }
    }
}
