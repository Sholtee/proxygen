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
    using Attributes;
    using Generators;

    internal class ProxyEmbedder
    {
        private static bool Is(Compilation compilation, ISymbol? s, Type t) => SymbolEqualityComparer.Default.Equals(s, compilation.GetTypeByMetadataName(t.FullName));

        public static IEnumerable<AttributeSyntax> GetAttributes<TAttribute>(CSharpCompilation compilation, CancellationToken cancellation = default) where TAttribute : Attribute
        {
            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel sm = compilation.GetSemanticModel(syntaxTree);

                foreach (AttributeSyntax node in syntaxTree.GetRoot(cancellation).DescendantNodes().OfType<AttributeSyntax>())
                {
                    if (Is(compilation, sm.GetTypeInfo(node, cancellation).Type, typeof(TAttribute)))
                        yield return node;
                }
            }
        }

        public static IEnumerable<(Type Generator, IReadOnlyCollection<ITypeSymbol> GenericArguments)> GetAOTGenerators(CSharpCompilation compilation, CancellationToken cancellation = default) 
        {
            foreach(AttributeSyntax attr in GetAttributes<EmbedGeneratedTypeAttribute>(compilation, cancellation))
            {
                if (attr.ArgumentList!.Arguments.Single().Expression is TypeOfExpressionSyntax expr) // parametere nem NULL?
                {
                    SemanticModel sm = compilation.GetSemanticModel(attr.SyntaxTree);

                    if (sm.GetTypeInfo(expr.Type, cancellation).Type is INamedTypeSymbol named)
                    {
                        INamedTypeSymbol genericTypeDefinition = named.OriginalDefinition;

                        Type? generator = genericTypeDefinition switch
                        {
                            _ when Is(compilation, genericTypeDefinition, typeof(ProxyGenerator<,>)) => typeof(ProxyGenerator<,>),
                            _ when Is(compilation, genericTypeDefinition, typeof(DuckGenerator<,>)) => typeof(DuckGenerator<,>),
                            _ => null
                        };

                        if (generator is not null) yield return (generator, named.TypeArguments);
                    }
                }
            }
        }
    }
}
