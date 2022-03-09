/********************************************************************************
* SymbolEventInfo.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolEventInfo : IEventInfo
    {
        private IEventSymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        public string Name => UnderlyingSymbol.StrippedName();

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (AddMethod ?? RemoveMethod!).DeclaringType;

        private IMethodInfo? FAddMethod;
        public IMethodInfo AddMethod => FAddMethod ??= SymbolMethodInfo.CreateFrom(UnderlyingSymbol.AddMethod!, Compilation);

        private IMethodInfo? FRemoveMethod;
        public IMethodInfo RemoveMethod => FRemoveMethod ??= SymbolMethodInfo.CreateFrom(UnderlyingSymbol.RemoveMethod!, Compilation);

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.Type, Compilation);

        public bool IsStatic => (AddMethod ?? RemoveMethod!).IsStatic;

        private SymbolEventInfo(IEventSymbol evt, Compilation compilation)
        {
            UnderlyingSymbol = evt;
            Compilation = compilation;
        }

        public static IEventInfo CreateFrom(IEventSymbol evt, Compilation compilation)
        {
            evt.EnsureNotError();

            return new SymbolEventInfo(evt, compilation);
        }

        public override bool Equals(object obj) => obj is SymbolEventInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();
    }
}
