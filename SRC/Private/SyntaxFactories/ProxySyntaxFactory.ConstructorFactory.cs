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
        protected override IEnumerable<MemberDeclarationSyntax> ResolveConstructors(object context)
        {
            foreach (IConstructorInfo ctor in InterceptorType.GetPublicConstructors())
            {
                foreach (MemberDeclarationSyntax member in ResolveConstructor(null!, ctor))
                {
                    yield return member;
                }
            }
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveConstructor(object context, IConstructorInfo ctor)
        {
            yield return ResolveConstructor
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
                                InterceptorType.Properties.Single
                                (
                                    prop => prop.Name == nameof(InterfaceInterceptor<object>.Proxy)
                                )!,
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
}