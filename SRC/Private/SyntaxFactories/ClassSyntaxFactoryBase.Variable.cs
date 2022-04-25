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
        /// System.Object paramName [= ...];
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
        /// System.Object paramName [= ...];
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected LocalDeclarationStatementSyntax ResolveLocal<T>(string name, ExpressionSyntax? initializer = null) => ResolveLocal(MetadataTypeInfo.CreateFrom(typeof(T)), name, initializer);

        #if DEBUG
        internal
        #endif
        protected static IdentifierNameSyntax ToIdentifierName(LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single()!.Identifier);

        #if DEBUG
        internal
        #endif
        protected static ArgumentSyntax ToArgument(LocalDeclarationStatementSyntax variable) => Argument
        (
            ToIdentifierName(variable)
        );

        #if DEBUG
        internal
        #endif
        protected static IdentifierNameSyntax ToIdentifierName(FieldDeclarationSyntax field) => IdentifierName(field.Declaration.Variables.Single()!.Identifier);

        #if DEBUG
        internal
        #endif
        protected static ArgumentSyntax ToArgument(FieldDeclarationSyntax field) => Argument
        (
            ToIdentifierName(field)
        );

        #if DEBUG
        internal
        #endif
        protected static NameSyntax ToIdentifierName(ClassDeclarationSyntax cls) => cls.TypeParameterList is null //TODO: move to ClassSyntaxFactoryBase.Identifier.cs
            ? IdentifierName(cls.Identifier)
            : GenericName
            (
                cls.Identifier,
                TypeArgumentList
                (
                    cls.TypeParameterList.Parameters.ToSyntaxList(ga => (TypeSyntax) IdentifierName(ga.Identifier))
                )
            );
    }
}
