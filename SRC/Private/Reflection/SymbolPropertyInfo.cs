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
        private IPropertySymbol UnderLyingSymbol { get; }

        private Compilation Compilation { get; }

        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => FGetMethod ??= UnderLyingSymbol.GetMethod is not null
            ? SymbolMethodInfo.CreateFrom(UnderLyingSymbol.GetMethod, Compilation) 
            : null;

        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => FSetMethod ??= UnderLyingSymbol.SetMethod is not null
            ? SymbolMethodInfo.CreateFrom(UnderLyingSymbol.SetMethod, Compilation) 
            : null;

        public string Name 
        {
            get 
            {
                string strippedName = UnderLyingSymbol.StrippedName();

                return string.IsNullOrEmpty(strippedName)
                    ? "Item" // "this[]" eseten
                    : strippedName;
            }
        }

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(UnderLyingSymbol.Type, Compilation);

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderLyingSymbol
            .Parameters
            .Select(p => SymbolParameterInfo.CreateFrom(p, Compilation))
            .ToArray();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        private SymbolPropertyInfo(IPropertySymbol prop, Compilation compilation)
        {
            UnderLyingSymbol = prop;
            Compilation = compilation;
        }

        public static IPropertyInfo CreateFrom(IPropertySymbol prop, Compilation compilation) => new SymbolPropertyInfo(prop, compilation);

        public override bool Equals(object obj) => obj is SymbolPropertyInfo that && SymbolEqualityComparer.Default.Equals(that.UnderLyingSymbol, UnderLyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderLyingSymbol);
    }
}
