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
            #pragma warning disable CA2201 // In theory we should never reach here.
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
            #pragma warning restore CA2201
        };

        public static IEnumerable<INamedTypeSymbol> GetDeclaringInterfaces(this IMethodSymbol src) => src.ContainingType.IsInterface()
            ? Array.Empty<INamedTypeSymbol>()
            : src
                .GetImplementedInterfaceMethods()
                .Select(m => m.ContainingType);

        public static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(this IMethodSymbol src, bool inspectOverrides = true)
        {
            INamedTypeSymbol containingType = src.ContainingType;

            return containingType
                .AllInterfaces
                .SelectMany(@interface => @interface
                    .GetMembers()
                    .OfType<IMethodSymbol>())
                .Where(interfaceMethod =>
                {
                    for (IMethodSymbol? met = src; met is not null; met = met.OverriddenMethod)
                    {
                        if (SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMethod), met))
                            return true;

                        if (!inspectOverrides)
                            break;
                    }

                    return false;
                });
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
            if (src.MethodKind is MethodKind.ExplicitInterfaceImplementation) // nem vagom a MethodKind mi a faszert nem lehet bitmaszk
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

        public static bool IsFinal(this IMethodSymbol src) => 
            src.IsSealed || 
            (!src.IsVirtual && !src.IsAbstract && !src.IsOverride && src.GetImplementedInterfaceMethods(inspectOverrides: false).Any()); // a fordito implicit lepecsetelt virtualist csinal az interface tagot megvalosito metodusbol
    }
}
