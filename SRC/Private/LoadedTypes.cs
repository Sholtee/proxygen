/********************************************************************************
* LoadedTypes.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Contains the loaded proxy <see cref="Type"/>s
    /// </summary>
    public static class LoadedTypes  // this class is referenced by the generated proxies so it must be public
    {
        private static readonly ConcurrentDictionary<string, Type> FStore = new();

        /// <summary>
        /// Tries to grab the proxy <see cref="Type"/> by its name.
        /// </summary>
        public static bool TryGet(string name, out Type type) =>
            FStore.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out type);

        /// <summary>
        /// Registers a new <see cref="Type"/>
        /// </summary>
        /// <param name="instance"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Register(Type instance)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            if (!FStore.TryAdd(instance.Name, instance))
            {
                Trace.TraceWarning($"Type already loaded: {instance.Name}");
            }
        }
    }
}