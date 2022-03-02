/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        //
        // Ha ugyanazzal a kulccsal hivjuk parhuzamosan a GetOrAdd()-et akkor a factory tobbszor is
        // meghivasra kerulhet (MSDN) -> Lazy
        //

        private static readonly ConcurrentDictionary<Generator, Lazy<Task<Type>>> FGeneratedTypes = new(GeneratorComparer.Instance);

        private static readonly ConcurrentDictionary<Type, Lazy<ProxyActivator.Activator>> FActivators = new();

        //
        // Mivel a Task minden metodusa szal biztos (https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-6.0#thread-safety) ezert nem
        // gond ha ugyanazon a peldanyon osztozunk.
        //

        internal Task<Type> GetGeneratedTypeAsyncInternal() => FGeneratedTypes.GetOrAdd
        (
            this,
            new Lazy<Task<Type>>
            (
                () => Task<Type>.Factory.StartNew
                (
                    () => 
                    {
                        foreach (ITypeResolution resolution in SupportedResolutions)
                        {
                            //
                            // Megszakitast itt nem adhatunk at mivel az a factoryaba agyazodna -> Ha egyszer
                            // megszakitasra kerul a fuggveny onnantol soha tobbet nem lehetne hivni.
                            //

                            Type? resolved = resolution.TryResolve(default);
                            if (resolved is not null)
                                return resolved;
                        }
                        throw new NotSupportedException();
                    }
                ),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        ).Value;

        /// <summary>
        /// Returns the supported type resolution strategies.
        /// </summary>
        internal abstract IEnumerable<ITypeResolution> SupportedResolutions { get; }

        #region Public
        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
        {
            TaskCompletionSource<Type> tcs = new();

            //
            // Ha a megszakitas mar kerelmezve lett akkor a SetCanceled() azonnal meghivasra kerul
            //

            cancellation.Register(tcs.SetCanceled);

            return Task.WhenAny(GetGeneratedTypeAsyncInternal(), tcs.Task).Unwrap();
        }

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Type GetGeneratedType() => GetGeneratedTypeAsyncInternal().Result;

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
            Type generated = await GetGeneratedTypeAsync(cancellation).ConfigureAwait(true);

            return FActivators
                .GetOrAdd(generated, new Lazy<ProxyActivator.Activator>(() => ProxyActivator.Create(generated)))
                .Value(tuple);
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
            Type generated = GetGeneratedType();

            return FActivators
                .GetOrAdd(generated, new Lazy<ProxyActivator.Activator>(() => ProxyActivator.Create(generated)))
                .Value(tuple);
        }
        #endregion
    }
}
