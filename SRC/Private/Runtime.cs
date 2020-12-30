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

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string asms)
                yield break;

            foreach (string assemblyPath in asms.Split(Path.PathSeparator))
            {
                string fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (mandatoryAssemblies.Contains(fileName))
                    yield return Assembly.LoadFile(assemblyPath);
            }
#endif
        }
    }
}