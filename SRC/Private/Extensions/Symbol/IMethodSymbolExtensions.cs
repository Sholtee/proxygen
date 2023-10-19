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
                // Since NET6_0 interfaces may have satic abstract members:
                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/static-abstract-interface-methods
                //
                // It's undocumented but seems these members won't be present in IL if they are open generics.
                // We treat such members private
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
                .Convert(static m => m.ContainingType);

        public static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(this IMethodSymbol src, bool inspectOverrides = true)
        {
            //
            // As of C# 11 interfaces may have satic abstract methods... We don't deal with
            // the implementors.
            //

            if (src.IsStatic)
                yield break;

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
            if (src.MethodKind is MethodKind.ExplicitInterfaceImplementation) // why not bitmask?
                src = src.GetImplementedInterfaceMethods().Single()!;

            return SpecialMethods.Some(mk => mk == src.MethodKind) ||
                //
                // Starting from C# 11 interfaces may have static abstract methods.
                //

                (src.IsStatic && src.IsAbstract);
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
            // The compiler makes method sealed virtual if it implements some insterface method.
            //

            (!src.IsVirtual && !src.IsAbstract && !src.IsOverride && src.GetImplementedInterfaceMethods(inspectOverrides: false).Some());

        //
        // OverriddenMethod won't work in case of "new" override.
        //

        public static IMethodSymbol? GetOverriddenMethod(this IMethodSymbol src)
        {
            IMethodSymbol? overriddenMethod = src.OverriddenMethod;
            if (overriddenMethod is not null)
                return overriddenMethod;

            foreach(INamedTypeSymbol baseType in src.ContainingType.GetBaseTypes())
            {
                IMethodSymbol? baseMethod = GetBaseMethods(baseType).Single(throwOnEmpty: false);
                if (baseMethod is not null)
                    return baseMethod;
            }

            return null;

            IEnumerable<IMethodSymbol> GetBaseMethods(INamedTypeSymbol baseType)
            {
                foreach (ISymbol member in baseType.GetMembers(src.Name))
                {
                    if (member.IsStatic != src.IsStatic || member is not IMethodSymbol baseMethod)
                        continue;

                    if (baseMethod.Parameters.Length != src.Parameters.Length)
                        continue;

                    bool match = true;
                    for (int i = 0; i < baseMethod.Parameters.Length; i++)
                    {
                        IParameterSymbol
                            basePara = baseMethod.Parameters[i],
                            para = src.Parameters[i];

                        if (!SymbolEqualityComparer.Default.Equals(basePara.Type, para.Type) || basePara.RefKind != para.RefKind)
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                        yield return baseMethod;
                }
            }
        }
    }
}
