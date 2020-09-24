/********************************************************************************
* Runtime.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class Runtime
    {
        public static IReadOnlyList<Assembly> Assemblies { get; } = GetRuntimeAssemblies().ToArray();

        private static IEnumerable<Assembly> GetRuntimeAssemblies()
        {
            yield return typeof(object).Assembly;
#if NETSTANDARD
            string[] mandatoryAssemblies =
            {
                "System.Runtime",
                "netstandard"
            };

            foreach (string assemblyPath in ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator))
            {
                string fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (mandatoryAssemblies.Contains(fileName))
                    yield return Assembly.LoadFile(assemblyPath);
            }
#endif
        }
    }
}