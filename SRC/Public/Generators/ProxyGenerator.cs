/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that intercept interface method calls.
    /// </summary>
    public sealed class ProxyGenerator : Generator
    {
        /// <summary>
        /// The target class or interface for which the proxy will be created.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// An <see cref="InterfaceInterceptor{TInterface}"/> descendant that has at least one public constructor.
        /// </summary>
        public Type? Interceptor { get; }

        /// <summary>
        /// Creates a new <see cref="ProxyGenerator"/> instance which hooks into the given <paramref name="interface"/>.
        /// </summary>
        /// <param name="interface">The interface to be proxied</param>
        /// <param name="interceptor">The interceptor implementation. Should be an <see cref="InterfaceInterceptor{TInterface, TTarget}"/> descendant</param>
        public ProxyGenerator(Type @interface, Type interceptor): base(id: GenerateId(nameof(ProxyGenerator), @interface, interceptor))
        {
            //
            // The rest of validation is done in the compile phase.
            //

            Interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
            Target = @interface ?? throw new ArgumentNullException(nameof(@interface));
        }

        /// <summary>
        /// Creates a new <see cref="ProxyGenerator"/> instance.
        /// </summary>
        /// <param name="class">The class to be proxied</param>
        /// <remarks>The interceptor is passed as an <see cref="IInterceptor"/> implementation during the proxy activation.</remarks>
        public ProxyGenerator(Type @class): base(id: GenerateId(nameof(ProxyGenerator), @class)) =>
            Target = @class ?? throw new ArgumentNullException(nameof(@class));

        private protected override ProxyUnitSyntaxFactoryBase CreateMainUnit(string? asmName, ReferenceCollector referenceCollector) => Interceptor is not null
            ? new InterfaceProxySyntaxFactory
            (
                MetadataTypeInfo.CreateFrom(Target),
                MetadataTypeInfo.CreateFrom(Interceptor),
                asmName,
                OutputType.Module,
                referenceCollector
            )      
            : new ClassProxySyntaxFactory
            (
                MetadataTypeInfo.CreateFrom(Target),
                asmName,
                OutputType.Module,
                referenceCollector
            );
    }
}