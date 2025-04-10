/********************************************************************************
* ClassSyntaxFactoryBase.Common                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ClassSyntaxFactoryBase
    {
        // https://github.com/dotnet/roslyn/issues/4861
        protected const string Value = "value";

        private static T Fail<T>(string message)
        {
            Debug.Fail(message);
            return default!;
        }

        private static AccessorDeclarationSyntax ResolveAccessor(SyntaxKind kind, CSharpSyntaxNode? body, params IEnumerable<SyntaxKind> modifiers)
        {
            AccessorDeclarationSyntax declaration = AccessorDeclaration(kind);

            declaration = body switch
            {
                BlockSyntax block => declaration.WithBody(block),
                ArrowExpressionClauseSyntax arrow => declaration.WithExpressionBody(arrow).WithSemicolonToken
                (
                    Token(SyntaxKind.SemicolonToken)
                ),
                null => declaration.WithSemicolonToken
                (
                    Token(SyntaxKind.SemicolonToken)
                ),
                _ => Fail<AccessorDeclarationSyntax>("Unknown node type")
            };

            if (modifiers.Any()) declaration = declaration.WithModifiers
            (
                modifiers: TokenList
                (
                    modifiers.Select(Token)
                )
            );

            return declaration;
        }

        private IEnumerable<SyntaxKind> ResolveAccessModifiers(IMethodInfo method)
        {
            bool internalAllowed = method.DeclaringType.DeclaringAssembly?.IsFriend(ContainingAssembly) is true;

            IEnumerable<SyntaxKind> ams = method
                .AccessModifiers
                .SetFlags()

                //
                // When overriding an "internal protected" member we cannot reuse the "internal" keyword
                // if the base is declared in a different assembly
                //

                .Where(am => am >= AccessModifiers.Protected && (am is not AccessModifiers.Internal || internalAllowed))
                .Select
                (
                    static am => am switch
                    {
                        AccessModifiers.Public => SyntaxKind.PublicKeyword,
                        AccessModifiers.Protected => SyntaxKind.ProtectedKeyword,
                        AccessModifiers.Internal => SyntaxKind.InternalKeyword,
                        _ => Fail<SyntaxKind>("Member not visible")
                    }
                );
            if (!ams.Any())
                throw new InvalidOperationException(Resources.UNDETERMINED_ACCESS_MODIFIER);

            return ams;
        }

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
