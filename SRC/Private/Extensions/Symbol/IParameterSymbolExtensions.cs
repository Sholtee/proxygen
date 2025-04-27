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
            { IsParams: true } => ParameterKind.Params,
            { RefKind: RefKind.RefReadOnly} => ParameterKind.In,
            { RefKind: RefKind.Out} => ParameterKind.Out,
            { RefKind: RefKind.Ref
#if !LEGACY_COMPILER
                or RefKind.RefReadOnlyParameter
#endif
            } => ParameterKind.Ref,
            _ => ParameterKind.None
        };
    }
}
