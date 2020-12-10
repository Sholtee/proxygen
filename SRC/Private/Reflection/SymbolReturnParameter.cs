/********************************************************************************
* SymbolReturnParameter.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolReturnParameter : IParameterInfo 
    {
        public ParameterKind Kind { get; }

        public string Name { get; } = string.Empty;

        public ITypeInfo Type { get; }

        private SymbolReturnParameter(ITypeSymbol type, bool refRetVal, Compilation compilation)
        {
            Kind = refRetVal
                ? ParameterKind.InOut
                : ParameterKind.Out;            

            Type = SymbolTypeInfo.CreateFrom(type, compilation);
        }

        public static IParameterInfo CreateFrom(IMethodSymbol method, Compilation compilation) => new SymbolReturnParameter(method.ReturnType, method.ReturnsByRef, compilation);
    }
}