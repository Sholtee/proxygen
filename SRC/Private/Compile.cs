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
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

#if IGNORE_VISIBILITY
using System.Runtime.CompilerServices;
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class Compile
    {
        public static Assembly ToAssembly(CompilationUnitSyntax root, string asmName, string? outputFile, IReadOnlyCollection<MetadataReference> references, CancellationToken cancellation = default)
        {
            string
                separator = $",{Environment.NewLine}",
                src = root.NormalizeWhitespace().ToFullString(),
                refs = string.Join(separator, references.Select(r => r.Display));

            Debug.WriteLine(src);
            Debug.WriteLine(refs);

            CSharpCompilation compilation = CSharpCompilation.Create
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

            using Stream stm = outputFile != null ? File.Create(outputFile) : (Stream) new MemoryStream();

            EmitResult result = compilation.Emit(stm, cancellationToken: cancellation);

            Debug.WriteLine(string.Join(separator, result.Diagnostics));

            if (!result.Success)
            {
                string failures = string.Join(separator, result
                    .Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error));

                var ex = new Exception(Resources.COMPILATION_FAILED);

                IDictionary extra = ex.Data;

                extra.Add(nameof(failures), failures);
                extra.Add(nameof(src), src);
                extra.Add(nameof(references), refs);

                throw ex;
            }

            stm.Seek(0, SeekOrigin.Begin);

            return AssemblyLoadContext
                .Default
                .LoadFromStream(stm);
        }
    }
}