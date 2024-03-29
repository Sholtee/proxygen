﻿/********************************************************************************
* CodeAnalysisExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

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
            List<NameSyntax> coll = parts as List<NameSyntax> ?? new List<NameSyntax>(parts);

            if (coll.Count is 0)
                throw new InvalidOperationException();

            if (coll.Count is 1)
                return coll[0];

            return QualifiedName
            (
                left: Qualify(coll.GetRange(0, coll.Count - 1)),
                right: (SimpleNameSyntax) coll[coll.Count - 1]
            );
        }

        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
        public static string GetMD5HashCode(this SyntaxNode node)
        {
            using MD5 md5 = MD5.Create();
            
            byte[] hash = md5.ComputeHash
            (
                Encoding.UTF8.GetBytes(node.ToFullString())
            );

            StringBuilder sb = new();

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append
                (
                    md5.Hash[i].ToString("X2", null)
                );
            }

            return sb.ToString();
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
