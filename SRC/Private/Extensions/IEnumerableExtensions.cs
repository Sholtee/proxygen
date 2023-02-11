/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

        public static IEnumerable<TConverted> Convert<TConverted, TConcrete>(this IEnumerable<TConcrete> original, Func<TConcrete, int, TConverted> convert, Func<TConcrete, int, bool>? drop = null)
        {
            int i = 0;

            foreach (TConcrete concrete in original)
            {
                if (drop?.Invoke(concrete, i) is not true)
                    yield return convert(concrete, i++);
            }
        }

        public static IEnumerable<TConverted> Convert<TConverted, TConcrete>(this IEnumerable<TConcrete> original, Func<TConcrete, TConverted> convert, Func<TConcrete, bool>? drop = null) =>
            original.Convert((element, _) => convert(element), drop is not null ? (element, _) => drop(element) : null);

        public static TConverted[] ConvertAr<TConverted, TConcrete>(this IEnumerable<TConcrete> original, Func<TConcrete, int, TConverted> convert, Func<TConcrete, int, bool>? drop = null)
        {
            TConverted[] ar = new TConverted[5];

            int i = 0;

            foreach (TConverted converted in original.Convert(convert, drop))
            {
                if (i == ar.Length)
                    Array.Resize(ref ar, ar.Length * 2);

                ar[i++] = converted;
            }

            Array.Resize(ref ar, i);

            return ar;
        }

        public static TConverted[] ConvertAr<TConverted, TConcrete>(this IEnumerable<TConcrete> original, Func<TConcrete, TConverted> convert, Func<TConcrete, bool>? drop = null) =>
            original.ConvertAr((element, _) => convert(element), drop is not null ? (element, _) => drop(element) : null);

        public static IReadOnlyDictionary<TKey, TValue> ConvertDict<TOriginal, TKey, TValue>(this IEnumerable<TOriginal> original, Func<TOriginal, KeyValuePair<TKey, TValue>> convert, Func<TOriginal, bool>? drop = null)
        {
            Dictionary<TKey, TValue> dict = new();

            foreach (KeyValuePair<TKey, TValue> pair in original.Convert(convert, drop))
            {
                dict.Add(pair.Key, pair.Value);
            }

            return dict;
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

        public static T? Single<T>(this IEnumerable<T> src, Func<T, bool>? predicate = null, bool throwOnEmpty = true) where T : class
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

        public static IEnumerable<T> OfType<T>(this IEnumerable src) where T : class
        {
            foreach (object item in src)
            {
                if (item is T result)
                    yield return result;
            }
        }

        public static string Join(this IEnumerable src)
        {
            StringBuilder sb = new();

            foreach (object item in src)
            {
                sb.Append(item);
            }

            return sb.ToString();
        }
    }
}
