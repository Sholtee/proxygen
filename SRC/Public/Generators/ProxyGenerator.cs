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
        /// The interface for which the proxy will be created.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// An <see cref="InterfaceInterceptor{TInterface}"/> descendant that has at least one public constructor.
        /// </summary>
        public Type Interceptor { get; }

        /// <summary>
        /// Creates a new <see cref="ProxyGenerator"/> instance.
        /// </summary>
        public ProxyGenerator(Type iface, Type interceptor): base
        (
            (iface ?? throw new ArgumentNullException(nameof(iface))).GetHashCode() ^ (interceptor ?? throw new ArgumentNullException(nameof(interceptor))).GetHashCode()
        )
        {
            //
            // Nem kell itt tulzasba vinni a validalast, generalaskor ugy is elhasal majd a rendszer ha vmi gond van
            //

            Interceptor = interceptor;
            Interface = iface;
        }

        private protected override ProxyUnitSyntaxFactory CreateMainUnit(string? asmName, ReferenceCollector referenceCollector) => new ProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Interface),
            MetadataTypeInfo.CreateFrom(Interceptor),
            asmName,
            OutputType.Module,
            referenceCollector
        );
    }
}