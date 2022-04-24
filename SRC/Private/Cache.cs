/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;

namespace Solti.Utils.Proxy.Internals
{
    internal static class Cache
    {
        private static class Implementation<TKey, TValue>
        {
            public static readonly ConcurrentDictionary<TKey, TValue> Value = new();
        }

        private sealed class LazySlim<TValue, TContext> where TValue : class
        {
            private readonly Func<TContext, TValue> FFactory;
            private readonly TContext FContext;
            private TValue? FValue;

            public LazySlim(/*static*/ Func<TContext, TValue> factory, TContext context)
            {
                FFactory = factory;
                FContext = context;
            }

            public TValue Value
            {
                get 
                {
                    if (FValue is null)
                        lock (FFactory)
                            FValue ??= FFactory(FContext);
                    return FValue;
                }
            }
        }
        
        public static TValue GetOrAdd<TKey, TValue>(TKey key, /*static*/ Func<TKey, TValue> factory) where TValue: class => Implementation<TKey, LazySlim<TValue, TKey>>
            .Value
            
            //
            // Ha ugyanazzal a kulccsal hivjuk parhuzamosan a GetOrAdd()-et akkor a factory tobbszor is
            // meghivasra kerulhet (MSDN) -> Lazy
            //

            .GetOrAdd(key, new LazySlim<TValue, TKey>(factory, key))
            .Value;
    }
}
