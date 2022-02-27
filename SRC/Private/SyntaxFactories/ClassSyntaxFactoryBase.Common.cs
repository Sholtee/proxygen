/********************************************************************************
* ClassSyntaxFactoryBase.Common                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        // https://github.com/dotnet/roslyn/issues/4861
        protected const string Value = "value";

        private AccessorDeclarationSyntax DeclareAccessor(SyntaxKind kind, CSharpSyntaxNode body, bool forceInlining)
        {
            AccessorDeclarationSyntax declaration = AccessorDeclaration(kind);

            switch (body)
            {
                case BlockSyntax block:
                    declaration = declaration.WithBody(block);
                    break;
                case ArrowExpressionClauseSyntax arrow:
                    declaration = declaration
                        .WithExpressionBody(arrow)
                        .WithSemicolonToken
                        (
                            Token(SyntaxKind.SemicolonToken)
                        );
                    break;
                default:
                    Debug.Fail("Unknown node type");
                    return null!;
            }

            if (forceInlining) declaration = declaration.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            return declaration;
        }

        /// <summary>
        /// new System.Object[] {..., ..., ...}
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ArrayCreationExpressionSyntax CreateArray(ITypeInfo elementType, params ExpressionSyntax[] elements) => ArrayCreationExpression
        (
            type: ArrayType
            (
                elementType: CreateType(elementType)
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    ArrayRankSpecifier(SingletonSeparatedList
                    (
                        elements.Some() ? OmittedArraySizeExpression() : (ExpressionSyntax)LiteralExpression
                        (
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)
                        )
                    ))
                )
            ),
            initializer: !elements.Some() ? null : InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: elements.ToSyntaxList()
            )
        );

        /// <summary>
        /// new System.Object[] {..., ..., ...}
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ArrayCreationExpressionSyntax CreateArray<T>(params ExpressionSyntax[] elements) => CreateArray(MetadataTypeInfo.CreateFrom(typeof(T)), elements);

        /// <summary>
        /// new NameSpace.T(.., ...,)
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ObjectCreationExpressionSyntax CreateObject<T>(params ArgumentSyntax[] arguments) => ObjectCreationExpression(type: CreateType<T>()).WithArgumentList
        (
            ArgumentList
            (
                arguments.ToSyntaxList()
            )
        );

        /// <summary>
        /// Enum.Member
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected MemberAccessExpressionSyntax EnumAccess<T>(T val) where T : Enum =>
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, CreateType<T>(), IdentifierName(val.ToString()));
    }
}
