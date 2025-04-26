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
        /// <summary>
        /// Associates <see cref="ParameterKind"/> to the given <see cref="IParameterSymbol"/>.
        /// </summary>
        public static ParameterKind GetParameterKind(this IParameterSymbol src) => src switch
        {
            _ when src.IsParams => ParameterKind.Params,
            _ when src.RefKind is RefKind.RefReadOnly => ParameterKind.In,
            _ when src.RefKind is RefKind.Out => ParameterKind.Out,
            _ when src.RefKind is RefKind.Ref or RefKind.RefReadOnlyParameter => ParameterKind.Ref,
            _ => ParameterKind.None
        };
    }
}
