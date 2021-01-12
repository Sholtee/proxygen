/********************************************************************************
* SymbolReturnParameterInfo.cs.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolReturnParameterInfo : IParameterInfo 
    {
        public ParameterKind Kind { get; }

        public string Name { get; } = string.Empty;

        public ITypeInfo Type { get; }

        private SymbolReturnParameterInfo(IMethodSymbol method, Compilation compilation)
        {
            Kind = method switch 
            {
                _ when method.ReturnsByRefReadonly => ParameterKind.RefReadonly,
                _ when method.ReturnsByRef => ParameterKind.Ref,
                _ => ParameterKind.Out
            };

            Type = SymbolTypeInfo.CreateFrom(method.ReturnType, compilation);
        }

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