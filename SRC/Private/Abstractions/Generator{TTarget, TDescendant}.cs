/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Tuple =
    #if NETSTANDARD2_1_OR_GREATER
    System.Runtime.CompilerServices.ITuple;
    #else
    object;
    #endif

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Base of typed generators.
    /// </summary>
    /// <remarks>Generators should not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType()"/> or <see cref="GetGeneratedTypeAsync(CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class Generator<TTarget, TDescendant> where TDescendant: Generator<TTarget, TDescendant>, new()
    {
        /// <summary>
        /// Gets the concrete generator.
        /// </summary>
        protected abstract Generator GetConcreteGenerator();

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="ctorParamz">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <param name="cancellation">Token to cancel the operation.</param>
        /// <returns>The just activated instance.</returns>
        protected static async Task<TTarget> ActivateAsync(Tuple? ctorParamz, CancellationToken cancellation)
            => (TTarget) await Instance.ActivateAsync(ctorParamz, cancellation).ConfigureAwait(false);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="ctorParamz">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <returns>The just activated instance.</returns>
        protected static TTarget Activate(Tuple ctorParamz) => (TTarget) Instance.Activate(ctorParamz);

        /// <summary>
        /// The singleton generator instance.
        /// </summary>
        public static Generator Instance { get; } = new TDescendant().GetConcreteGenerator();

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default) => Instance.GetGeneratedTypeAsync(cancellation);

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static Type GetGeneratedType() => Instance.GetGeneratedType();
    }
}
