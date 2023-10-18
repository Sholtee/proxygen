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
    public abstract class Generator: TypeEmitter
    {
        private static readonly ConcurrentDictionary<Generator, Task<Type>> FFactoryCache = new(GeneratorComparer.Instance);
        private static readonly ConcurrentDictionary<Generator, Task<Func<object?, object>>> FActivatorCache = new(GeneratorComparer.Instance);

        private protected override IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector)
        {
            //
            // Don't use Type.GetType() here as it would find the internal implementation in this
            // assembly.
            //

            if (typeof(MethodImplAttribute).Assembly.GetType("System.Runtime.CompilerServices.ModuleInitializerAttribute", throwOnError: false) is null)
                yield return new ModuleInitializerSyntaxFactory(OutputType.Unit, referenceCollector);
        }

        /// <summary>
        /// Createsa new <see cref="Generator"/> instance.
        /// </summary>
        protected Generator(object id) => Id = id;

        //
        // Since all Task methods are thread safe (https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-6.0#thread-safety)
        // we can cache in this method.
        //

        internal Task<Type> GetGeneratedTypeAsyncInternal() => FFactoryCache.GetOrAdd
        (
            //
            // Generators havnig the same Id emit the same output, too.
            //

            this,
            static self => Task<Type>.Factory.StartNew
            (
                //
                // Since the returned task is cached, we cannot cancel it.
                //

                static self => ((Generator) self).Emit(null, WorkingDirectories.Instance.AssemblyCacheDir, default),
                self
            )
        );

        internal Task<Func<object?, object>> GetActivatorAsyncInternal() => FActivatorCache.GetOrAdd
        (
            this,
            async static self => ProxyActivator.Create
            (
                await self.GetGeneratedTypeAsyncInternal()
            ) 
        );

        #region Public
        /// <summary>
        /// Unique generator id. Generators emitting the same output should have the same id.
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default) => GetGeneratedTypeAsyncInternal().AsCancellable(cancellation);

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Type GetGeneratedType() => GetGeneratedTypeAsyncInternal()
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
            (await GetActivatorAsyncInternal().AsCancellable(cancellation)).Invoke(tuple);

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
            GetActivatorAsyncInternal()
            .GetAwaiter()
            .GetResult()
            .Invoke(tuple);
        #endregion
    }
}
