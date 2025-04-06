/********************************************************************************
* InterfaceProxyGenerator.cs                                                    *
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
    /// Creates a new <see cref="InterfaceProxyGenerator"/> instance which hooks into the given <paramref name="interface"/>.
    /// </summary>
    /// <param name="interface">The interface to be proxied</param>
    /// <param name="interceptor">The interceptor implementation. Should be an <see cref="InterfaceInterceptor{TInterface, TTarget}"/> descendant</param>
    public sealed class InterfaceProxyGenerator(Type @interface, Type interceptor) : Generator(id: GenerateId(nameof(InterfaceProxyGenerator), @interface, interceptor))
    {
        /// <summary>
        /// The target class or interface for which the proxy will be created.
        /// </summary>
        public Type Interface { get; } = @interface ?? throw new ArgumentNullException(nameof(@interface));

        /// <summary>
        /// An <see cref="InterfaceInterceptor{TInterface}"/> descendant having at least one public constructor.
        /// </summary>
        public Type Interceptor { get; } = interceptor ?? throw new ArgumentNullException(nameof(interceptor));

        /// <summary>
        /// Activates the proxy type.
        /// </summary>
        public new Task<object> ActivateAsync(Tuple ctorParamz, CancellationToken cancellation = default) =>
            base.ActivateAsync(ctorParamz, cancellation);

        /// <summary>
        /// Activates the underlying duck type.
        /// </summary>
        public object Activate(Tuple ctorParamz) => ActivateAsync(ctorParamz, CancellationToken.None).GetAwaiter().GetResult();

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(string? asmName, ReferenceCollector referenceCollector) => new InterfaceProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Interface),
            MetadataTypeInfo.CreateFrom(Interceptor),
            asmName,
            OutputType.Module,
            referenceCollector
        );
    }
}