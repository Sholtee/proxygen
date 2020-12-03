/********************************************************************************
* IParameterSymbolExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IParameterSymbolExtensions
    {
        public static ParameterKind GetParameterKind(this IParameterSymbol src) => src switch
        {
            _ when src.IsParams => ParameterKind.Params,
            _ when src.RefKind == RefKind.RefReadOnly => ParameterKind.In,
            _ when src.RefKind == RefKind.Out => ParameterKind.Out,
            _ when src.RefKind == RefKind.Ref => ParameterKind.InOut,
            _ => ParameterKind.None
        };
    }
}
