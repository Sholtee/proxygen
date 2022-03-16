/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal static class Cache
    {
        private static class Implementation<TKey, TValue>
        {
            public static readonly ConcurrentDictionary<object, TValue> Value = new();
        }

        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") => Implementation<TKey, Lazy<TValue>>
            .Value
            
            //
            // Ha ugyanazzal a kulccsal hivjuk parhuzamosan a GetOrAdd()-et akkor a factory tobbszor is
            // meghivasra kerulhet (MSDN) -> Lazy
            //

            .GetOrAdd(new { key, scope }, new Lazy<TValue>(factory, LazyThreadSafetyMode.ExecutionAndPublication))
            .Value;
    }
}
