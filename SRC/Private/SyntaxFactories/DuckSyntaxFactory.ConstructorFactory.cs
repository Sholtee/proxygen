/********************************************************************************
* DuckSyntaxFactory.ConstructorFactory.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        internal sealed class ConstructorFactory : MemberSyntaxFactory
        {
            public DuckSyntaxFactory Owner { get; }

            public ITypeInfo Base { get; }

            public ConstructorFactory(DuckSyntaxFactory owner) : base(owner.InterfaceType)
            {
                Owner = owner;
                Base = (ITypeInfo) ((IGenericTypeInfo)MetadataTypeInfo.CreateFrom(typeof(DuckBase<>))).Close(owner.TargetType);
            }

            public override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                cancellation.ThrowIfCancellationRequested();

                Members = Base
                    .Constructors
                    .Select(ctor => DeclareCtor(ctor, Owner.Classes.Single()))
                    .ToArray();

                return true;
            }
        }
    }
}