/********************************************************************************
* TypeEmitter.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal static class TypeEmitter
    {
        private static readonly ConcurrentDictionary<string, Type> FInstances = new();

        /// <summary>
        /// Registers a generated class.
        /// </summary>
        public static void RegisterInstance(Type instance) => FInstances[instance.Name] = instance;

        private static AssemblyLoadContext AssemblyLoader { get; } = AssemblyLoadContext.Default;

        public static Type Emit(ProxyUnitSyntaxFactory syntaxFactory, string? asmCacheDir, in CancellationToken cancellation)
        {
            string className = syntaxFactory.DefinedClasses.Single()!; // TODO: Mi van ha tobb van?

            //
            // 1) A tipus mar be lett toltve (pl beagyazott tipus eseten)
            //

            if (FInstances.TryGetValue(className, out Type type))
                return type;

            //
            // 2) Ha fizikailag mentjuk a szerelvenyt akkor lehet korabban mar letre lett hozva
            // 

            string? cacheFile = null;

            if (!string.IsNullOrEmpty(asmCacheDir))
            {
                cacheFile = Path.Combine(asmCacheDir, $"{syntaxFactory.ContainingAssembly}.dll");

                if (File.Exists(cacheFile)) 
                    return ExtractType
                    (
                        //
                        // Kivetelt dob ha mar egyszer be lett toltve
                        //

                        AssemblyLoader.LoadFromAssemblyPath(cacheFile)
                    );

                Directory.CreateDirectory(asmCacheDir);
            }

            //
            // 3) Egyik korabbi sem nyert akkor leforditjuk
            //

            using Stream asm = Compile.ToAssembly
            (
                new CompilationUnitSyntax[] { syntaxFactory.ResolveUnitAndDump(cancellation) },
                syntaxFactory.ContainingAssembly,
                cacheFile,
                syntaxFactory
                    .ReferenceCollector!
                    .References
                    .Convert(asm => MetadataReference.CreateFromFile(asm.Location!)),
                customConfig: null,
                cancellation
            );

            return ExtractType
            (
                AssemblyLoader.LoadFromStream(asm)
            );

            Type ExtractType(Assembly asm)
            {
                //
                // Fasz se tudja miert de ha dinamikusan toltunk be egy szerelvenyt akkor annak a module-inicializaloja
                // nem fog lefutni... Ezert jol meghivjuk kezzel
                //

                foreach (Module module in asm.GetModules())
                {
                    RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
                }

                return asm.GetType(className, throwOnError: true);
            }
        }
    }
}