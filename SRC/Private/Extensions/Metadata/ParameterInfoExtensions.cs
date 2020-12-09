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
            if (src.IsRetval)
                return src.ParameterType.IsByRef ? ParameterKind.InOut : ParameterKind.Out;

            if (src.ParameterType.IsByRef)
            {
                //
                // Innentol "native by ref" (pl. IntPtr) nem jatszik.
                //

#if NETSTANDARD2_0
                //
                // "IsReadOnlyAttribute" csak netstandard2.1-tol kezdve publikus.
                //

                if (src.IsIn && src.GetCustomAttributes().Any(attr => attr.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"))
#else
                //
                // "src.IsIn" nem eleg
                //

                if (src.IsIn && src.GetCustomAttribute<System.Runtime.CompilerServices.IsReadOnlyAttribute>() != null)
#endif
                    return ParameterKind.In;

                if (src.IsOut)
                    return ParameterKind.Out;

                return ParameterKind.InOut;
            }

            //
            // "params" es referencia szerinti parameter atadas egymast kizaroak.
            //

            if (src.GetCustomAttribute<ParamArrayAttribute>() != null) 
                return ParameterKind.Params;

            return ParameterKind.None;
        }
    }
}
