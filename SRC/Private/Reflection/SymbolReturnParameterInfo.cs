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

        private SymbolReturnParameterInfo(ITypeSymbol type, bool refRetVal, Compilation compilation)
        {
            Kind = refRetVal
                ? ParameterKind.InOut
                : ParameterKind.Out;            

            Type = SymbolTypeInfo.CreateFrom(type, compilation);
        }

        public static IParameterInfo CreateFrom(IMethodSymbol method, Compilation compilation) => new SymbolReturnParameterInfo(method.ReturnType, method.ReturnsByRef || method.ReturnsByRefReadonly, compilation);

        public override bool Equals(object obj) => obj is SymbolReturnParameterInfo that && that.Type.Equals(Type);

        public override int GetHashCode() => new { Type, Kind }.GetHashCode();

        public override string ToString() => $"{Type}&";
    }
}