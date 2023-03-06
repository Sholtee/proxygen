/********************************************************************************
* ParameterInfoExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ParameterInfoExtensions
    {
        public static ParameterKind GetParameterKind(this ParameterInfo src)
        {
            //
            // Visszateresunk van? Vizsgalathoz ne az IsRetVal-t hasznaljuk mert az 
            // mindig false (netcore3.0)
            //

            if (src.Position is -1) return src switch
            {
                _ when src.ParameterType.IsByRef && IsReadOnly() => ParameterKind.RefReadonly,
                _ when src.ParameterType.IsByRef => ParameterKind.Ref,
                _ => ParameterKind.Out
            };

            //
            // "Normal" parameterunk van
            //

            if (src.ParameterType.IsByRef)
            {
                //
                // Innentol "native by ref" (pl. IntPtr) nem jatszik.
                //

                if (src.IsIn && IsReadOnly()) // siman src.IsIn nem eleg
                    return ParameterKind.In;

                if (!src.IsIn && src.IsOut)
                    return ParameterKind.Out;

                return ParameterKind.Ref;
            }

            //
            // "params" es referencia szerinti parameter atadas egymast kizaroak.
            //

            if (src.GetCustomAttribute<ParamArrayAttribute>() != null) 
                return ParameterKind.Params;

            return ParameterKind.None;

            bool IsReadOnly() => src
                .GetCustomAttributes()
                .Some
                (
                    //
                    // "IsReadOnlyAttribute" csak netstandard2.1-tol kezdve publikus.
                    //

                    static attr => attr
                        .GetType()
                        .FullName
                        .Equals("System.Runtime.CompilerServices.IsReadOnlyAttribute", StringComparison.OrdinalIgnoreCase)
                );
        }
    }
}
