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
    /// Type generator for creating proxies that let target behaves like an interface.
    /// </summary>
    public sealed record DuckGenerator: Generator
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