/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Base of untyped generators.
    /// </summary>
    public abstract class Generator : TypeEmitter
    {
        #region Private
        private sealed class GeneratorContext
        {
            public Type? GeneratedType { get; set; }

            public SemaphoreSlim Lock { get; } = new(1, 1);
        }

        private static readonly ConcurrentDictionary<object, GeneratorContext> FContextCache = new();
        private static readonly ConcurrentDictionary<object, Func<object?, object>> FActivatorCache = new();

        private protected override IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector)
        {
            //
            // Don't use Type.GetType() here as it would find the internal implementation in this
            // assembly.
            //

            if (typeof(MethodImplAttribute).Assembly.GetType("System.Runtime.CompilerServices.ModuleInitializerAttribute", throwOnError: false) is null)
                yield return new ModuleInitializerSyntaxFactory(OutputType.Unit, referenceCollector);
        }

        private async Task<Type> GetGeneratedTypeAsyncInternal(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            GeneratorContext context = FContextCache.GetOrAdd(Id, static _ => new GeneratorContext());
            if (context.GeneratedType is not null)
                return context.GeneratedType;

            await context.Lock.WaitAsync(cancellation);

            try
            {
                context.GeneratedType ??= await EmitAsync(null, WorkingDirectories.Instance.AssemblyCacheDir, cancellation);
            }
            finally
            {
                context.Lock.Release();
            }

            return context.GeneratedType;
        }

        private Task<Func<object?, object>> GetActivatorAsyncInternal(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            return FActivatorCache.TryGetValue(Id, out Func<object?, object> activator)
                ? Task.FromResult(activator)
                : Create();

            async Task<Func<object?, object>> Create()
            {
                return FActivatorCache.GetOrAdd(Id, ProxyActivator.Create(await GetGeneratedTypeAsyncInternal(cancellation)));
            }
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="Generator"/> instance.
        /// </summary>
        protected Generator(object id) => Id = id;

        #region Public
        /// <summary>
        /// Unique generator id. Generators emitting the same output should have the same id.
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default) => GetGeneratedTypeAsyncInternal(cancellation);

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Type GetGeneratedType() => GetGeneratedTypeAsync()
            .GetAwaiter()
            .GetResult();

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <param name="cancellation">Token to cancel the operation.</param>
        /// <returns>The just activated instance.</returns>
        #if NETSTANDARD2_1_OR_GREATER
        public async Task<object> ActivateAsync(ITuple? tuple, CancellationToken cancellation = default) =>
        #else
        public async Task<object> ActivateAsync(object? tuple, CancellationToken cancellation = default) =>
        #endif
            (await GetActivatorAsyncInternal(cancellation)).Invoke(tuple);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <returns>The just activated instance.</returns>
        #if NETSTANDARD2_1_OR_GREATER
        public object Activate(ITuple? tuple) =>
        #else
        public object Activate(object? tuple) =>
        #endif
            GetActivatorAsyncInternal(default)
            .GetAwaiter()
            .GetResult()
            .Invoke(tuple);
        #endregion
    }
}
