/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Generators
{
    using Abstractions;
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that intercept interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to which the proxy will be created.</typeparam>
    /// <typeparam name="TInterceptor">An <see cref="InterfaceInterceptor{TInterface}"/> descendant that has at least one public constructor.</typeparam>
    public sealed class ProxyGenerator<TInterface, TInterceptor> : TypeGenerator<ProxyGenerator<TInterface, TInterceptor>> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        /// <summary>
        /// Creates a new <see cref="ProxyGenerator{TInterface, TInterceptor}"/> instance.
        /// </summary>
        public ProxyGenerator() => SyntaxFactory = new ProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(typeof(TInterface)),
            MetadataTypeInfo.CreateFrom(typeof(TInterceptor)),
            TypeResolutionStrategy.AssemblyName,
            TypeResolutionStrategy.Type,
            MetadataTypeInfo.CreateFrom(GetType())
        );

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public override IUnitSyntaxFactory SyntaxFactory { get; }
    }
}