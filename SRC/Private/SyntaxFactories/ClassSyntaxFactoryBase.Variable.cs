/********************************************************************************
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
        /// <code>
        /// System.Object paramName [= ...];
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected LocalDeclarationStatementSyntax ResolveLocal(ITypeInfo type, string name, ExpressionSyntax? initializer = null)
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
                    type: ResolveType(type),
                    variables: SingletonSeparatedList(declarator)
                )
            );
        }

        /// <summary>
        /// <code>
        /// System.Object paramName [= ...];
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected LocalDeclarationStatementSyntax ResolveLocal<T>(string name, ExpressionSyntax? initializer = null) => ResolveLocal(MetadataTypeInfo.CreateFrom(typeof(T)), name, initializer);
    }
}
