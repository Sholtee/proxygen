/********************************************************************************
* ProxySyntaxFactory.ConstructorFactory.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        internal sealed class ConstructorFactory : MemberSyntaxFactory
        {
            public IProxyContext Context { get; }

            public IPropertyInfo Proxy { get; }

            public ConstructorFactory(IProxyContext context) : base(context.InterceptorType)
            {
                Context = context;

                Proxy = Context.InterceptorType.Properties.Single
                (
                    prop => prop.Name == nameof(InterfaceInterceptor<object>.Proxy)
                )!;
            }

            public override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                Members = Context
                    .InterceptorType
                    .GetPublicConstructors()
                    .Select(ctor => 
                    {
                        cancellation.ThrowIfCancellationRequested();

                        return DeclareCtor(ctor, Context.ClassName).WithBody
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
                    })
                    .ToArray();

                return true;
            }
        }
    }
}