/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Base of the generators.
    /// </summary>
    /// <remarks>Generators should not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType()"/> or <see cref="GetGeneratedTypeAsync(CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class Generator<TInterface, TDescendant> where TDescendant: Generator<TInterface, TDescendant>, new()
    {
        /// <summary>
        /// Gets the concrete generator.
        /// </summary>
        /// <returns></returns>
        protected abstract Generator GetConcreteGenerator();

        /// <summary>
        /// The singleton generator instance.
        /// </summary>
        public static Generator Instance { get; } = new TDescendant().GetConcreteGenerator();

        #region Public
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

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <param name="cancellation">Token to cancel the operation.</param>
        /// <returns>The just activated instance.</returns>
        #if NETSTANDARD2_1_OR_GREATER
        public static async Task<TInterface> ActivateAsync(ITuple? tuple, CancellationToken cancellation = default)
        #else
        public static async Task<TInterface> ActivateAsync(object? tuple, CancellationToken cancellation = default)
        #endif
            => (TInterface) await Instance.ActivateAsync(tuple, cancellation).ConfigureAwait(false);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <returns>The just activated instance.</returns>
        #if NETSTANDARD2_1_OR_GREATER
        public static TInterface Activate(ITuple? tuple)
        #else
        public static TInterface Activate(object? tuple)
        #endif
            => (TInterface) Instance.Activate(tuple);
        #endregion
    }
}
