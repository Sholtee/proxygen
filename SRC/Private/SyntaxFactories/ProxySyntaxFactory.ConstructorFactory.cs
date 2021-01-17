﻿/********************************************************************************
* ProxySyntaxFactory.ConstructorFactory.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        internal sealed class ConstructorFactory : MemberSyntaxFactory
        {
            public IProxyContext Context { get; }

            public ConstructorFactory(IProxyContext context) : base(context.InterceptorType) => Context = context;

            public override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                Members = Context
                    .InterceptorType
                    .GetPublicConstructors()
                    .Select(ctor => 
                    {
                        cancellation.ThrowIfCancellationRequested();

                        return DeclareCtor(ctor, Context.ClassName);
                    })
                    .ToArray();

                return true;
            }
        }
    }
}