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
    internal sealed class RuntimeCompiledTypeResolutionStrategy : ITypeResolution
    {
        public RuntimeCompiledTypeResolutionStrategy(Type generatorType, ClassSyntaxFactory syntaxFactory)
        {
            GeneratorType = generatorType;
            SyntaxFactory = syntaxFactory;
        }

        public string? CacheDir { get; internal set; } = WorkingDirectories.Instance.AssemblyCacheDir; // tesztek miatt van setter

        public ClassSyntaxFactory SyntaxFactory { get; }

        public Type GeneratorType { get; }

        public Type? TryResolve(string assemblyName, CancellationToken cancellation)
        {
            string? cacheFile = null;

            if (!string.IsNullOrEmpty(CacheDir))
            {
                cacheFile = Path.Combine(CacheDir, $"{assemblyName}.dll");

                if (File.Exists(cacheFile)) return ExtractType
                (
                    Assembly.LoadFile(cacheFile)
                );

                Directory.CreateDirectory(CacheDir);
            }

            SyntaxFactory.BuildAndDump(cancellation);

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     SyntaxFactory.Unit!,
                     assemblyName,
                     cacheFile,
                     SyntaxFactory
                        .References
                        .Select(asm => MetadataReference.CreateFromFile(asm.Location!))
                        .ToArray(),
                     customConfig: null,
                     cancellation
                 )
            );

            Type ExtractType(Assembly asm) => asm.GetType
            (
                SyntaxFactory.ClassName, 
                throwOnError: true
            );
        }

        public Type? TryResolve(CancellationToken cancellation) => TryResolve(SyntaxFactory.ContainingAssembly, cancellation);
    }
}
