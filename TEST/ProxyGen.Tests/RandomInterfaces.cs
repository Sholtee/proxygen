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
                foreach (Type iface in typeof(object)
                    .Assembly
                    .GetExportedTypes()
                    .Where(t => t.IsInterface))
                {
                    if (iface.ContainsGenericParameters)
                    {
                        Type[] gas = iface.GetGenericArguments();

                        if (!gas.Any(ga => ga.GetGenericParameterConstraints().Any()))
                            yield return iface.MakeGenericType(Enumerable.Repeat(typeof(T), gas.Length).ToArray());

                        continue;
                    }

                    yield return iface;
                }
            }
        }
    }
}
