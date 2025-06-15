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
    /// <summary>
    /// Helper methods for various syntax related types.
    /// </summary>
    internal static class CodeAnalysisExtensions
    {
        /// <summary>
        /// Stringifies the given <see cref="SyntaxNode"/>.
        /// </summary>
        public static string Stringify(this SyntaxNode src, string? lineEnding = null) => src
            .NormalizeWhitespace(eol: lineEnding ?? Environment.NewLine)
            .ToFullString();

        /// <summary>
        /// Creates a coma separated list from the given nodes.
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<T, TNode>(this IEnumerable<T> src, Func<T, int, TNode> factory) where TNode : SyntaxNode
        {
            List<SyntaxNodeOrToken> nodesAndTokens = [];

            int i = 0;

            foreach (T item in src)
            {
                if (i > 0) nodesAndTokens.Add
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
        /// Creates a coma separated list from the given nodes.
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<T, TNode>(this IEnumerable<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => ToSyntaxList(src, (p, i) => factory(p));

        /// <summary>
        /// Creates a coma separated list from the given nodes.
        /// </summary>
        public static SeparatedSyntaxList<TNode> ToSyntaxList<TNode>(this IEnumerable<TNode> src) where TNode : SyntaxNode => ToSyntaxList(src, static x => x);

        /// <summary>
        /// Creates a qualified (dot separated) name from the given <see cref="NameSyntax"/> list.
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

        /// <summary>
        /// Converts the given value to <see cref="LiteralExpressionSyntax"/>.
        /// </summary>
        public static LiteralExpressionSyntax AsLiteral(this string param) => LiteralExpression
        (
            SyntaxKind.StringLiteralExpression,
            Literal(param)
        );

        /// <summary>
        /// Converts the given value to <see cref="LiteralExpressionSyntax"/>.
        /// </summary>
        public static LiteralExpressionSyntax AsLiteral(this int param) => LiteralExpression
        (
            SyntaxKind.NumericLiteralExpression,
            Literal(param)
        );
    }
}
