/********************************************************************************
* SymbolReturnParameterInfo.cs.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolReturnParameterInfo(IMethodSymbol method, Compilation compilation) : IParameterInfo 
    {
        public ParameterKind Kind => method switch
        {
            _ when method.ReturnsByRefReadonly => ParameterKind.RefReadonly,
            _ when method.ReturnsByRef => ParameterKind.Ref,
            _ => ParameterKind.Out
        };

        public string Name { get; } = string.Empty;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= SymbolTypeInfo.CreateFrom(method.ReturnType, compilation);

        public static IParameterInfo CreateFrom(IMethodSymbol method, Compilation compilation)
        {
            method.EnsureNotError();

            return new SymbolReturnParameterInfo(method, compilation);
        }

        public override bool Equals(object obj) => obj is SymbolReturnParameterInfo that && that.Type.Equals(Type);

        public override int GetHashCode() => new { Type, Kind }.GetHashCode();

        public override string ToString() => $"{Type}&";
    }
}