/********************************************************************************
* SymbolPropertyInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolPropertyInfo : IPropertyInfo
    {
        private IPropertySymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => FGetMethod ??= UnderlyingSymbol.GetMethod is not null
            ? SymbolMethodInfo.CreateFrom(UnderlyingSymbol.GetMethod, Compilation) 
            : null;

        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => FSetMethod ??= UnderlyingSymbol.SetMethod is not null
            ? SymbolMethodInfo.CreateFrom(UnderlyingSymbol.SetMethod, Compilation) 
            : null;

        public string Name => UnderlyingSymbol.StrippedName();

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.Type, Compilation);

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderlyingSymbol
            .Parameters
            .Select(p => SymbolParameterInfo.CreateFrom(p, Compilation))
            .ToArray();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        private SymbolPropertyInfo(IPropertySymbol prop, Compilation compilation)
        {
            UnderlyingSymbol = prop;
            Compilation = compilation;
        }

        public static IPropertyInfo CreateFrom(IPropertySymbol prop, Compilation compilation) => new SymbolPropertyInfo(prop, compilation);

        public override bool Equals(object obj) => obj is SymbolPropertyInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();
    }
}
