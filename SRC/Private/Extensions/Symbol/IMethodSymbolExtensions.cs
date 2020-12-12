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
            Accessibility.Public => AccessModifiers.Public,
            Accessibility.Private when src.GetDeclaringType().IsInterface() => AccessModifiers.Explicit,
            Accessibility.Private => AccessModifiers.Private,
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        public static INamedTypeSymbol GetDeclaringType(this IMethodSymbol src) => (src.GetImplementedInterfacceMethod() ?? src).ContainingType;

        public static IMethodSymbol? GetImplementedInterfacceMethod(this IMethodSymbol src)
        {
            INamedTypeSymbol containingType = src.ContainingType;

            return containingType
                .AllInterfaces
                .SelectMany(@interface => @interface
                    .GetMembers()
                    .OfType<IMethodSymbol>())
                .SingleOrDefault(interfaceMethod => 
                    SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMethod), src));
        }

        private static readonly IReadOnlyList<MethodKind> SpecialMethods = new[]
        {
            MethodKind.Constructor, MethodKind.StaticConstructor,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.UserDefinedOperator
        };

        public static bool IsSpecial(this IMethodSymbol src) // slow
        {
            if (src.MethodKind == MethodKind.ExplicitInterfaceImplementation) // nem vagom a MethodKind mi a faszert nem lehet bitmaszk
                src = src.GetImplementedInterfacceMethod()!;

            return SpecialMethods.Contains(src.MethodKind);
        }
    }
}
