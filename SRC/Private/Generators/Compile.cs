/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    /// <summary>
    /// Wrapper around the <see cref="CSharpCompilation"/> class. 
    /// </summary>
    internal static class Compile
    {
        /// <summary>
        /// Compiles the given <paramref name="units"/> to an assembly and yields the IL code as a <see cref="Stream"/>. If <paramref name="outputFile"/> is specified the assembly will be persisted on the disk.
        /// </summary>
        /// <remarks>The caller is responsible for loading the returned assembly by calling the <see cref="Assembly.Load(byte[])"/> method.</remarks>
        public static Stream ToAssembly
        (
            IEnumerable<CompilationUnitSyntax> units,
            string asmName,
            string? outputFile,
            IEnumerable<MetadataReference> references,
            LanguageVersion languageVersion,
            ILogger logger,
            Func<Compilation, Compilation>? customConfig = default,
            in CancellationToken cancellation = default
        ) 
        {
            logger.Log(LogLevel.Info, "COMP-200", "Starting compilation", new Dictionary<string, object?>
            {
                ["References"] = references.Select(static @ref => @ref.Display).ToList(),
                ["LanguageVersion"] = languageVersion.ToString(),
                ["AssemblyName"] = asmName,
                ["OutputFile"] = outputFile
            });

            Compilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: units.Select
                (
                    unit => CSharpSyntaxTree.Create(unit, new CSharpParseOptions(languageVersion))
                ),
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

                logger.Log(LogLevel.Info, "COMP-201", "Compilation finished", new Dictionary<string, object?>
                {
                    ["Diagnostics"] = result.Diagnostics.Select(static d => d.ToString()).ToList(),
                    ["Success"] = result.Success
                });

                if (!result.Success)
                {
                    InvalidOperationException ex = new(Resources.COMPILATION_FAILED);

                    IDictionary extra = ex.Data;
                    extra.Add
                    (
                        "failures",
                        result
                            .Diagnostics
                            .Where(static d => d.Severity is DiagnosticSeverity.Error)
                            .Select(static d => d.ToString())
                            .ToList()
                    );
                    extra.Add
                    (
                        "src",
                        string.Join
                        (
                            Environment.NewLine,
                            compilation.SyntaxTrees.Select
                            (
                                static unit => unit
                                    .GetCompilationUnitRoot()
                                    .Stringify()
                            )
                        )
                    );

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