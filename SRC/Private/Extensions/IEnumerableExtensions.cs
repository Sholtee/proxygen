/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IEnumerableExtensions
    {
        public static int? IndexOf<T>(this IEnumerable<T> src, T item, IEqualityComparer<T> comparer) => src
            .Select((it, i) => new
            {
                Item = it,
                Index = i
            })
            .Where(x => comparer.Equals(x.Item, item))
            .Select(x => (int?) x.Index)
            .SingleOrDefault();

        public static int? IndexOf<T>(this IEnumerable<T> src, T item) => src.IndexOf(item, EqualityComparer<T>.Default);
    }
}
