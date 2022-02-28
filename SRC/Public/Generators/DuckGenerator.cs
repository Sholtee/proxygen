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
    /// Type generator for creating proxies that let target behaves like an interface.
    /// </summary>
    public sealed class DuckGenerator: Generator
    {
        /// <summary>
        /// The target who implements all the <see cref="Interface"/> members.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// The interface to which the proxy will be created.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// Creates a new <see cref="DuckGenerator"/> instance.
        /// </summary>
        public DuckGenerator(Type iface, Type target)
        {
            //
            // Nem kell itt tulzasba vinni a validalast, generalaskor ugy is elhasal majd a rendszer ha vmi gond van
            //

            Target = target ?? throw new ArgumentNullException(nameof(target));
            Interface = iface ?? throw new ArgumentNullException(nameof(iface));
        }

        internal override IEnumerable<ITypeResolution> SupportedResolutions
        {
            get 
            {
                Type generator = GetType();
                ITypeInfo
                    iface = MetadataTypeInfo.CreateFrom(Interface),
                    target = MetadataTypeInfo.CreateFrom(Target);

                yield return new RuntimeCompiledTypeResolutionStrategy
                (
                    generator,
                    new DuckSyntaxFactory
                    (
                        iface,
                        target,
                        $"Duck_{ITypeInfoExtensions.GetMD5HashCode(iface, target)}",
                        OutputType.Module,
                        MetadataTypeInfo.CreateFrom(generator),
                        new ReferenceCollector()
                    )
                );
            }
        }
    }
}