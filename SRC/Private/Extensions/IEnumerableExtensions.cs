/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IEnumerableExtensions
    {
        public static int? IndexOf<T>(this IEnumerable<T> src, T item, IEqualityComparer<T> comparer)
        {
            int index = 0;

            foreach (T i in src)
            {
                if (comparer.Equals(item, i))
                    return index;
                index++;
            }
            return null;
        }

        public static int? IndexOf<T>(this IEnumerable<T> src, T item) => src.IndexOf(item, EqualityComparer<T>.Default);

        public static TMeta[] Convert<TMeta, TConcrete>(this IEnumerable<TConcrete> original, Func<TConcrete, TMeta> convert, Func<TConcrete, bool>? drop = null)
        {
            TMeta[] ar = new TMeta[5];

            int i = 0;

            foreach (TConcrete concrete in original)
            {
                if (drop?.Invoke(concrete) is true)
                    continue;

                if (i == ar.Length)
                    Array.Resize(ref ar, ar.Length * 2);

                ar[i++] = convert(concrete);
            }

            Array.Resize(ref ar, i);

            return ar;
        }

        public static bool Some<T>(this IEnumerable<T> src, Func<T, bool>? predicate = null)
        {
            if (predicate is null && src is ICollection<T> coll)
                return coll.Count > 0;

            foreach (T item in src)
            {
                if (predicate?.Invoke(item) is not false)
                    return true;
            }

            return false;
        }

        public static T? Single<T>(this IEnumerable<T> src, Func<T, bool>? predicate = null, bool throwOnEmpty = true) where T: class
        {
            T? result = null;

            foreach (T item in src)
            {
                if (predicate?.Invoke(item) is not false)
                {
                    if (result is not null)
                        throw new InvalidOperationException();
                    result = item;
                }
            }

            if (result is null && throwOnEmpty)
                throw new InvalidOperationException();

            return result;
        }
    }
}
