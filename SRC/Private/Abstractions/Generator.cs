/********************************************************************************
* Generator.cs                                                                  *
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
    public abstract class Generator<TInterface, TDescendant> where TDescendant : Generator<TInterface, TDescendant>, new()
    {
        #region Private
        private static readonly SemaphoreSlim FLock = new(1, 1);

        private static Type? FType;

        private static ProxyActivator.Activator? FActivator;

        private static Type GetGeneratedType(CancellationToken cancellation)
        {
            try
            {
                TDescendant self = new();

                return self
                    .SupportedResolutions
                    .Select(res => res.TryResolve(cancellation))
                    .First(t => t is not null)!;
            }

            //
            // "new TDescendant()" Activator.CreateInstace() hivas valojaban
            //

            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

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
        public static async Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
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
        public static Type GetGeneratedType() 
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
        public static async Task<TInterface> ActivateAsync(ITuple? tuple, CancellationToken cancellation = default)
        #else
        public static async Task<TInterface> ActivateAsync(object? tuple, CancellationToken cancellation = default)
        #endif
        {
            if (FActivator is null)
            {
                Type type = await GetGeneratedTypeAsync(cancellation).ConfigureAwait(false);

                await FLock.WaitAsync(cancellation).ConfigureAwait(false);

                try
                {
                    #pragma warning disable CA1508 // This method can be called parallelly so there is no dead code
                    FActivator ??= ProxyActivator.Create(type);
                    #pragma warning restore CA1508
                }
                finally { FLock.Release(); }
            }
            return (TInterface) FActivator(tuple);
        }

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
        {
            if (FActivator is null)
            {
                Type type = GetGeneratedType();

                FLock.Wait();

                try
                {
                    #pragma warning disable CA1508 // This method can be called parallelly so there is no dead code
                    FActivator ??= ProxyActivator.Create(type);
                    #pragma warning restore CA1508
                }
                finally { FLock.Release(); }
            }
            return (TInterface) FActivator(tuple);
        }
        #endregion
    }
}
