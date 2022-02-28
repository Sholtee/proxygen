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
        public RuntimeCompiledTypeResolutionStrategy(ProxyUnitSyntaxFactory syntaxFactory) => SyntaxFactory = syntaxFactory;

        public string? CacheDir 
        { 
            get;
#if DEBUG
            internal set;
#endif
        } = WorkingDirectories.Instance.AssemblyCacheDir;

        public ProxyUnitSyntaxFactory SyntaxFactory { get; }

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

            CompilationUnitSyntax unit = SyntaxFactory.ResolveUnitAndDump(cancellation);

            return ExtractType
            (
                 Compile.ToAssembly
                 (
                     unit,
                     assemblyName,
                     cacheFile,
                     SyntaxFactory
                        .ReferenceCollector!
                        .References
                        .Convert(asm => MetadataReference.CreateFromFile(asm.Location!)),
                     customConfig: null,
                     cancellation
                 )
            );

            Type ExtractType(Assembly asm) => asm.GetType
            (
                SyntaxFactory.DefinedClasses.Single(), 
                throwOnError: true
            );
        }

        public Type? TryResolve(CancellationToken cancellation) => TryResolve(SyntaxFactory.ContainingAssembly, cancellation);
    }
}
