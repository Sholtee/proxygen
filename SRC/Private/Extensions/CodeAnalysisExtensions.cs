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
            List<SyntaxNodeOrToken> nodesAndTokens = new();

            int i = 0;

            foreach (T item in src)
            {
                if (nodesAndTokens.Count > 0)
                    nodesAndTokens.Add
                    (
                        Token(SyntaxKind.CommaToken)
                    );

                nodesAndTokens.Add
                (
                    factory(item, i++)
                );
            }

            return SeparatedList<TNode>(nodesAndTokens);
        }

        /// <summary>
        /// SyntaxNode1, SyntaxNode2, ....
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<T, TNode>(this IEnumerable<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => ToSyntaxList(src, (p, i) => factory(p));

        /// <summary>
        /// SyntaxNode1, SyntaxNode2, ....
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<TNode>(this IEnumerable<TNode> src) where TNode : SyntaxNode => ToSyntaxList(src, static x => x);

        /// <summary>
        /// Name1.Name2.Name3.....
        /// </summary>
        public static NameSyntax Qualify(this IEnumerable<NameSyntax> parts)
        {
            int count = parts.Count();
            return count switch
            {
                0 => throw new InvalidOperationException(),
                1 => parts.Single(),
                _ => QualifiedName
                (
                    left: Qualify(parts.Take(count - 1)),
                    right: (SimpleNameSyntax) parts.Last()
                )
            };
        }

        public static LiteralExpressionSyntax AsLiteral(this string param) => LiteralExpression
        (
            SyntaxKind.StringLiteralExpression,
            Literal(param)
        );

        public static LiteralExpressionSyntax AsLiteral(this int param) => LiteralExpression
        (
            SyntaxKind.NumericLiteralExpression,
            Literal(param)
        );
    }
}
