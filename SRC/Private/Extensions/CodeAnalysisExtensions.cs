/********************************************************************************
* CodeAnalysisExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal static class CodeAnalysisExtensions
    {
        /// <summary>
        /// SyntaxNode1, SyntaxNode2, ....
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<T, TNode>(this IEnumerable<T> src, Func<T, int, TNode> factory) where TNode : SyntaxNode
        {
            //
            // Ha "src" implementalja ICollection<T>-t akkor nem lesz felsorolas
            //

            int count = src.Count();

            return SeparatedList<TNode>
            (
                nodesAndTokens: src.SelectMany(Node)
            );

            IEnumerable<SyntaxNodeOrToken> Node(T p, int i) 
            {
                yield return factory(p, i);
                if (i < count - 1) yield return Token(SyntaxKind.CommaToken);
            }
        }

        /// <summary>
        /// SyntaxNode1, SyntaxNode2, ....
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<T, TNode>(this IEnumerable<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => ToSyntaxList(src, (p, i) => factory(p));

        /// <summary>
        /// SyntaxNode1, SyntaxNode2, ....
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<TNode>(this IEnumerable<TNode> src) where TNode : SyntaxNode => ToSyntaxList(src, x => x);

        /// <summary>
        /// Name1.Name2.Name3.....
        /// </summary>
        public static NameSyntax Qualify(this IEnumerable<NameSyntax> parts) => parts.Count() <= 1 ? parts.Single() : QualifiedName
        (
#if NETSTANDARD2_1_OR_GREATER
            left: Qualify(parts.SkipLast(1)),         
#else
            left: Qualify(parts.Take(parts.Count() - 1)),
#endif
            right: (SimpleNameSyntax) parts.Last()
        );
    }
}
