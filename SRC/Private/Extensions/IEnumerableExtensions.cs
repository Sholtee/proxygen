/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
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

        /// <summary>
        /// Enumerates elements while disposing the recent ones upon the <see cref="IEnumerator.MoveNext()"/> calls
        /// </summary>
        public static IEnumerable<TDisposable> AsVolatile<TDisposable>(this IEnumerable<TDisposable> src) where TDisposable : IDisposable
        {
            IDisposable? previous = null;
            foreach (TDisposable current in src)
            {
                previous?.Dispose();
                yield return current;
                previous = current;
            }
            previous?.Dispose();
        }
    } 
}
