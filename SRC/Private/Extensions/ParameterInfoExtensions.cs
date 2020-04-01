/********************************************************************************
* ParameterInfoExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ParameterInfoExtensions
    {
        private static readonly HashSet<UnmanagedType> NativeByRefIndicators = new HashSet<UnmanagedType> 
        {
            UnmanagedType.LPArray
        };

        public static ParameterKind GetParameterKind(this ParameterInfo src) 
        {
            if (src.IsOut)
            {
                //
                // Ha a parameter csak nativ kontextusban kimeneti akkor az minket nem erdekel.
                //

                if (IsNativeByRef()) return ParameterKind.None;

                return ParameterKind.Out;
            }

            if (src.IsIn) 
            {
                if (IsNativeByRef()) return ParameterKind.None;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
                //
                // Ha nincs "IsReadOnlyAttribute" akkor "ref ValueType" a parameter tipusa.
                //

                if (src.GetCustomAttribute<IsReadOnlyAttribute>() != null)
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

            bool IsNativeByRef() 
            {
                var @as = src.GetCustomAttribute<MarshalAsAttribute>();

                return @as != null && NativeByRefIndicators.Contains(@as.Value);
            }
        }
    }
}
