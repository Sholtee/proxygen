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
            _ when src.RefKind == RefKind.Ref => ParameterKind.Ref,
            _ => ParameterKind.None
        };

        public static bool EqualsTo(this IParameterSymbol src, IParameterSymbol that) 
        {
            if (!GetParameterBasicAttributes(src).Equals(GetParameterBasicAttributes(that)))
                return false;

            return src.Type.EqualsTo(that.Type);

            static object GetParameterBasicAttributes(IParameterSymbol p) => new
            {
                //p.Name,
                p.RefKind,
                p.Ordinal,
                p.NullableAnnotation
            };
        }
    }
}
