/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Base of the generators.
    /// </summary>
    /// <remarks>Generators can not be instantiated. To access the created <see cref="Type"/> use the <see cref="GetGeneratedType()"/> or <see cref="GetGeneratedTypeAsync(CancellationToken)"/> method.</remarks>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        #region Private
        private static readonly SemaphoreSlim FLock = new(1, 1);

        private static Type? FType;

        private static Type GetGeneratedType(CancellationToken cancellation)
        {
            try
            {
                ITypeGenerator self = new TDescendant();

                return self
                    .TypeResolutionStrategy
                    .Resolve(cancellation);
            }

            //
            // "new TDescendant()" Activator.CreateInstace() hivas valojaban
            //

            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        private readonly ITypeResolution FTypeResolution;
        ITypeResolution ITypeGenerator.TypeResolutionStrategy => FTypeResolution;
        #endregion

        #region Protected
        /// <summary>
        /// Creates a new <see cref="TypeGenerator{TDescendant}"/> instance.
        /// </summary>
        protected TypeGenerator() => FTypeResolution = SupportedResolutions.Single(strat => strat.ShouldUse);

        /// <summary>
        /// Returns the supported type resolution strategies.
        /// </summary>
        private protected abstract IEnumerable<ITypeResolution> SupportedResolutions { get; }
        #endregion

        #region Public
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
                #pragma warning disable CA1508 // This method can be called parallelly so there is no dead code
                return FType ??= GetGeneratedType(cancellation);
                #pragma warning restore CA1508
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
                #pragma warning disable CA1508 // This method can be called parallelly so there is no dead code
                return FType ??= GetGeneratedType(default);
                #pragma warning restore CA1508
            }
            finally { FLock.Release(); }
        }
        #endregion
    }
}
