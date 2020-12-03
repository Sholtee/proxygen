/********************************************************************************
* IXxXSymbolExtensionsTestsBase.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
        protected static CSharpCompilation CreateCompilation(string src, params Assembly[] additionalReferences) => CSharpCompilation.Create
        (
            "cica",
            new[]
            {
                CSharpSyntaxTree.ParseText(src)
            },
            Runtime.Assemblies.Concat(additionalReferences).Distinct().Select(asm => MetadataReference.CreateFromFile(asm.Location!))
        );

        protected class FindAllTypesVisitor : SymbolVisitor
        {
            public List<INamedTypeSymbol> AllTypeSymbols { get; } = new List<INamedTypeSymbol>();

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                Parallel.ForEach(symbol.GetMembers(), s => s.Accept(this));
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
