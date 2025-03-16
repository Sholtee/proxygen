/********************************************************************************
* TypeEmitter.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Compiles SyntaxFactory outputs to materialized <see cref="Type"/>s.
    /// </summary>
    /// <remarks>Static methods of this type are thread safe while instance methods are NOT.</remarks>
    public abstract class TypeEmitter
    {
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
            if (!LoadedTypes.TryGet(className, out Type type) && throwOnMissing)
                throw new TypeLoadException(className);  // FIXME: somehow try to set the TypeName property
            return type;
        }

        private protected abstract ProxyUnitSyntaxFactory CreateMainUnit(string? asmName, ReferenceCollector referenceCollector);

        private protected abstract IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector);

        internal Task<Type> EmitAsync(string? asmName, string? asmCacheDir, CancellationToken cancellation)
        {
            ReferenceCollector referenceCollector = new();

            ProxyUnitSyntaxFactory mainUnit = CreateMainUnit(asmName, referenceCollector);

            //
            // 1) Type already loaded (for e.g. in case of embedded types)
            //

            Type? type = GetInstanceFromCache(mainUnit.ExposedClass, throwOnMissing: false);
            if (type is not null)
                return Task.FromResult(type);

            return Task<Type>.Factory.StartNew(() =>
            {
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
                            Assembly.LoadFile(cacheFile)
                        );

                        return GetInstanceFromCache(mainUnit.ExposedClass, throwOnMissing: true)!;
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
                    units
                        .Select(unit => unit.ResolveUnitAndDump(cancellation))
                        .ToImmutableList(),
                    mainUnit.ContainingAssembly,
                    cacheFile,
                    referenceCollector
                        .References
                        .Select(static asm => MetadataReference.CreateFromFile(asm.Location!))
                        .ToImmutableList(),
                    customConfig: null,
                    cancellation
                );

                RunInitializers
                (
                    Assembly.Load(asm.ToArray())
                );

                return GetInstanceFromCache(mainUnit.ExposedClass, throwOnMissing: true)!;
            }, cancellation);
        }
#if DEBUG
        internal string GetDefaultAssemblyName() => CreateMainUnit(null, null!).ContainingAssembly;
#endif
    }
}