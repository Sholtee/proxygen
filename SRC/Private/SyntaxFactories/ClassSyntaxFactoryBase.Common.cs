/********************************************************************************
* ClassSyntaxFactoryBase.Common                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        // https://github.com/dotnet/roslyn/issues/4861
        protected const string Value = "value";

        private AccessorDeclarationSyntax ResolveAccessor(SyntaxKind kind, CSharpSyntaxNode? body, params IEnumerable<SyntaxKind> modifiers)
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
                case null:
                    declaration = declaration.WithSemicolonToken
                    (
                        Token(SyntaxKind.SemicolonToken)
                    );
                    break;
                default:
                    Debug.Fail("Unknown node type");
                    return null!;
            }

            if (modifiers.Any()) declaration = declaration.WithModifiers
            (
                modifiers: TokenList
                (
                    modifiers.Select(Token)
                )
            );

            return declaration;
        }

        private static IEnumerable<SyntaxKind> AccessModifiersToSyntaxList(AccessModifiers am) => am.SetFlags().Select
        (
            static am =>
            {
                switch (am)
                {
                    case AccessModifiers.Public: return SyntaxKind.PublicKeyword;
                    case AccessModifiers.Protected: return SyntaxKind.ProtectedKeyword;
                    case AccessModifiers.Internal: return SyntaxKind.InternalKeyword;
                    default:
                        Debug.Fail("Member not visible");
                        return SyntaxKind.None;
                }
            }
        );

        /// <summary>
        /// <code>
        /// new System.Object[] {..., ..., ...}
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ArrayCreationExpressionSyntax ResolveArray(ITypeInfo elementType, IEnumerable<ExpressionSyntax> elements) => ArrayCreationExpression
        (
            type: ArrayType
            (
                elementType: ResolveType(elementType)
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    ArrayRankSpecifier
                    (
                        SingletonSeparatedList
                        (
                            elements.Any() ? OmittedArraySizeExpression() : (ExpressionSyntax) 0.AsLiteral()
                        )
                    )
                )
            ),
            initializer: !elements.Any() ? null : InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: elements.ToSyntaxList()
            )
        );

        /// <summary>
        /// <code>
        /// new System.Object[] {..., ..., ...}
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ArrayCreationExpressionSyntax ResolveArray<T>(IEnumerable<ExpressionSyntax> elements) => ResolveArray(MetadataTypeInfo.CreateFrom(typeof(T)), elements);

        /// <summary>
        /// <code>
        /// new NameSpace.T(.., ...,)
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ObjectCreationExpressionSyntax ResolveObject<T>(params IEnumerable<ArgumentSyntax> arguments) => ObjectCreationExpression(type: ResolveType<T>()).WithArgumentList
        (
            ArgumentList
            (
                arguments.ToSyntaxList()
            )
        );

        #if DEBUG
        internal
        #endif
        protected static SimpleNameSyntax ResolveIdentifierName(LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single().Identifier);

        #if DEBUG
        internal
        #endif
        protected static ArgumentSyntax ResolveArgument(LocalDeclarationStatementSyntax variable) => Argument
        (
            ResolveIdentifierName(variable)
        );

        #if DEBUG
        internal
        #endif
        protected static SimpleNameSyntax ResolveIdentifierName(FieldDeclarationSyntax field) => IdentifierName(field.Declaration.Variables.Single()!.Identifier);

        #if DEBUG
        internal
        #endif
        protected static SimpleNameSyntax ResolveIdentifierName(ClassDeclarationSyntax cls) => cls.TypeParameterList is null
            ? IdentifierName(cls.Identifier)
            : GenericName
            (
                cls.Identifier,
                TypeArgumentList
                (
                    cls.TypeParameterList.Parameters.ToSyntaxList<TypeParameterSyntax, TypeSyntax>
                    (
                        static ga => IdentifierName(ga.Identifier)
                    )
                )
            );
    }
}
