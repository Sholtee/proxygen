/********************************************************************************
* RandomInterfaces.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Tests
{
    internal static class RandomInterfaces<T>
    {
        public static IEnumerable<Type> Values
        {
            get
            {
                foreach
                (
                    Type iface in typeof(object)
                        .Assembly
                        .GetExportedTypes()
                        .Where(t => t.IsInterface)
                )
                {
                    Type result = iface;

                    if (result.ContainsGenericParameters)
                    {
                        Type[] gas = result.GetGenericArguments();

                        if (gas.Any(ga => ga.GetGenericParameterConstraints().Any()))
                            continue;
#if NET8_0_OR_GREATER
                        try
#endif
                        {
                            result = result.MakeGenericType(Enumerable.Repeat(typeof(T), gas.Length).ToArray());
                        }
#if NET8_0_OR_GREATER
                        //
                        // GetGenericParameterConstraints() under .NET8.0 returns an empty array for some types having
                        // self-referencing constraint (IClass<T> where T: IClass). Seems a .NET bug...
                        //

                        catch (ArgumentException)
                        {
                            continue;
                        }
#endif
                    }

                    yield return result;
                }
            }
        }
    }
}
