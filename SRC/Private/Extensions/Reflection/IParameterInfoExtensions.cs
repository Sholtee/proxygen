/********************************************************************************
* IParameterInfoExtensions.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="IParameterInfo"/> interface.
    /// </summary>
    internal static class IParameterInfoExtensions
    {
        /// <summary>
        /// Determines the equality of the given two parameters. This method doesn't take <see cref="IHasName.Name"/> into account.
        /// </summary>
        public static bool EqualsTo(this IParameterInfo @this, IParameterInfo that) =>
            //
            // Not checking the name is intentional
            //

            @this.Kind == that.Kind && @this.Type.EqualsTo(that.Type);
    }
}
