/********************************************************************************
* ITypeResolutionStrategy.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;

    /// <summary>
    /// Represents the method how the generated <see cref="Type"/> will be resolved.
    /// </summary>
    public interface ITypeResolutionStrategy
    {
        /// <summary>
        /// The related <see cref="TypeGenerator{TDescendant}"/> descendant.
        /// </summary>
        Type GeneratorType { get; }

        /// <summary>
        /// The name of the <see cref="Assembly"/> that contains the generated <see cref="Type"/>.
        /// </summary>
        string ContainingAssembly { get; }

        /// <summary>
        /// The fully qualified name of the generated <see cref="Type"/>.
        /// </summary>
        string ClassName { get; }

        /// <summary>
        /// Returns true if the strategy should be used.
        /// </summary>
        bool ShouldUse { get; }

        /// <summary>
        /// Resolves the generated <see cref="Type"/>.
        /// </summary>
        Type Resolve(CancellationToken cancellation = default);
    }
}
