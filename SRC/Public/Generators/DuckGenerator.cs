/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating a proxy that wraps the <see cref="Target"/> to implement the <see cref="Interface"/>.
    /// </summary>
    public sealed class DuckGenerator: Generator
    {
        /// <summary>
        /// The target implementing all the <see cref="Interface"/> members.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// The interface for which the proxy will be created.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// Creates a new <see cref="DuckGenerator"/> instance.
        /// </summary>
        public DuckGenerator(Type iface, Type target): base
        (
            (target ?? throw new ArgumentNullException(nameof(target))).GetHashCode() ^ (iface ?? throw new ArgumentNullException(nameof(iface))).GetHashCode()
        )
        {
            //
            // Nem kell itt tulzasba vinni a validalast, generalaskor ugy is elhasal majd a rendszer ha vmi gond van
            //

            Target = target;
            Interface = iface;
        }

        private protected override ProxyUnitSyntaxFactory CreateMainUnit(string? asmName, ReferenceCollector referenceCollector) => new DuckSyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(Interface),
            MetadataTypeInfo.CreateFrom(Target),
            asmName,
            OutputType.Module,
            MetadataAssemblyInfo.CreateFrom(GetType().Assembly),
            referenceCollector
        );
    }
}