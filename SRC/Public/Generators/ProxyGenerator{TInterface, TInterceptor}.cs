/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
                ProxySyntaxFactory syntaxFactory = new
                (
                    MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                    MetadataTypeInfo.CreateFrom(typeof(TInterceptor)),
                    null,
                    OutputType.Module,
                    MetadataTypeInfo.CreateFrom(GetType()),
                    new ReferenceCollector()
                );

                yield return new LoadedTypeResolutionStrategy(syntaxFactory);
                yield return new RuntimeCompiledTypeResolutionStrategy(syntaxFactory);
            }
        }
    }
}