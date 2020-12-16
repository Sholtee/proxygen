/********************************************************************************
* IXxXSymbolExtensionsTestsBase.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals.Tests
{
    public abstract class IXxXSymbolExtensionsTestsBase
    {
        protected static CSharpCompilation CreateCompilation(string src, params Assembly[] additionalReferences) 
        {
            var result = CSharpCompilation.Create
            (
                "cica",
                new[]
                {
                    CSharpSyntaxTree.ParseText(src)
                },
                Runtime.Assemblies.Concat(additionalReferences).Distinct().Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                CompilationOptionsFactory.Create(allowUnsafe: true)
            );

            Diagnostic[] errors = result.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Any()) throw new Exception("Bad source");

            return result;
        }

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
                foreach (var childSymbol in symbol.GetTypeMembers())
                {
                    base.Visit(childSymbol);
                }
            }
        }
    }
}
