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
            public IDuckContext Context { get; }

            public ConstructorFactory(IDuckContext context) : base(context.InterfaceType) =>
                Context = context;

            public override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                Members = Context
                    .BaseType
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