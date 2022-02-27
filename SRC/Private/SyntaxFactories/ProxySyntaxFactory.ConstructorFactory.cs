/********************************************************************************
* ProxySyntaxFactory.ConstructorFactory.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ConstructorDeclarationSyntax> ResolveConstructors(object context)
        {
            foreach (IConstructorInfo ctor in InterceptorType.GetPublicConstructors())
            {
                yield return ResolveConstructor(null!, ctor);
            }
        }

        #if DEBUG
        internal
        #endif
        protected override ConstructorDeclarationSyntax ResolveConstructor(object context, IConstructorInfo ctor) =>
            DeclareCtor
            (
                ctor,
                ResolveClassName(null!)
            )
            .WithBody
            (
                Block
                (
                    //
                    // Proxy = this;
                    //

                    ExpressionStatement
                    (
                        AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: PropertyAccess
                            (
                                Proxy,
                                target: null,
                                castTargetTo: null
                            ),
                            right: ThisExpression()
                        )
                    )
                )
            );
    }
}