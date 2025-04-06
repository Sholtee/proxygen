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
    /// Describes a loaded generated <see cref="Type"/>.
    /// </summary>
    public record TypeContext(Type Type, Func<object?, object> Activator);

    /// <summary>
    /// Contains the loaded proxy <see cref="Type"/>s
    /// </summary>
    public static class LoadedTypes  // this class is referenced by the generated proxies so it must be public
    {
        private static readonly ConcurrentDictionary<string, TypeContext> FStore = new();

        /// <summary>
        /// Tries to grab the proxy <see cref="Type"/> by its name.
        /// </summary>
        internal static bool TryGet(string name, out TypeContext type) => FStore.TryGetValue(name, out type);

        /// <summary>
        /// Registers a new <see cref="Type"/> alongside its activator
        /// </summary>
        public static void Register(Type type, Func<object?, object> activator)
        {
            if (!FStore.TryAdd(type.Name, new TypeContext(type ?? throw new ArgumentNullException(nameof(type)), activator ?? throw new ArgumentNullException(nameof(activator)))))
            {
                Trace.TraceWarning($"Type already loaded: {type.Name}");
            }
        }
    }
}