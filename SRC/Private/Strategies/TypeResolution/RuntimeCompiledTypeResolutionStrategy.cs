/********************************************************************************
* RuntimeCompiledTypeResolutionStrategy.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class RuntimeCompiledTypeResolutionStrategy : ITypeResolutionStrategy
    {
        public RuntimeCompiledTypeResolutionStrategy(Type generatorType) => GeneratorType = generatorType;

        public string? CacheDir { get; internal set; } = WorkingDirectories.AssemblyCacheDir; // tesztek miatt van setter

        public Type GeneratorType { get; }

        public OutputType Type { get; } = OutputType.Module; 

        public Type Resolve(IUnitSyntaxFactory syntaxFactory, CancellationToken cancellation)
        {
            string? cacheFile = null;

            if (!string.IsNullOrEmpty(CacheDir))
            {
                cacheFile = Path.Combine(CacheDir, $"{AssemblyName}.dll");

                if (File.Exists(cacheFile)) return ExtractType
                (
                    Assembly.LoadFile(cacheFile)
                );

                if (!Directory.Exists(CacheDir))
                    Directory.CreateDirectory(CacheDir);
            }

            syntaxFactory.BuildAndDump(cancellation);

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     syntaxFactory.Unit!,
                     AssemblyName,
                     cacheFile,
                     syntaxFactory
                        .References
                        .Select(asm => MetadataReference.CreateFromFile(asm.Location!))
                        .ToArray(),
                     customConfig: null,
                     cancellation
                 )
            );

            Type ExtractType(Assembly asm) => asm.GetType
            (
                syntaxFactory.DefinedClasses.Single(), 
                throwOnError: true
            );
        }

        public bool ShouldUse => !new EmbeddedTypeResolutionStrategy(GeneratorType).ShouldUse;

        public string AssemblyName => $"Generated_{MetadataTypeInfo.CreateFrom(GeneratorType).GetMD5HashCode()}";
    }
}
