/********************************************************************************
* ClassSyntaxFactoryBase.Constructor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// TypeName(int a, string b, ...): base(a, b, ...){ }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ConstructorDeclarationSyntax ResolveConstructor(IConstructorInfo ctor, string className)
        {
            IReadOnlyList<IParameterInfo> paramz = ctor.Parameters;

            return ConstructorDeclaration
            (
                identifier: Identifier(className)
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
                            param => Argument
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
        protected abstract IEnumerable<MemberDeclarationSyntax> ResolveConstructors(object context);

        #if DEBUG
        internal
        #endif
        protected abstract IEnumerable<MemberDeclarationSyntax> ResolveConstructor(object context, IConstructorInfo ctor);
    }
}
