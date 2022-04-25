/********************************************************************************
* ClassSyntaxFactoryBase.Field.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// [private|public] static readonly System.Object paramName [= ...];
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected FieldDeclarationSyntax ResolveStaticGlobal(ITypeInfo type, string name, ExpressionSyntax? initializer = null, bool @private = true)
        {
            VariableDeclaratorSyntax declarator = VariableDeclarator
            (
                identifier: Identifier(name)
            );

            if (initializer is not null) declarator = declarator.WithInitializer
            (
                initializer: EqualsValueClause
                (
                    initializer
                )
            );

            return FieldDeclaration
            (
                declaration: VariableDeclaration
                (
                    type: ResolveType(type),
                    variables: SingletonSeparatedList(declarator)
                )
            )
            .WithModifiers
            (
                TokenList
                (
                    new SyntaxToken[]
                    {
                        Token(@private ? SyntaxKind.PrivateKeyword : SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.ReadOnlyKeyword)
                    }
                )
            );
        }

        /// <summary>
        /// [private|public] static readonly System.Object paramName [= ...];
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected FieldDeclarationSyntax ResolveStaticGlobal<T>(string name, ExpressionSyntax? initializer = null, bool @private = true) => ResolveStaticGlobal
        (
            MetadataTypeInfo.CreateFrom(typeof(T)),
            name,
            initializer,
            @private
        );
    }
}
