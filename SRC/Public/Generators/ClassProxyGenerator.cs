/********************************************************************************
* ClassProxyGenerator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Tuple =
    #if NETSTANDARD2_1_OR_GREATER
    System.Runtime.CompilerServices.ITuple;
    #else
    object;
    #endif

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that intercept class method calls.
    /// </summary>
    public sealed class ClassProxyGenerator(Type @class) : Generator(id: GenerateId(nameof(ClassProxyGenerator), @class))
    {
        /// <summary>
        /// The target class
        /// </summary>
        public Type Class { get; } = @class ?? throw new ArgumentNullException(nameof(@class));

        /// <summary>
        /// Activates the proxy type.
        /// </summary>
        public Task<object> ActivateAsync(IInterceptor interceptor, Tuple? ctorParamz, CancellationToken cancellation = default) =>
            ActivateAsync(System.Tuple.Create(interceptor, (object?) ctorParamz), cancellation);

        /// <summary>
        /// Activates the underlying proxy type.
        /// </summary>
        public object Activate(IInterceptor interceptor, Tuple? ctorParamz) => ActivateAsync(interceptor, ctorParamz, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(SyntaxFactoryContext context) => new ClassProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Class),
            context
        );
    }
}