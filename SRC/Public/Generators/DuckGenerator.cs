/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating a proxy that wraps the <see cref="Target"/> to implement the <see cref="Interface"/>.
    /// </summary>
    public sealed class DuckGenerator(Type iface, Type target) : Generator(id: GenerateId(nameof(DuckGenerator), iface, target))
    {
        /// <summary>
        /// The target implementing all the <see cref="Interface"/> members.
        /// </summary>
        public Type Target { get; } = target ?? throw new ArgumentNullException(nameof(target));

        /// <summary>
        /// The interface for which the proxy will be created.
        /// </summary>
        public Type Interface { get; } = iface ?? throw new ArgumentNullException(nameof(iface));

        /// <summary>
        /// Activates the underlying duck type.
        /// </summary>
        #if NETSTANDARD2_0
        new
        #endif
        public async Task<object> ActivateAsync(object target, CancellationToken cancellation = default)
        {
            ITargetAccess targetAccess = (ITargetAccess) await base.ActivateAsync(null, cancellation);
            targetAccess.Target = target;
            return targetAccess;
        }

        /// <summary>
        /// Activates the underlying duck type.
        /// </summary>
        public object Activate(object target) => ActivateAsync(target, CancellationToken.None).GetAwaiter().GetResult();

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(SyntaxFactoryContext context) => new DuckSyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Interface),
            MetadataTypeInfo.CreateFrom(Target),
            context
        );
    }
}