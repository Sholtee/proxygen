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
        public static Assembly ToAssembly(CompilationUnitSyntax root, string asmName, IReadOnlyList<Assembly> references)
        {
            Debug.WriteLine(root.NormalizeWhitespace().ToFullString());
            Debug.WriteLine(string.Join(Environment.NewLine, references));
 
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
                references: Runtime
                    .Assemblies
                    .Concat(references)
#if IGNORE_VISIBILITY
                    .Append(typeof(IgnoresAccessChecksToAttribute).Assembly())
#endif
                    .Distinct()
                    .Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                options: CompilationOptionsFactory.Create()
            );

            using (Stream stm = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stm);

                Debug.WriteLine(string.Join(Environment.NewLine, result.Diagnostics));

                if (!result.Success)
                {
                    string failures = string.Join($",{Environment.NewLine}", result
                        .Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error));

                    var ex = new Exception(Resources.COMPILATION_FAILED);

                    IDictionary extra = ex.Data;

                    extra.Add(nameof(failures), failures);
                    extra.Add("src", root.NormalizeWhitespace().ToFullString());
                    extra.Add(nameof(references), references);

                    throw ex;
                }

                stm.Seek(0, SeekOrigin.Begin);

                return AssemblyLoadContext
                    .Default
                    .LoadFromStream(stm);
            } 
        }
    }
}