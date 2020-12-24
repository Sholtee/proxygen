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
    internal class SymbolMethodInfo : IMethodInfo, IConstructorInfo
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
            ? SymbolReturnParameterInfo.CreateFrom(UnderlyingSymbol, Compilation)
            : null!;

        private bool? FIsSpecial;
        public bool IsSpecial => FIsSpecial ??= UnderlyingSymbol.IsSpecial();

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.ContainingType, Compilation);

        private IReadOnlyList<ITypeInfo>? FDeclaringInterfaces;
        public IReadOnlyList<ITypeInfo> DeclaringInterfaces => FDeclaringInterfaces ??= UnderlyingSymbol
            .GetDeclaringInterfaces()
            .Select(di => SymbolTypeInfo.CreateFrom(di, Compilation))
            .ToArray();

        public bool IsStatic => UnderlyingSymbol.IsStatic;

        public string Name => UnderlyingSymbol.StrippedName();

        public bool IsFinal => UnderlyingSymbol.IsFinal();

        public override bool Equals(object obj) => obj is SymbolMethodInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();

        public bool SignatureEquals(IMethodInfo that, bool ignoreVisibility) =>
            that is SymbolMethodInfo thatMethod && UnderlyingSymbol.SignatureEquals(thatMethod.UnderlyingSymbol, ignoreVisibility);
    }
}
