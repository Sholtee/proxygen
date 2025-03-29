/********************************************************************************
* ClassSyntaxFactoryBase.Field.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// <code>
        /// [private|public] [static] [readonly] System.Object paramName [= ...];
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected FieldDeclarationSyntax ResolveField(ITypeInfo type, string name, ExpressionSyntax? initializer = null, bool @private = true, bool @static = true, bool @readonly = true)
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

            List<SyntaxKind> modifiers = [@private ? SyntaxKind.PrivateKeyword : SyntaxKind.PublicKeyword];           
            if (@static)
                modifiers.Add(SyntaxKind.StaticKeyword);
            if (@readonly)
                modifiers.Add(SyntaxKind.ReadOnlyKeyword);

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
                    modifiers.Select(Token)
                )
            );
        }

        /// <summary>
        /// <code>
        /// [private|public] static readonly System.Object paramName [= ...];
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected FieldDeclarationSyntax ResolveField<T>(string name, ExpressionSyntax? initializer = null, bool @private = true, bool @static = true, bool @readonly = true) => ResolveField
        (
            MetadataTypeInfo.CreateFrom(typeof(T)),
            name,
            initializer,
            @private,
            @static,
            @readonly
        );
    }
}
