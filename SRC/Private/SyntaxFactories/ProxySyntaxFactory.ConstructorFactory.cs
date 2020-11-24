/********************************************************************************
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
            public ProxySyntaxFactory Owner { get; }

            public ConstructorFactory(ProxySyntaxFactory owner) : base(owner.InterceptorType) => Owner = owner;

            public override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                cancellation.ThrowIfCancellationRequested();

                Members = Owner
                    .InterceptorType
                    .Constructors
                    .Select(ctor => DeclareCtor(ctor, Owner.Classes.Single()))
                    .ToArray();

                return true;
            }
        }
    }
}