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
    public sealed class ProxyGenerator<TInterface, TInterceptor> : TypeGenerator<ProxyGenerator<TInterface, TInterceptor>> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        internal override IEnumerable<ITypeResolution> SupportedResolutions
        {
            get
            {
                Type generatorType = GetType();
                yield return new EmbeddedTypeResolutionStrategy(generatorType);

                ITypeInfo generatorTypeMeta = MetadataTypeInfo.CreateFrom(generatorType);
                yield return new RuntimeCompiledTypeResolutionStrategy
                (
                    generatorType,
                    new ProxySyntaxFactory
                    (
                        MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                        MetadataTypeInfo.CreateFrom(typeof(TInterceptor)),
                        $"Generated_{generatorTypeMeta.GetMD5HashCode()}",
                        OutputType.Module,
                        generatorTypeMeta
                    )
                );
            }
        }
    }
}