/********************************************************************************
* ParameterInfoExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ParameterInfoExtensions
    {
        /// <summary>
        /// Determines if the parameter possesses the given attribute. The attribute is identified by its fully qualified name since not all attribute types are available in NETSTANDARD. 
        /// </summary>
        public static bool HasAttribute(this ParameterInfo src, string fullName) => src
            .GetCustomAttributes()
            .Select(static attr => attr.GetType().FullName)
            .Contains(fullName, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Associates <see cref="ParameterKind"/> to the given <see cref="ParameterInfo"/>.
        /// </summary>
        /// <remarks>This method examines attributes as well so the caller better cache the return value.</remarks>
        public static ParameterKind GetParameterKind(this ParameterInfo src)
        {
            //
            // IsRetval is quirky under netcore3.0 so determine if we have a return
            // value by position
            //

            if (src.Position is -1) return src switch
            {
                _ when src.ParameterType.IsByRef && IsReadOnly() => ParameterKind.RefReadonly,
                _ when src.ParameterType.IsByRef => ParameterKind.Ref,
                _ => ParameterKind.Out
            };

            //
            // We have a "regular" parameter
            //

            if (src.ParameterType.IsByRef)
            {
                //
                // "native by ref" (e.g. IntPtr) is out of play from here.
                //

                if (src.IsIn && IsReadOnly()) // src.IsIn is not enough
                    return ParameterKind.In;

                if (!src.IsIn && src.IsOut)
                    return ParameterKind.Out;

                return ParameterKind.Ref;
            }

            //
            // "params" and by ref parameters are mutually exclusives.
            //

            if (src.HasAttribute(typeof(ParamArrayAttribute).FullName) || src.HasAttribute("System.Runtime.CompilerServices.ParamCollectionAttribute")) 
                return ParameterKind.Params;

            return ParameterKind.None;

            bool IsReadOnly() => src.HasAttribute("System.Runtime.CompilerServices.IsReadOnlyAttribute");
        }
    }
}
