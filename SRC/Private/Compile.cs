/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static Stream ToAssembly
        (
            IEnumerable<CompilationUnitSyntax> units,
            string asmName,
            string? outputFile,
            IEnumerable<MetadataReference> references,
            Func<Compilation, Compilation>? customConfig = default,
            in CancellationToken cancellation = default
        ) 
        {
            Compilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: units.Select(static unit => CSharpSyntaxTree.Create(unit)),
                references: references.Union
                (              
                    PlatformAssemblies.References,
                    MetadataReferenceComparer.Instance
                ),
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
                            .Select
                            (
                                static unit => unit
                                    .GetCompilationUnitRoot()
                                    .NormalizeWhitespace(eol: Environment.NewLine)
                                    .ToFullString()
                            )
                    );

                    string[]
                        failures = 
                        [
                            ..result
                                .Diagnostics
                                .Where(static d => d.Severity is DiagnosticSeverity.Error)
                                .Select(static d => d.ToString())
                        ],
                        refs =
                        [
                            ..compilation
                                .References
                                .Select(static r => r.Display!)
                        ];

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