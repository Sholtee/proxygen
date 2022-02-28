/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that intercept interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to which the proxy will be created.</typeparam>
    /// <typeparam name="TInterceptor">An <see cref="InterfaceInterceptor{TInterface}"/> descendant that has at least one public constructor.</typeparam>
    public sealed class ProxyGenerator<TInterface, TInterceptor> : Generator<TInterface, ProxyGenerator<TInterface, TInterceptor>> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        internal override IEnumerable<ITypeResolution> SupportedResolutions
        {
            get
            {
                Type generator = GetType();
                yield return new EmbeddedTypeResolutionStrategy(generator);

                ITypeInfo interceptor = MetadataTypeInfo.CreateFrom(typeof(TInterceptor));
                yield return new RuntimeCompiledTypeResolutionStrategy
                (
                    generator,
                    new ProxySyntaxFactory
                    (
                        MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                        interceptor,
                        $"Proxy_{interceptor.GetMD5HashCode()}",
                        OutputType.Module,
                        MetadataTypeInfo.CreateFrom(generator),
                        new ReferenceCollector()
                    )
                );
            }
        }
    }
}