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

            if (src.GetCustomAttribute<ParamArrayAttribute>() != null) 
                return ParameterKind.Params;

            return ParameterKind.None;

            bool IsReadOnly() => src
                .GetCustomAttributes()
                .Any
                (
                    //
                    // "IsReadOnlyAttribute" is public since netstandard2.1.
                    //

                    static attr => attr
                        .GetType()
                        .FullName
                        .Equals("System.Runtime.CompilerServices.IsReadOnlyAttribute", StringComparison.OrdinalIgnoreCase)
                );
        }
    }
}
