/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Base of typed generators.
    /// </summary>
    /// <remarks>Generators should not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType()"/> or <see cref="GetGeneratedTypeAsync(CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class Generator<TUntypedGenerator, TDescendant> where TDescendant: Generator<TUntypedGenerator, TDescendant>, new() where TUntypedGenerator: Generator
    {
        /// <summary>
        /// Gets the concrete generator.
        /// </summary>
        protected abstract TUntypedGenerator GetConcreteGenerator();

        /// <summary>
        /// The singleton generator instance.
        /// </summary>
        public static TUntypedGenerator Instance { get; } = new TDescendant().GetConcreteGenerator();

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously.
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
