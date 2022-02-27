﻿/********************************************************************************
* ClassSyntaxFactoryBase.Variable.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// System.Object paramName [= ...];
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected LocalDeclarationStatementSyntax DeclareLocal(ITypeInfo type, string name, ExpressionSyntax? initializer = null)
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

            return LocalDeclarationStatement
            (
                declaration: VariableDeclaration
                (
                    type: CreateType(type),
                    variables: SingletonSeparatedList(declarator)
                )
            );
        }

        /// <summary>
        /// System.Object paramName [= ...];
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected LocalDeclarationStatementSyntax DeclareLocal<T>(string name, ExpressionSyntax? initializer = null) => DeclareLocal(MetadataTypeInfo.CreateFrom(typeof(T)), name, initializer);

        #if DEBUG
        internal
        #endif
        protected static IdentifierNameSyntax ToIdentifierName(LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single()!.Identifier);

        #if DEBUG
        internal
        #endif
        protected static ArgumentSyntax ToArgument(LocalDeclarationStatementSyntax variable) => Argument(ToIdentifierName(variable));
    }
}
