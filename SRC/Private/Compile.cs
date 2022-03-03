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
        public static Stream ToAssembly(CompilationUnitSyntax root, string asmName, string? outputFile, IEnumerable<MetadataReference> references, Func<Compilation, Compilation>? customConfig = null, CancellationToken cancellation = default)
        {
            string separator = $",{Environment.NewLine}";

            Compilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: new []
                {
                    CSharpSyntaxTree.Create
                    (
                        root: root
                    )
                },
                references: references,
                options: CompilationOptionsFactory.Create()
            );

            if (customConfig is not null)
                compilation = customConfig(compilation);

            Stream stm = outputFile is not null ? File.Create(outputFile) : (Stream) new MemoryStream();
            try
            {
                EmitResult result = compilation.Emit(stm, cancellationToken: cancellation);

                Debug.WriteLine(string.Join(separator, result.Diagnostics));

                if (!result.Success)
                {
                    string src = root.NormalizeWhitespace(eol: Environment.NewLine).ToFullString();

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