/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal class ProxyEmbedder
    {
        public static IEnumerable<AttributeSyntax> GetAttributes<TAttribute>(CSharpCompilation compilation, CancellationToken cancellation = default) where TAttribute : Attribute
        {
            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel sm = compilation.GetSemanticModel(syntaxTree);

                foreach (AttributeSyntax node in syntaxTree.GetRoot(cancellation).DescendantNodes().OfType<AttributeSyntax>())
                {
                    bool found = SymbolEqualityComparer.Default.Equals
                    (
                        sm.GetTypeInfo(node, cancellation).Type,
                        compilation.GetTypeByMetadataName(typeof(TAttribute).FullName)
                    );
                    if (found) yield return node;
                }
            }
        }
    }
}
