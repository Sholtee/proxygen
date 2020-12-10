/********************************************************************************
* SymbolMethodInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolMethodInfo : IMethodInfo
    {
        private IMethodSymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        private SymbolMethodInfo(IMethodSymbol method, Compilation compilation) 
        {
            UnderlyingSymbol = method;
            Compilation = compilation;
        }

        public static IMethodInfo CreateFrom(IMethodSymbol method, Compilation compilation) => new SymbolMethodInfo(method, compilation);

        private IReadOnlyList<IParameterInfo>? FParameters;
        public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderlyingSymbol
            .Parameters
            .Select(p => SymbolParameterInfo.CreateFrom(p, Compilation))
            .ToArray();

        private IParameterInfo? FReturnValue;
        public IParameterInfo ReturnValue => FReturnValue ??= UnderlyingSymbol.MethodKind != MethodKind.Constructor
            ? SymbolReturnParameter.CreateFrom(UnderlyingSymbol, Compilation)
            : null!;

        private static readonly MethodKind[] SpecialMethods = new[] 
        { 
            MethodKind.PropertyGet, MethodKind.PropertySet, 
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise
        };

        private bool? FIsSpecial;
        public bool IsSpecial => FIsSpecial ??= SpecialMethods.Contains
        (
            (
                UnderlyingSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation // nem vagom ez mi a faszert nem lehet bitmaszk
                    ? (IMethodSymbol) UnderlyingSymbol.GetDeclaringType().GetMembers(Name).Single()
                    : UnderlyingSymbol
            ).MethodKind
        );

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        public ITypeInfo DeclaringType => throw new System.NotImplementedException();

        public bool IsStatic => UnderlyingSymbol.IsStatic;

        public string Name => UnderlyingSymbol.StrippedName();
    }
}
