/********************************************************************************
* MemberSyntaxFactory.Common                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class MemberSyntaxFactory
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
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
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

        protected internal string GetSafeTypeName(ITypeInfo type) => CreateType(type)
            .ToFullString()
            //
            // Csak karaktert es ne karakterlancot csereljunk h az eredmenyt ne befolyasolja a
            // felhasznalo teruleti beallitasa.
            //
            .Replace(',', '_');

        protected internal string GetSafeTypeName<T>() => GetSafeTypeName(MetadataTypeInfo.CreateFrom(typeof(T)));

        /// <summary>
        /// new System.Object[] {..., ..., ...}
        /// </summary>
        protected internal ArrayCreationExpressionSyntax CreateArray(ITypeInfo elementType, params ExpressionSyntax[] elements) => ArrayCreationExpression
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
                        elements.Any() ? OmittedArraySizeExpression() : (ExpressionSyntax)LiteralExpression
                        (
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)
                        )
                    ))
                )
            ),
            initializer: !elements.Any() ? null : InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: elements.ToSyntaxList()
            )
        );

        /// <summary>
        /// new System.Object[] {..., ..., ...}
        /// </summary>
        protected internal ArrayCreationExpressionSyntax CreateArray<T>(params ExpressionSyntax[] elements) => CreateArray(MetadataTypeInfo.CreateFrom(typeof(T)), elements);

        /// <summary>
        /// new NameSpace.T(.., ...,)
        /// </summary>
        protected internal ObjectCreationExpressionSyntax CreateObject<T>(params ArgumentSyntax[] arguments) => ObjectCreationExpression(type: CreateType<T>()).WithArgumentList
        (
            ArgumentList
            (
                arguments.ToSyntaxList()
            )
        );
    }
}
