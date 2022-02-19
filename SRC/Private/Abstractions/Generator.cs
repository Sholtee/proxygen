/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
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
    public abstract class Generator
    {
        #region Private
        private readonly SemaphoreSlim FLock = new(1, 1);

        private Type? FType;

        private ProxyActivator.Activator? FActivator;

        private Type GetGeneratedType(CancellationToken cancellation) => SupportedResolutions
            .Select(res => res.TryResolve(cancellation))
            .First(t => t is not null)!;

        /// <summary>
        /// Returns the supported type resolution strategies.
        /// </summary>
        internal abstract IEnumerable<ITypeResolution> SupportedResolutions { get; }
        #endregion

        #region Public
        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public async Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
        {
            if (FType is not null) return FType;

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
        public Type GetGeneratedType() 
        {
            if (FType is not null) return FType;

            FLock.Wait();

            try
            {
                #pragma warning disable CA1508 // This method can be called parallelly so there is no dead code
                return FType ??= GetGeneratedType(default);
                #pragma warning restore CA1508
            }
            finally { FLock.Release(); }
        }

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <param name="cancellation">Token to cancel the operation.</param>
        /// <returns>The just activated instance.</returns>
        #if NETSTANDARD2_1_OR_GREATER
        public async Task<object> ActivateAsync(ITuple? tuple, CancellationToken cancellation = default)
        #else
        public async Task<object> ActivateAsync(object? tuple, CancellationToken cancellation = default)
        #endif
        {
            if (FActivator is null)
            {
                Type type = await GetGeneratedTypeAsync(cancellation).ConfigureAwait(false);

                await FLock.WaitAsync(cancellation).ConfigureAwait(false);

                try
                {
                    FActivator ??= ProxyActivator.Create(type);
                }
                finally { FLock.Release(); }
            }
            return FActivator(tuple);
        }

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <returns>The just activated instance.</returns>
        #if NETSTANDARD2_1_OR_GREATER
        public object Activate(ITuple? tuple)
        #else
        public object Activate(object? tuple)
        #endif
        {
            if (FActivator is null)
            {
                Type type = GetGeneratedType();

                FLock.Wait();

                try
                {
                    FActivator ??= ProxyActivator.Create(type);
                }
                finally { FLock.Release(); }
            }
            return FActivator(tuple);
        }
        #endregion
    }
}
