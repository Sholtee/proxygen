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

        public static IReadOnlyList<TMeta> Convert<TMeta, TConcrete>(this IEnumerable<TConcrete> original, Func<TConcrete, TMeta> convert, Func<TConcrete, bool>? drop = null)
        {
            List<TMeta> lst = new();
            foreach (TConcrete concrete in original)
            {
                if (drop?.Invoke(concrete) is true)
                    continue;

                lst.Add(convert(concrete));
            }
            return lst;
        }

        public static bool Some<T>(this IEnumerable<T> src, Func<T, bool>? predicate = null)
        {
            foreach (T item in src)
            {
                if (predicate?.Invoke(item) is not false)
                    return true;
            }
            return false;
        }

        public static T Single<T>(this IEnumerable<T> src, Func<T, bool>? predicate = null) where T: class
        {
            T? found = null;
            foreach (T item in src)
            {
                if (predicate?.Invoke(item) is not false)
                {
                    if (found is not null)
                        throw new InvalidOperationException();
                    found = item;
                }
            }
            return found ?? throw new InvalidOperationException();
        }
    }
}
