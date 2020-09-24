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
using System.Runtime.Loader;

namespace Solti.Utils.Proxy.Internals
{
    internal static class Runtime
    {
        public static IReadOnlyList<Assembly> Assemblies { get; } = GetRuntimeAssemblies().ToArray();

        private static IEnumerable<Assembly> GetRuntimeAssemblies()
        {
            yield return typeof(object).Assembly;
#if NETSTANDARD
            yield return GetPlatformAssembly("System.Runtime");

            Assembly netstandard = GetPlatformAssembly("netstandard");
            yield return netstandard;

            //
            // Az implementacio szerelvenyei (kell ha nem "netcoreapp"-ot celzunk) 
            //

            foreach (Assembly netstandardAsm in netstandard.GetReferences())
                yield return netstandardAsm;

            static Assembly GetPlatformAssembly(string name) 
            {
                string asms = (string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

                return AssemblyLoadContext.Default.LoadFromAssemblyPath
                (
                    asms.Split(Path.PathSeparator).Single(asm => Path.GetFileNameWithoutExtension(asm) == name)
                );
            }
#endif
        }
    }
}