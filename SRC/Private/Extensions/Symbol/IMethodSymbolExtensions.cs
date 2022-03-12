/********************************************************************************
* IMethodSymbolExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

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
            Accessibility.Private when /*src.MethodKind is MethodKind.ExplicitInterfaceImplementation*/ src.GetImplementedInterfaceMethods().Some() =>
                //
                // NET6_0-tol interface-nek lehet absztrakt statikus tagja:
                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/static-abstract-interface-methods
                //
                // Es -bar ez sehol nincs dokumentalva- de ugy tunik h ha a tag nyilt-generikus akkor a
                // generalt IL-ben nem fog szerepelni -> Az ilyen tagokat privatnak tekintjuk
                //

                src.IsStatic && src.TypeArguments.Some()
                    ? AccessModifiers.Private
                    : AccessModifiers.Explicit,
            Accessibility.Private => AccessModifiers.Private,
            #pragma warning disable CA2201 // In theory we should never reach here.
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
            #pragma warning restore CA2201
        };

        public static IEnumerable<INamedTypeSymbol> GetDeclaringInterfaces(this IMethodSymbol src) => src.ContainingType.IsInterface()
            ? Array.Empty<INamedTypeSymbol>()
            : src
                .GetImplementedInterfaceMethods()
                .Convert(m => m.ContainingType);

        public static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(this IMethodSymbol src, bool inspectOverrides = true)
        {
            INamedTypeSymbol containingType = src.ContainingType;

            foreach (ITypeSymbol iface in containingType.GetAllInterfaces())
            {
                foreach (ISymbol member in iface.GetMembers())
                {
                    if (member is not IMethodSymbol ifaceMethod)
                        continue;

                    for (IMethodSymbol? met = src; met is not null; met = met.OverriddenMethod)
                    {
                        if (SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(ifaceMethod), met))
                            yield return ifaceMethod;

                        if (!inspectOverrides)
                            break;
                    }
                }
            }
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
                src = src.GetImplementedInterfaceMethods().Single()!;

            return SpecialMethods.Some(mk => mk == src.MethodKind);
        }

        private static readonly IReadOnlyList<MethodKind> ClassMethods = new MethodKind[]
        {
            MethodKind.Ordinary,
            MethodKind.ExplicitInterfaceImplementation,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.UserDefinedOperator,
            MethodKind.Conversion // explicit, implicit
        };

        public static bool IsClassMethod(this IMethodSymbol src) => ClassMethods.Some(mk => mk == src.MethodKind);

        public static bool IsFinal(this IMethodSymbol src) => 
            src.IsSealed ||
            //
            // A fordito implicit lepecsetelt virtualist csinal az interface tagot megvalosito metodusbol
            //

            (!src.IsVirtual && !src.IsAbstract && !src.IsOverride && src.GetImplementedInterfaceMethods(inspectOverrides: false).Some());
    }
}
