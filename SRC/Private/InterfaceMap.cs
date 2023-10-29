/********************************************************************************
* InterfaceMap.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Cached interface mappings. Intended for private use only.
    /// </summary>
    public static class InterfaceMap<TInterface, TImplementation> // this class is referenced by the generated proxies so it must be public
        where TInterface: class
        where TImplementation: TInterface
    {
        private static IReadOnlyDictionary<MethodInfo, MethodInfo> GetMappings()
        {
            Dictionary<MethodInfo, MethodInfo> dict = new();

            foreach (Type iface in typeof(TInterface).GetHierarchy())
            {
                InterfaceMapping mapping = typeof(TImplementation).GetInterfaceMap(iface);

                for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
                {
                    try
                    {
                        dict.Add(mapping.InterfaceMethods[i], mapping.TargetMethods[i]);
                    }
                    catch (Exception ex)
                    {
                        //
                        // We dont wanna a TypeInitializationException to be thrown so eat the exception
                        //

                        Trace.TraceWarning($"Cannot register mapping (${mapping.InterfaceMethods[i].Name}): {ex.Message}");
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Returns the cached mappings.
        /// </summary>
        public static IReadOnlyDictionary<MethodInfo, MethodInfo> Value { get; } = GetMappings();
    }
}
