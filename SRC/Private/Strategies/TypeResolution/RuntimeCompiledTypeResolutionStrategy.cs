/********************************************************************************
* RuntimeCompiledTypeResolutionStrategy.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class RuntimeCompiledTypeResolutionStrategy : ITypeResolution
    {
        public RuntimeCompiledTypeResolutionStrategy(Type generatorType, ProxyUnitSyntaxFactory syntaxFactory)
        {
            GeneratorType = generatorType;
            ProxyUnitSyntaxFactory = syntaxFactory;
        }

        public string? CacheDir 
        { 
            get;
#if DEBUG
            internal set;
#endif
        } = WorkingDirectories.Instance.AssemblyCacheDir;

        public Type GeneratorType { get; }

        public ProxyUnitSyntaxFactory ProxyUnitSyntaxFactory { get; }

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

            CompilationUnitSyntax unit = ProxyUnitSyntaxFactory.ResolveUnitAndDump(cancellation);

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     unit,
                     assemblyName,
                     cacheFile,
                     ProxyUnitSyntaxFactory
                        .ReferenceCollector!
                        .References
                        .Convert(asm => MetadataReference.CreateFromFile(asm.Location!)),
                     customConfig: null,
                     cancellation
                 )
            );

            Type ExtractType(Assembly asm) => asm.GetType
            (
                ProxyUnitSyntaxFactory.DefinedClasses.Single(), 
                throwOnError: true
            );
        }

        public Type? TryResolve(CancellationToken cancellation) => TryResolve(ProxyUnitSyntaxFactory.ContainingAssembly, cancellation);
    }
}
