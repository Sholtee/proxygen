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
    /// Contains the loaded proxy <see cref="Type"/>s. Every type (created by this library either run or compile time) contains a 
    /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers">module initializer</see>
    /// that invokes the <see cref="Register(Type, Func{object?, object})"/> method to register the actual type and its activator.
    /// </summary>
    public static class LoadedTypes  // this class is referenced by the generated proxies so it must be public
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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