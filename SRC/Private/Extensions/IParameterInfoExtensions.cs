/********************************************************************************
* IParameterInfoExtensions.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal static class IParameterInfoExtensions
    {
        public static bool EqualsTo(this IParameterInfo @this, IParameterInfo that) =>
            //
            // Not checking the name is intentional
            //

            @this.Kind == that.Kind && @this.Type.EqualsTo(that.Type);
    }
}
