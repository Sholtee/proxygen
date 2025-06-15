/********************************************************************************
* TypeEmitter.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Buffers;
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

        /// <summary>
        /// When overridden, creates the factory that is responsible for crafting the main (exported) class.
        /// </summary>
        private protected abstract ProxyUnitSyntaxFactoryBase CreateMainUnit(SyntaxFactoryContext context);

        /// <summary>
        /// When overridden, creates the factory that is responsible for crafting helper classes (such as <see cref="ModuleInitializerAttribute"/> on older frameworks)
        /// </summary>
        private protected abstract IEnumerable<UnitSyntaxFactoryBase> CreateChunks(SyntaxFactoryContext context);

        /// <summary>
        /// Emits the actual <see cref="Type"/>. When possible this method tries to avoid initiating new compilations.
        /// </summary>
        private protected async Task<TypeContext> EmitAsync(CancellationToken cancellation)
        {
            RuntimeConfig config = new();

            using LoggerFactory loggerFactory = new(config);

            return await EmitAsync
            (
                config,
                SyntaxFactoryContext.Default with
                {
                    ReferenceCollector = new ReferenceCollector(),
                    LoggerFactory = loggerFactory
                },
                cancellation
            );
        }

        /// <summary>
        /// The core logic of <see cref="EmitAsync(CancellationToken)"/>.
        /// </summary>
        #if DEBUG
        internal
        #else
        private 
        #endif
        static Assembly LoadAssembly(Stream stm)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((int) stm.Length);
            try
            {
                stm.Read(buffer, 0, (int) stm.Length);
                return Assembly.Load(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        #if DEBUG
        internal
        #else
        private 
        #endif
        Task<TypeContext> EmitAsync(IAssemblyCachingConfiguration cachingConfiguration, SyntaxFactoryContext context, CancellationToken cancellation)
        {
            Debug.Assert(context.OutputType is OutputType.Module, $"Incompatible {nameof(context.OutputType)}");
            Debug.Assert(context.ReferenceCollector is not null, $"{nameof(context.ReferenceCollector)} cannot be null when compiling a module");

            ProxyUnitSyntaxFactoryBase mainUnit = CreateMainUnit(context);
            
            //
            // We don't want to put the compilation logs into a separate file so reuse the log
            // session created by the syntax factory.
            //

            ILogger logger = mainUnit.Logger;
            
            logger.Log(LogLevel.Info, "EMIT-200", $"Emitting type: \"{mainUnit.ExposedClass}\"");

            //
            // 1) Type already loaded (for e.g. in case of embedded types)
            //

            TypeContext? type = GetInstanceFromCache(mainUnit.ExposedClass, throwOnMissing: false);
            if (type is not null)
            {
                logger.Log(LogLevel.Info, "EMIT-201", $"\"{mainUnit.ExposedClass}\" found in cache");
                return Task.FromResult(type);
            }

            return Task<TypeContext>.Factory.StartNew(() =>
            {
                logger.Log(LogLevel.Info, "EMIT-202", $"Starting new compilation task");

                //
                // 2) If the assembly physically stored, try to load it from the cache directory.
                // 

                string? cacheFile = null;

                if (!string.IsNullOrEmpty(cachingConfiguration.AssemblyCacheDir))
                {
                    cacheFile = Path.Combine(cachingConfiguration.AssemblyCacheDir, $"{mainUnit.ContainingAssembly}.dll");

                    if (File.Exists(cacheFile))
                    {
                        logger.Log(LogLevel.Info, "EMIT-203", $"Cache file exists, loading it");

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

                    Directory.CreateDirectory(cachingConfiguration.AssemblyCacheDir);
                }

                //
                // 3) Compile the assembly from the scratch.
                //
          
                logger.Log(LogLevel.Info, "EMIT-204", $"Invoking the compiler");

                using Stream asm = Compile.ToAssembly
                (
                    CreateChunks(context)
                        .Append(mainUnit)
                        .Select(unit => unit.ResolveUnit(null!, cancellation))

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
                    context.LanguageVersion,
                    logger,
                    customConfig: null,
                    cancellation
                );

                RunInitializers
                (
                    LoadAssembly(asm)
                );

                logger.Log(LogLevel.Info, "EMIT-204", $"Built type is ready");

                return GetInstanceFromCache(mainUnit.ExposedClass, throwOnMissing: true)!;
            }, cancellation);
        }
#if DEBUG
        /// <summary>
        /// Returns the name of the underlying <see cref="Assembly"/> that will be created during the emit process.
        /// </summary>
        internal string GetDefaultAssemblyName() => CreateMainUnit(SyntaxFactoryContext.Default).ContainingAssembly;
#endif
    }
}