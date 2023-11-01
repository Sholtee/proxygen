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
        public DuckGenerator(Type iface, Type target): base(id: new { iface, target })
        {
            //
            // The rest of validation is done in the compile phase.
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