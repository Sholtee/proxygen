/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that let <typeparamref name="TTarget"/> behaves like a <typeparamref name="TInterface"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface to which the proxy will be created.</typeparam>
    /// <typeparam name="TTarget">The target who implements all the <typeparamref name="TInterface"/> members.</typeparam>
    public sealed class DuckGenerator<TInterface, TTarget>: Generator<TInterface, DuckGenerator<TInterface, TTarget>> where TInterface: class
    {
        internal override IEnumerable<ITypeResolution> SupportedResolutions
        {
            get 
            {
                DuckSyntaxFactory syntaxFactory = new
                (
                    MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                    MetadataTypeInfo.CreateFrom(typeof(TTarget)),
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