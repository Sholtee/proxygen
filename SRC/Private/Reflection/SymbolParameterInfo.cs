/********************************************************************************
* SymbolParameterInfo.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolParameterInfo : IParameterInfo
    {
        private IParameterSymbol UnderlyingSymbol { get; }

        protected Compilation Compilation { get; }

        private SymbolParameterInfo(IParameterSymbol para, Compilation compilation)
        {
            UnderlyingSymbol = para;
            Compilation = compilation;
        }

        public static IParameterInfo CreateFrom(IParameterSymbol para, Compilation compilation) => new SymbolParameterInfo(para, compilation);

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.Type, Compilation);

        public ParameterKind Kind => UnderlyingSymbol.GetParameterKind();

        public string Name => UnderlyingSymbol.Name;

        public override bool Equals(object obj) => obj is SymbolParameterInfo that && SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, that.UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);
    }
}