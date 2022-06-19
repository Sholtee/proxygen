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
    public abstract record TypeEmitter
    {
        private static readonly ConcurrentDictionary<string, Type> FInstances = new();

        private static AssemblyLoadContext AssemblyLoader { get; } = AssemblyLoadContext.Default;

        private protected abstract ProxyUnitSyntaxFactory CreateMainUnit(string? asmName, ReferenceCollector referenceCollector);

        private protected abstract IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector);

        internal Type Emit(string? asmName, string? asmCacheDir, CancellationToken cancellation)
        {
            ReferenceCollector referenceCollector = new();

            ProxyUnitSyntaxFactory mainUnit = CreateMainUnit(asmName, referenceCollector);

            string className = mainUnit.DefinedClasses.Single()!; // TODO: Mi van ha tobb van?

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
                cacheFile = Path.Combine(asmCacheDir, $"{mainUnit.ContainingAssembly}.dll");

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

            List<UnitSyntaxFactoryBase> units = new
            (
                CreateChunks(referenceCollector)
            );
            units.Add(mainUnit);

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

        internal static void RegisterInstance(Type instance) => FInstances[instance.Name] = instance;
#if DEBUG
        internal string GetDefaultAssemblyName() => CreateMainUnit(null, null!).ContainingAssembly;
#endif
    }
}