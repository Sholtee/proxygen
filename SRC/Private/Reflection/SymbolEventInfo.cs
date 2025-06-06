﻿/********************************************************************************
* SymbolEventInfo.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

using Microsoft.CodeAnalysis;


namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolEventInfo : IEventInfo
    {
        private IEventSymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        public string Name => UnderlyingSymbol.StrippedName();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (AddMethod ?? RemoveMethod!).DeclaringType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FAddMethod;
        public IMethodInfo AddMethod => FAddMethod ??= SymbolMethodInfo.CreateFrom(UnderlyingSymbol.AddMethod!, Compilation);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FRemoveMethod;
        public IMethodInfo RemoveMethod => FRemoveMethod ??= SymbolMethodInfo.CreateFrom(UnderlyingSymbol.RemoveMethod!, Compilation);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.Type, Compilation);

        public bool IsStatic => (AddMethod ?? RemoveMethod!).IsStatic;

        public bool IsAbstract => (AddMethod ?? RemoveMethod!).IsAbstract;

        public bool IsVirtual => (AddMethod ?? RemoveMethod!).IsVirtual;

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
