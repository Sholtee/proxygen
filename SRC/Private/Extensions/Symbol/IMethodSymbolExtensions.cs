/********************************************************************************
* IMethodSymbolExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    /// <summary>
    /// Helpers methods for the <see cref="IMethodSymbol"/> interface.
    /// </summary>
    internal static class IMethodSymbolExtensions
    {
        /// <summary>
        /// Calculates the <see cref="AccessModifiers"/> value for the given method.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the access modifier can not be determined</exception>
        public static AccessModifiers GetAccessModifiers(this IMethodSymbol src) => src.DeclaredAccessibility switch
        {
            Accessibility.Protected => AccessModifiers.Protected,
            Accessibility.Internal => AccessModifiers.Internal,
            Accessibility.ProtectedOrInternal => AccessModifiers.Protected | AccessModifiers.Internal,
            Accessibility.ProtectedAndInternal => AccessModifiers.Protected | AccessModifiers.Private,
            Accessibility.Public => AccessModifiers.Public,
            Accessibility.Private when src.GetImplementedInterfaceMethods().Any() =>
                //
                // Since NET6_0 interfaces may have static abstract members:
                // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/static-abstract-interface-methods
                //
                // It's undocumented but seems these members won't be present in IL if they are open generics.
                // We treat such members private
                //

                src.IsStatic && src.TypeArguments.Any()
                    ? AccessModifiers.Private
                    : AccessModifiers.Explicit,
            Accessibility.Private => AccessModifiers.Private,
            _ => throw new InvalidOperationException(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        /// <summary>
        /// Returns the interfaces that declare the given method. Similar to the <see cref="MethodInfoExtensions.GetDeclaringInterfaces(System.Reflection.MethodBase)"/> method.
        /// </summary>
        public static IEnumerable<INamedTypeSymbol> GetDeclaringInterfaces(this IMethodSymbol src) => src.ContainingType.IsInterface()
            ? []
            : src
                .GetImplementedInterfaceMethods()
                .Select(static m => m.ContainingType);

        /// <summary>
        /// Returns the interface methods that are implemented by the given implementation. <paramref name="src"/> should belong to a class method.
        /// </summary>
        public static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(this IMethodSymbol src)
        {
            //
            // As of C# 11 interfaces may have static abstract methods... We don't deal with
            // the implementors.
            //

            if (src.IsStatic)
                yield break;

            INamedTypeSymbol containingType = src.ContainingType;
            if (containingType.IsInterface())
                yield break;

            foreach (ITypeSymbol iface in containingType.GetAllInterfaces())
                foreach (ISymbol member in iface.GetMembers())
                    if (member is IMethodSymbol ifaceMethod)
                        for (IMethodSymbol? met = src; met is not null; met = met.OverriddenMethod)
                            if (SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(ifaceMethod), met))
                                yield return ifaceMethod;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IReadOnlyList<MethodKind> FSpecialMethods =
        [
            MethodKind.Constructor, MethodKind.StaticConstructor,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.UserDefinedOperator,
            MethodKind.Conversion // explicit, implicit
        ];

        /// <summary>
        /// Returns true if the given method is special.
        /// </summary>
        public static bool IsSpecial(this IMethodSymbol src)
        {
            if (src.MethodKind is MethodKind.ExplicitInterfaceImplementation && !src.ContainingType.IsInterface())
                src = src.GetImplementedInterfaceMethods().Single();

            return FSpecialMethods.Contains(src.MethodKind);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IReadOnlyList<MethodKind> FClassMethods =
        [
            MethodKind.Ordinary,
            MethodKind.ExplicitInterfaceImplementation,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.UserDefinedOperator,
            MethodKind.Conversion, // explicit, implicit
            MethodKind.DelegateInvoke
        ];

        /// <summary>
        /// Returns true if the given method is class method.
        /// </summary>
        public static bool IsClassMethod(this IMethodSymbol src) => FClassMethods.Contains(src.MethodKind);

        //
        // OverriddenMethod won't work in case of "new" override.
        //

        /// <summary>
        /// Gets the immediate method that has been overridden by the given method (using "new" or "override" keyword). Returns null if the base method could not be determined.
        /// </summary>
        public static IMethodSymbol? GetOverriddenMethod(this IMethodSymbol src)
        {
            IMethodSymbol? overriddenMethod = src.OverriddenMethod;
            if (overriddenMethod is not null)
                return overriddenMethod;

            foreach(INamedTypeSymbol baseType in src.ContainingType.GetBaseTypes())
            {
                IMethodSymbol? baseMethod = GetBaseMethods(src, baseType).SingleOrDefault();
                if (baseMethod is not null)
                    return baseMethod;
            }

            return null;

            static IEnumerable<IMethodSymbol> GetBaseMethods(IMethodSymbol src, INamedTypeSymbol baseType)
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

        /// <summary>
        /// Returns true if the given method can be overridden (not sealed virtual)
        /// </summary>
        public static bool IsVirtual(this IMethodSymbol src) =>
            (src.IsVirtual || src.IsAbstract || (src.OverriddenMethod is not null && !src.IsSealed)) && !src.ContainingType.IsInterface();
    }
}
