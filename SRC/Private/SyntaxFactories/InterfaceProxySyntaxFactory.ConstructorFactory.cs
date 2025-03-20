/********************************************************************************
* InterfaceProxySyntaxFactory.ConstructorFactory.cs                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class InterfaceProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context)
        {
            foreach (IConstructorInfo ctor in InterceptorType.GetPublicConstructors())
            {
                cls = ResolveConstructor(cls, context, ctor);
            }
            return cls;
        }

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => cls.AddMembers
        (
            ResolveConstructor
            (
                ctor,
                cls.Identifier
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
                                    static prop => prop.Name == nameof(InterfaceInterceptor<object>.Proxy)
                                ),
                                target: null,
                                castTargetTo: null
                            ),
                            right: ThisExpression()
                        )
                    )
                )
            )
        );
    }
}