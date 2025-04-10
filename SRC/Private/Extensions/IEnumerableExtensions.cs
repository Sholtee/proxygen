/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IEnumerableExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> src, T item, IEqualityComparer<T> comparer)
        {
            int index = 0;

            foreach (T i in src)
            {
                if (comparer.Equals(item, i))
                    return index;
                index++;
            }
            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> src, T item) => src.IndexOf(item, EqualityComparer<T>.Default);
    } 
}
