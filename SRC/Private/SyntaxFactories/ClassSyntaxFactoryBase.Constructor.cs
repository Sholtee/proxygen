/********************************************************************************
* ClassSyntaxFactoryBase.Constructor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

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
        /// TypeName(T a, TT b, ...): base(a, b, ...) { }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ConstructorDeclarationSyntax ResolveConstructor(IConstructorInfo ctor, SyntaxToken name)
        {
            IReadOnlyList<IParameterInfo> paramz = ctor.Parameters;

            return ConstructorDeclaration
            (
                name
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    Token(SyntaxKind.PublicKeyword)
                )
            )
            .WithParameterList
            (
                parameterList: ParameterList
                (
                    paramz.ToSyntaxList
                    (
                        param => Parameter
                        (
                            identifier: Identifier(param.Name)
                        )
                        .WithType
                        (
                            type: ResolveType(param.Type)
                        )
                    )
                )
            )
            .WithInitializer
            (
                initializer: ConstructorInitializer
                (
                    SyntaxKind.BaseConstructorInitializer,
                    ArgumentList
                    (
                        paramz.ToSyntaxList
                        (
                            static param => Argument
                            (
                                expression: IdentifierName(param.Name)
                            )
                        )
                    )
                )
            )
            .WithBody(Block());
        }

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => cls;
    }
}
