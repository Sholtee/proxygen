﻿/********************************************************************************
* CodeAnalysisTestsBase.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals.Tests
{
    public abstract class CodeAnalysisTestsBase
    {
        public static CSharpCompilation CreateCompilation(string src, IEnumerable<string> additionalReferences, LanguageVersion languageVersion = LanguageVersion.Latest, bool suppressErrors = false) 
        {
            var result = CSharpCompilation.Create
            (
                "cica",
                new[]
                {
                    CSharpSyntaxTree.ParseText(src, new CSharpParseOptions(languageVersion))
                },
                Internals.Compile.GetPlatformAssemblies(null, TargetFramework.Instance.PlatformAssemblies).Concat
                (
                    additionalReferences
                        .Append(typeof(object).Assembly.Location)
                        .Distinct()
                        .Select
                        (
                            location => MetadataReference.CreateFromFile(location)
                        )
                ),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.All, allowUnsafe: true)
            );

            if (!suppressErrors)
            {
                Diagnostic[] errors = result
                    .GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToArray();
                if (errors.Any())
                    throw new Exception("Bad source");
            }

            return result;
        }

        public static Assembly Compile(string src, Func<Compilation, Compilation> customConfig = null, params Assembly[] additionalReferences)
        {
            using Stream asm = Internals.Compile.ToAssembly
            (
                new CompilationUnitSyntax[] { CSharpSyntaxTree.ParseText(src).GetCompilationUnitRoot() },
                Guid.NewGuid().ToString(),
                null,
                Internals.Compile.GetPlatformAssemblies(null, TargetFramework.Instance.PlatformAssemblies).Concat
                (
                    additionalReferences.Append(typeof(object).Assembly).Select
                    (
                        @ref => MetadataReference.CreateFromFile(@ref.Location)
                    )
                ).ToArray(),
                customConfig
            );

            return Assembly.Load(asm.ToArray());
        }

        public static CSharpCompilation CreateCompilation(string src, params Assembly[] additionalReferences) => CreateCompilation(src, additionalReferences.Select(asm => asm.Location));

        protected class FindAllTypesVisitor : SymbolVisitor
        {
            public List<INamedTypeSymbol> AllTypeSymbols { get; } = new List<INamedTypeSymbol>();

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (INamespaceOrTypeSymbol sym in symbol.GetMembers()) 
                {
                    sym.Accept(this);
                } 
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                AllTypeSymbols.Add(symbol);
                foreach (INamedTypeSymbol childSymbol in symbol.GetTypeMembers())
                {
                    base.Visit(childSymbol);
                }
            }
        }
    }
}
