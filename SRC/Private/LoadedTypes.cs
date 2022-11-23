/********************************************************************************
* LoadedTypes.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Contains the loaded proxy <see cref="Type"/>s
    /// </summary>
    public static class LoadedTypes
    {
        private static readonly ConcurrentDictionary<string, Type> FStore = new();

        /// <summary>
        /// The values.
        /// </summary>
        public static IReadOnlyDictionary<string, Type> Values => FStore;

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