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
    using Abstractions;

    internal sealed class RuntimeCompiledTypeResolutionStrategy : ITypeResolutionStrategy
    {
        public RuntimeCompiledTypeResolutionStrategy(ITypeGenerator generator) => Generator = generator;

        public string? CacheDir { get; internal set; } = AppContext.GetData("AssemblyCacheDir") as string; // tesztek miatt van setter

        public ITypeGenerator Generator { get; }

        public OutputType Type { get; } = OutputType.Module; 

        public Type Resolve(CancellationToken cancellation)
        {
            IUnitSyntaxFactory syntaxFactory = Generator.SyntaxFactory;

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

            syntaxFactory.Build(cancellation);

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
                     allowUnsafe: false,
                     cancellation
                 )
            );

            Type ExtractType(Assembly asm) => asm.GetType
            (
                syntaxFactory.DefinedClasses.Single(), 
                throwOnError: true
            );
        }

        public bool ShouldUse => !new EmbeddedTypeResolutionStrategy(Generator).ShouldUse;

        public string AssemblyName => $"Generated_{MetadataTypeInfo.CreateFrom(Generator.GetType()).GetMD5HashCode()}";
    }
}
