/********************************************************************************
* TypeEmitter.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Compiles SyntaxFactory outputs to materialized <see cref="Type"/>s.
    /// </summary>
    /// <remarks>Static methods of this type are thread safe while instance methods are NOT.</remarks>
    public abstract record TypeEmitter
    {
        private static AssemblyLoadContext AssemblyLoader { get; } = AssemblyLoadContext.Default;

        private static void RunInitializers(Assembly assembly)
        {
            //
            // Module initializers won't run if the containing assembly being loaded
            // from code.
            //

            foreach (Module module in assembly.GetModules())
            {
                RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
            }
        }

        private static Type? GetInstanceFromCache(string className, bool throwOnMissing)
        {
            if (!LoadedTypes.Values.TryGetValue(className, out Type type) && throwOnMissing)
                throw new TypeLoadException(className);  // FIXME: somehow try to set the TypeName property
            return type;
        }

        private protected abstract ProxyUnitSyntaxFactory CreateMainUnit(string? asmName, ReferenceCollector referenceCollector);

        private protected abstract IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector);

        internal Type Emit(string? asmName, string? asmCacheDir, CancellationToken cancellation)
        {
            ReferenceCollector referenceCollector = new();

            ProxyUnitSyntaxFactory mainUnit = CreateMainUnit(asmName, referenceCollector);

            string className = mainUnit.DefinedClasses.Single()!; // TODO: support multiple defined classes

            //
            // 1) Type already loaded (for e.g. in case of embedded types)
            //

            Type? type = GetInstanceFromCache(className, throwOnMissing: false);
            if (type is not null)
                return type;

            //
            // 2) If the assembly physically stored, try to load it from the cache directory.
            // 

            string? cacheFile = null;

            if (!string.IsNullOrEmpty(asmCacheDir))
            {
                cacheFile = Path.Combine(asmCacheDir, $"{mainUnit.ContainingAssembly}.dll");

                if (File.Exists(cacheFile))
                {
                    RunInitializers
                    (
                        //
                        // Will throw if the assembly already loaded.
                        //

                        AssemblyLoader.LoadFromAssemblyPath(cacheFile)
                    );

                    return GetInstanceFromCache(className, throwOnMissing: true)!;
                }

                //
                // Couldn't find the assembly, in the next step we will compile it. Make sure the
                // cache directory exits.
                //

                Directory.CreateDirectory(asmCacheDir);
            }

            //
            // 3) Compile the assembly from the scratch.
            //

            List<UnitSyntaxFactoryBase> units = new
            (
                CreateChunks(referenceCollector)
            )
            {
                mainUnit
            };

            using Stream asm = Compile.ToAssembly
            (
                units.ConvertAr
                (
                    unit => unit.ResolveUnitAndDump(cancellation)
                ),
                mainUnit.ContainingAssembly,
                cacheFile,
                referenceCollector.References.ConvertAr
                (
                    static asm => MetadataReference.CreateFromFile(asm.Location!)
                ),
                customConfig: null,
                cancellation
            );

            RunInitializers
            (
                //
                // Will throw if the assembly already loaded.
                //

                AssemblyLoader.LoadFromStream(asm)
            );

            return GetInstanceFromCache(className, throwOnMissing: true)!;
        }
#if DEBUG
        internal string GetDefaultAssemblyName() => CreateMainUnit(null, null!).ContainingAssembly;
#endif
    }
}