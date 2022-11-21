/********************************************************************************
* InterfaceMap.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Cached interface mappings. Intended for private use only.
    /// </summary>
    public static class InterfaceMap<TInterface, TImplementation>
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
                    dict.Add(mapping.InterfaceMethods[i], mapping.TargetMethods[i]);
                }
            }

            return dict;
        }

        /// <summary>
        /// Returns the cached mappings.
        /// </summary>
        public static IReadOnlyDictionary<MethodInfo, MethodInfo> Mappings { get; } = GetMappings();
    }
}
