/********************************************************************************
* TypeEmitter.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
        }

        private static TypeContext? GetInstanceFromCache(string className, bool throwOnMissing)
        {
            if (!LoadedTypes.TryGet(className, out TypeContext type) && throwOnMissing)
                throw new TypeLoadException(className);  // FIXME: somehow try to set the TypeName property
            return type;
        }

        private protected abstract ProxyUnitSyntaxFactoryBase CreateMainUnit(SyntaxFactoryContext context);

        private protected abstract IEnumerable<UnitSyntaxFactoryBase> CreateChunks(SyntaxFactoryContext context);

        private protected Task<TypeContext> EmitAsync(CancellationToken cancellation) => EmitAsync
        (
            SyntaxFactoryContext.Default with { ReferenceCollector = new ReferenceCollector() },
            cancellation
        );

        #if DEBUG
        internal
        #else
        private protected
        #endif
        Task<TypeContext> EmitAsync(SyntaxFactoryContext context, CancellationToken cancellation)
        {
            Debug.Assert(context.OutputType is OutputType.Module, $"Incompatible {nameof(context.OutputType)}");
            Debug.Assert(context.ReferenceCollector is not null, $"{nameof(context.ReferenceCollector)} cannot be null when compiling a module");

            ProxyUnitSyntaxFactoryBase mainUnit = CreateMainUnit(context);

            //
            // 1) Type already loaded (for e.g. in case of embedded types)
            //

            TypeContext? type = GetInstanceFromCache(mainUnit.ExposedClass, throwOnMissing: false);
            if (type is not null)
                return Task.FromResult(type);

            return Task<TypeContext>.Factory.StartNew(() =>
            {
                //
                // 2) If the assembly physically stored, try to load it from the cache directory.
                // 

                string? cacheFile = null;

                if (!string.IsNullOrEmpty(context.Config.AssemblyCacheDir))
                {
                    cacheFile = Path.Combine(context.Config.AssemblyCacheDir, $"{mainUnit.ContainingAssembly}.dll");

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

                    Directory.CreateDirectory(context.Config.AssemblyCacheDir);
                }

                //
                // 3) Compile the assembly from the scratch.
                //

                using Stream asm = Compile.ToAssembly
                (
                    CreateChunks(context)
                        .Append(mainUnit)
                        .Select(unit => unit.ResolveUnitAndDump(cancellation))

                        //
                        // We need to craft the syntax trees first in order to have the references available
                        //

                        .ToList(),
                    mainUnit.ContainingAssembly,
                    cacheFile,
                    context
                        .ReferenceCollector!
                        .References
                        .Select(static asm => MetadataReference.CreateFromFile(asm.Location!)),
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
        internal string GetDefaultAssemblyName() => CreateMainUnit(SyntaxFactoryContext.Default).ContainingAssembly;
#endif
    }
}