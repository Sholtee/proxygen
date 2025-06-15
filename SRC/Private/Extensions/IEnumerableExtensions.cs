/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Extensions for the <see cref="IEnumerable{T}"/> interface.
    /// </summary>
    internal static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns the index of a particular <paramref name="item"/> within the provided list. Returns -1 if the list doesn't include the <paramref name="item"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the index of a particular <paramref name="item"/> within the provided list using the default <see cref="EqualityComparer{T}"/>. Returns -1 if the list doesn't include the <paramref name="item"/>.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> src, T item) => src.IndexOf(item, EqualityComparer<T>.Default);
    }
}
