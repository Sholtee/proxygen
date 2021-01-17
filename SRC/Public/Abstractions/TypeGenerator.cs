/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Implements the <see cref="ITypeGenerator"/> interface.
    /// </summary>
    /// <remarks>Generators can not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType()"/> or <see cref="GetGeneratedTypeAsync(CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        #region Private
        private static readonly SemaphoreSlim FLock = new SemaphoreSlim(1, 1);

        private static Type? FType;

        private static Type GetGeneratedType(CancellationToken cancellation)
        {
            try
            {
                return new TDescendant().TypeResolutionStrategy.Resolve(cancellation);
            }

            //
            // "new TDescendant()" Activator.CreateInstace() hivas valojaban
            //

            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// Creates a new <see cref="TypeGenerator{TDescendant}"/> instance.
        /// </summary>
        protected TypeGenerator(params ITypeResolutionStrategy[] supportedResolutions) => 
            TypeResolutionStrategy = supportedResolutions.Single(strat => strat.ShouldUse);

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static async Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
        {
            if (FType != null) return FType;

            await FLock.WaitAsync(cancellation).ConfigureAwait(false);

            try
            {
                return FType ??= GetGeneratedType(cancellation);
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public static Type GetGeneratedType() 
        {
            if (FType != null) return FType;

            FLock.Wait();

            try
            {
                return FType ??= GetGeneratedType(default);
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// The strategy used to resolve the generated <see cref="Type"/>.
        /// </summary>
        public ITypeResolutionStrategy TypeResolutionStrategy { get; }
        #endregion
    }
}
