/********************************************************************************
* DelegateProxyGenerator.cs                                                     *
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
    /// Type generator for creating proxies that intercept delegate invocations.
    /// </summary>
    public sealed class DelegateProxyGenerator(Type delegateType) : Generator(id: GenerateId(nameof(DelegateProxyGenerator), delegateType))
    {
        /// <summary>
        /// The delegate type to be used.
        /// </summary>
        public Type DelegateType { get; } = delegateType ?? throw new ArgumentNullException(nameof(delegateType));

        /// <summary>
        /// Activates the proxy type.
        /// </summary>
        public async Task<object> ActivateAsync(IInterceptor interceptor, Delegate? @delegate, CancellationToken cancellation = default)
        {
            object result = await ActivateAsync(null, cancellation);

            ((IInterceptorAccess) result).Interceptor = interceptor;
            ((ITargetAccess) result).Target = @delegate;

            return ((IDelegateWrapper) result).Wrapped;
        }

        /// <summary>
        /// Activates the underlying proxy type.
        /// </summary>
        public object Activate(IInterceptor interceptor, Delegate? @delegate) => ActivateAsync(interceptor, @delegate, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(SyntaxFactoryContext context) => new DelegateProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(DelegateType),
            context
        );
    }
}