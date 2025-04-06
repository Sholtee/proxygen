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
    public abstract class Generator(object id) : TypeEmitter
    {
        #region Private
        private sealed class ContextWrapper
        {
            public TypeContext? Context { get; set; }

            public SemaphoreSlim Lock { get; } = new(1, 1);
        }

        private static readonly ConcurrentDictionary<object, ContextWrapper> FContextCache = new();

        private protected override IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector)
        {
            //
            // Don't use Type.GetType() here as it would find the internal implementation in this
            // assembly.
            //

            if (typeof(MethodImplAttribute).Assembly.GetType("System.Runtime.CompilerServices.ModuleInitializerAttribute", throwOnError: false) is null)
                yield return new ModuleInitializerSyntaxFactory(OutputType.Unit, referenceCollector);
        }

        private async Task<TypeContext> GetContextAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            ContextWrapper context = FContextCache.GetOrAdd(Id, static _ => new ContextWrapper());
            if (context.Context is not null)
                return context.Context;

            await context.Lock.WaitAsync(cancellation);

            try
            {
                context.Context ??= await EmitAsync(null, WorkingDirectories.Instance.AssemblyCacheDir, cancellation);
            }
            finally
            {
                context.Lock.Release();
            }

            return context.Context;
        }
        #endregion

        #region Protected
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
            TypeContext context = await GetContextAsync(cancellation);
            return context.Activator(tuple!);
        }
        #endregion

        #region Public
        /// <summary>
        /// Unique generator id. Generators emitting the same output should have the same id.
        /// </summary>
        public object Id { get; } = id ?? throw new ArgumentNullException(nameof(id));

        /// <summary>
        /// Gets the generated <see cref="Type"/> asynchronously .
        /// </summary>
        /// <remarks>The returned <see cref="Type"/> is generated only once.</remarks>
        public async Task<Type> GetGeneratedTypeAsync(CancellationToken cancellation = default)
        {
            TypeContext context = await GetContextAsync(cancellation);
            return context.Type;
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