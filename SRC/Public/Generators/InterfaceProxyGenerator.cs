/********************************************************************************
* InterfaceProxyGenerator.cs                                                    *
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
    /// Creates a new <see cref="InterfaceProxyGenerator"/> instance which hooks into the given <paramref name="interface"/>.
    /// </summary>
    /// <param name="interface">The interface to be proxied</param>
    public sealed class InterfaceProxyGenerator(Type @interface) : Generator(id: GenerateId(nameof(InterfaceProxyGenerator), @interface))
    {
        /// <summary>
        /// The target class or interface for which the proxy will be created.
        /// </summary>
        public Type Interface { get; } = @interface ?? throw new ArgumentNullException(nameof(@interface));

        /// <summary>
        /// Activates the proxy type.
        /// </summary>
        public async Task<object> ActivateAsync(IInterceptor interceptor, object? target = null, CancellationToken cancellation = default)
        {
            object result = await ActivateAsync(Tuple.Create(target), cancellation);
            ((IInterceptorAccess) result).Interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
            return result;
        }

        /// <summary>
        /// Activates the underlying duck type.
        /// </summary>
        public object Activate(IInterceptor interceptor, object? target = null) => ActivateAsync(interceptor, target, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(SyntaxFactoryContext context) => new InterfaceProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Interface),
            context
        );
    }
}