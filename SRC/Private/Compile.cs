/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class Compile
    {
        internal static IEnumerable<MetadataReference> GetPlatformAssemblies(string? platformAsmsDir, IEnumerable<string> platformAsms)
        {
            if (!string.IsNullOrEmpty(platformAsmsDir) && Directory.Exists(platformAsmsDir))
            {
                return GetFilteredPlatformAssemblies
                (
                    Directory.EnumerateFiles(platformAsmsDir, "*.dll", SearchOption.TopDirectoryOnly)
                );
            }
            else
            {
                string tpa = (string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

                return GetFilteredPlatformAssemblies
                (
                    tpa.Split(Path.PathSeparator)
                );
            }

            IEnumerable<MetadataReference> GetFilteredPlatformAssemblies(IEnumerable<string> allAssemblies)
            {
                foreach (string asm in allAssemblies)
                {
                    foreach (string platformAsm in platformAsms)
                    {
                        if (Path.GetFileName(asm).Equals(platformAsm, StringComparison.OrdinalIgnoreCase))
                            yield return MetadataReference.CreateFromFile(asm);
                    }
                }
            }
        }

        private static readonly Lazy<ImmutableHashSet<MetadataReference>> FPlatformAssemblies = new
        (
            () => ImmutableHashSet.CreateRange
            (
                MetadataReferenceComparer.Instance,
                GetPlatformAssemblies
                (
                    TargetFramework.Instance.PlatformAssembliesDir,
                    TargetFramework.Instance.PlatformAssemblies
                )
            ),
            LazyThreadSafetyMode.ExecutionAndPublication
        );

        public static ImmutableHashSet<MetadataReference> PlatformAssemblies => FPlatformAssemblies.Value;

        public static Stream ToAssembly(
            IEnumerable<CompilationUnitSyntax> units,
            string asmName,
            string? outputFile,
            IEnumerable<MetadataReference> references,
            Func<Compilation, Compilation>? customConfig = null,
            in CancellationToken cancellation = default) 
        {
            Compilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: units.Convert(unit => CSharpSyntaxTree.Create(unit)),
                references: PlatformAssemblies.Union(references),
                options: new CSharpCompilationOptions
                (
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    metadataImportOptions: MetadataImportOptions.All,
                    optimizationLevel: OptimizationLevel.Release
                )
            );

            if (customConfig is not null)
                compilation = customConfig(compilation);

            Stream stm = outputFile is not null ? File.Create(outputFile) : new MemoryStream();
            try
            {
                EmitResult result = compilation.Emit(stm, cancellationToken: cancellation);

                Debug.WriteLine(string.Join($",{Environment.NewLine}", result.Diagnostics));

                if (!result.Success)
                {
                    string src = string.Join
                    (
                        Environment.NewLine,
                        compilation
                            .SyntaxTrees
                            .Convert(unit => unit
                                .GetCompilationUnitRoot()
                                .NormalizeWhitespace(eol: Environment.NewLine)
                                .ToFullString())
                    );

                    string[]
                        failures = result
                            .Diagnostics
                            .ConvertAr(d => d.ToString(), d => d.Severity is not DiagnosticSeverity.Error),
                        refs = references.ConvertAr(r => r.Display!);

                    InvalidOperationException ex = new(Resources.COMPILATION_FAILED);

                    IDictionary extra = ex.Data;

                    extra.Add(nameof(failures), failures);
                    extra.Add(nameof(src), src);
                    extra.Add(nameof(references), refs);

                    throw ex;
                }

                stm.Seek(0, SeekOrigin.Begin);
                return stm;
            }
            catch
            {
                stm.Dispose();
                throw;
            }
        }
    }
}