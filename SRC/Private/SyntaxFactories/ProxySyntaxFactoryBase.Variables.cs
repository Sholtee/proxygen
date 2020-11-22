/********************************************************************************
* ProxySyntaxFactoryBase.Variables.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactoryBase
    {
        /// <summary>
        /// System.Object paramName [= ...];
        /// </summary>
        protected internal virtual LocalDeclarationStatementSyntax DeclareLocal(ITypeInfo type, string name, ExpressionSyntax? initializer = null)
        {
            VariableDeclaratorSyntax declarator = VariableDeclarator
            (
                identifier: Identifier(name)
            );

            if (initializer != null) declarator = declarator.WithInitializer
            (
                initializer: EqualsValueClause(initializer)
            );

            return LocalDeclarationStatement
            (
                declaration: VariableDeclaration
                (
                    type: CreateType(type),
                    variables: SeparatedList(new List<VariableDeclaratorSyntax>
                    {
                        declarator
                    })
                )
            );
        }

        /// <summary>
        /// System.Object paramName [= ...];
        /// </summary>
        protected internal LocalDeclarationStatementSyntax DeclareLocal<T>(string name, ExpressionSyntax? initializer = null) => DeclareLocal(MetadataTypeInfo.CreateFrom(typeof(T)), name, initializer);

        protected internal static IdentifierNameSyntax ToIdentifierName(LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single().Identifier);

        protected internal static ArgumentSyntax ToArgument(LocalDeclarationStatementSyntax variable) => Argument(ToIdentifierName(variable));
    }
}
