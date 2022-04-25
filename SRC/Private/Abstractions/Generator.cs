/********************************************************************************
* Generator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Base of untyped generators.
    /// </summary>
    public abstract record Generator: TypeEmitter
    {
        private protected override IEnumerable<UnitSyntaxFactoryBase> CreateChunks(ReferenceCollector referenceCollector)
        {
            if (typeof(MethodImplAttribute).Assembly.GetType("System.Runtime.CompilerServices.ModuleInitializerAttribute", throwOnError: false) is null)
                yield return new ModuleInitializerSyntaxFactory(OutputType.Unit, referenceCollector);
        }

        //
        // Mivel a Task minden metodusa szal biztos (https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-6.0#thread-safety) ezert nem
        // gond ha ugyanazon a peldanyon osztozunk.
        //

        internal Task<Type> GetGeneratedTypeAsyncInternal() => Cache.GetOrAdd
        (
            //
            // Ha ket generatornak azonos a hash-e (ezert hasznalunk record tipust) akkor ugyanazt a tipust is generaljak.
            //

            this,
            static self => Task<Type>.Factory.StartNew
            (
                //
                // Megszakitast itt nem adhatunk at mivel az a factoryaba agyazodna -> Ha egyszer
                // megszakitasra kerul a fuggveny onnantol soha tobbet nem lehetne hivni.
                //

                static self => ((Generator) self).Emit(null, WorkingDirectories.Instance.AssemblyCacheDir, default),
                self
            )
        );

        #if NETSTANDARD2_1_OR_GREATER
        private static object ActivateInternal(Type t, ITuple? tuple) => 
        #else
        internal static object ActivateInternal(Type t, object? tuple) =>
        #endif
            Cache
                .GetOrAdd(t, static t => ProxyActivator.Create(t))
                .Invoke(tuple);

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

            return Task.WhenAny
            (
                GetGeneratedTypeAsyncInternal(),
                tcs.Task
            ).Unwrap();
        }

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
            ActivateInternal(await GetGeneratedTypeAsync(cancellation).ConfigureAwait(false), tuple);

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
            ActivateInternal(GetGeneratedType(), tuple);
        #endregion
    }
}
