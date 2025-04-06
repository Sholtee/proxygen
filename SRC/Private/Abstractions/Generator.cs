/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
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
        #endregion

        #region Protected
        /// <summary>
        /// Creates a new <see cref="Generator"/> instance.
        /// </summary>
        protected Generator(object id) => Id = id;

        /// <summary>
        /// Creates unique generator ids. 
        /// </summary>
        protected static string GenerateId(string prefix, params IEnumerable<Type> types) =>
            $"{prefix}:{types.Select(MetadataTypeInfo.CreateFrom).GetMD5HashCode()}";

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        /// <param name="tuple">A <see cref="Tuple"/> containing the constructor parameters or null if you want to invoke the parameterless constructor.</param>
        /// <param name="cancellation">Token to cancel the operation.</param>
        /// <returns>The just activated instance.</returns>
        protected async Task<object> ActivateAsync(Tuple? tuple, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            if (!FActivatorCache.TryGetValue(Id, out Func<object?, object> activator))
            {
                activator = FActivatorCache.GetOrAdd(Id, ProxyActivator.Create(await GetGeneratedTypeAsync(cancellation)));
            }

            return activator(tuple);
        }
        #endregion

        #region Public
        /// <summary>
        /// Unique generator id. Generators emitting the same output should have the same id.
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public async Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
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

        /// <summary>
        /// Gets the generated <see cref="Type"/>.
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public Type GetGeneratedType() => GetGeneratedTypeAsync()
            .GetAwaiter()
            .GetResult();
        #endregion
    }
}