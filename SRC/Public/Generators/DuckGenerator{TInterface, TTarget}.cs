/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
                Type generatorType = GetType();
                yield return new EmbeddedTypeResolutionStrategy(generatorType);

                ITypeInfo generatorTypeMeta = MetadataTypeInfo.CreateFrom(generatorType);
                yield return new RuntimeCompiledTypeResolutionStrategy
                (
                    generatorType,
                    new DuckSyntaxFactory
                    (
                        MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                        MetadataTypeInfo.CreateFrom(typeof(TTarget)),
                        $"Generated_{generatorTypeMeta.GetMD5HashCode()}",
                        OutputType.Module,
                        generatorTypeMeta
                    )
                );
            }
        }
    }
}