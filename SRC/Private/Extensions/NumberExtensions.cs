/********************************************************************************
* NumberExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="int"/> type.
    /// </summary>
    internal static class NumberExtensions
    {
        /// <summary>
        /// Calls the given factory for N times and creates a list from the returned values.
        /// </summary>
        public static IEnumerable<T> Times<T>(this int src, Func<T> factory) => src.Times(_ => factory());

        /// <summary>
        /// Calls the given factory for N times and creates a list from the returned values.
        /// </summary>
        public static IEnumerable<T> Times<T>(this int src, Func<int, T> factory)
        {
            for (int i = 0; i < src; i++)
                yield return factory(i);
        }
    }
}
