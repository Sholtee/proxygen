/********************************************************************************
* ParameterInfoExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ParameterInfoExtensions
    {
        public static ParameterKind GetParameterKind(this ParameterInfo src) 
        {
            if (src.IsOut) return ParameterKind.Out;

            if (src.IsIn) 
            {
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
                //
                // "ref ValueType" parameter is IN netstandard2_1 felett.
                //

                if (src.GetCustomAttribute<IsReadOnlyAttribute>() == null) return ParameterKind.InOut;
#endif
                return ParameterKind.In;
            }

            //
            // "ParameterType.IsByRef" param.Is[In|Out] eseten is igazat ad vissza -> a lenti feltetel In|Out vizsgalat utan szerepeljen.
            //

            if (src.ParameterType.IsByRef) return ParameterKind.InOut;

            //
            // "params" es referencia szerinti parameter atadas egymast kizaroak.
            //

            if (src.GetCustomAttribute<ParamArrayAttribute>() != null) return ParameterKind.Params;

            return ParameterKind.None;
        }
    }
}
