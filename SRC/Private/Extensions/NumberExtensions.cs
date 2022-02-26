/********************************************************************************
* NumberExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal static class NumberExtensions
    {
        public static IEnumerable<T> Times<T>(this int src, Func<T> factory) => src.Times(_ => factory());

        public static IEnumerable<T> Times<T>(this int src, Func<int, T> factory)
        {
            for (int i = 0; i < src; i++)
                yield return factory(i);
        }
    }
}
