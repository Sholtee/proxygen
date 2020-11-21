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
    using Abstractions;
    using Attributes;

    internal class ProxyEmbedder
    {
        public CSharpCompilation Compilation { get; }

        public CancellationToken Cancellation { get; }

        public ProxyEmbedder(CSharpCompilation compilation, in CancellationToken cancellation) 
        {
            Compilation = compilation;
            Cancellation = cancellation;
        }

        private bool Is(ISymbol? s, Type t) => SymbolEqualityComparer.Default.Equals(s, Compilation.GetTypeByMetadataName(t.FullName));

        internal IEnumerable<AttributeSyntax> GetAttributes<TAttribute>() where TAttribute : Attribute
        {
            foreach (SyntaxTree syntaxTree in Compilation.SyntaxTrees)
            {
                SemanticModel sm = Compilation.GetSemanticModel(syntaxTree);

                foreach (AttributeSyntax node in syntaxTree.GetRoot(Cancellation).DescendantNodes().OfType<AttributeSyntax>())
                {
                    if (Is(sm.GetTypeInfo(node, Cancellation).Type, typeof(TAttribute)))
                        yield return node;
                }
            }
        }

        private static IReadOnlyList<Type> Generators { get; } = typeof(ProxyEmbedder)
            .Assembly
            .GetTypes()
            .Where(t => t.BaseType?.IsGenericType == true && t.BaseType.GetGenericTypeDefinition() == typeof(TypeGenerator<>))
            .ToArray();

        internal IEnumerable<INamedTypeSymbol> GetAOTGenerators() 
        {
            foreach(AttributeSyntax attr in GetAttributes<EmbedGeneratedTypeAttribute>())
            {
                if (attr.ArgumentList!.Arguments.Single().Expression is TypeOfExpressionSyntax expr) // parametere nem NULL?
                {
                    SemanticModel sm = Compilation.GetSemanticModel(attr.SyntaxTree);

                    if (sm.GetTypeInfo(expr.Type, Cancellation).Type is INamedTypeSymbol named)
                    {
                        INamedTypeSymbol genericTypeDefinition = named.OriginalDefinition;

                        if (Generators.Any(generator => Is(genericTypeDefinition, generator)))
                            yield return named;
                    }
                }
            }
        }
    }
}
