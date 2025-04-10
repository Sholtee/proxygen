/********************************************************************************
* SymbolPropertyInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolPropertyInfo : IPropertyInfo
    {
        private IPropertySymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => UnderlyingSymbol.GetMethod is not null
            ? FGetMethod ??= SymbolMethodInfo.CreateFrom(UnderlyingSymbol.GetMethod, Compilation) 
            : null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => UnderlyingSymbol.SetMethod is not null
            ? FSetMethod ??= SymbolMethodInfo.CreateFrom(UnderlyingSymbol.SetMethod, Compilation) 
            : null;

        public string Name => UnderlyingSymbol.StrippedName();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.Type, Compilation);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderlyingSymbol
            .Parameters
            .Select(p => SymbolParameterInfo.CreateFrom(p, Compilation))
            .ToImmutableList();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        public bool IsAbstract => (GetMethod ?? SetMethod!).IsAbstract;

        public bool IsVirtual => (GetMethod ?? SetMethod!).IsVirtual;

        private SymbolPropertyInfo(IPropertySymbol prop, Compilation compilation)
        {
            UnderlyingSymbol = prop;
            Compilation = compilation;
        }

        public static IPropertyInfo CreateFrom(IPropertySymbol prop, Compilation compilation)
        {
            prop.EnsureNotError();

            return new SymbolPropertyInfo(prop, compilation);
        }

        public override bool Equals(object obj) => obj is SymbolPropertyInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();
    }
}
