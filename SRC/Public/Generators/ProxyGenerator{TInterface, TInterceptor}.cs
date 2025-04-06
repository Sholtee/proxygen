/********************************************************************************
* ProxyGenerator{TInterface, TInterceptor}.cs                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Tuple =
    #if NETSTANDARD2_1_OR_GREATER
    System.Runtime.CompilerServices.ITuple;
    #else
    object;
    #endif

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    file sealed class SupportsSourceGenerationAttribute : SupportsSourceGenerationAttributeBase
    {
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, Compilation compilation, ReferenceCollector? referenceCollector) => new InterfaceProxySyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
            compilation.Assembly.Name,
            OutputType.Unit,
            referenceCollector
        );
    }

    /// <summary>
    /// Type generator for creating proxies that intercept interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface for which the proxy will be created.</typeparam>
    /// <typeparam name="TInterceptor">An <see cref="InterfaceInterceptor{TInterface, TTarget}"/> descendant that has at least one public constructor.</typeparam>
    [SupportsSourceGeneration]
    public sealed class ProxyGenerator<TInterface, TInterceptor> : Generator<TInterface, InterfaceProxyGenerator, ProxyGenerator<TInterface, TInterceptor>>
        where TInterface : class
        where TInterceptor: InterfaceInterceptorBase<TInterface>
    {
        /// <inheritdoc/>
        protected override InterfaceProxyGenerator GetConcreteGenerator() => new(typeof(TInterface), typeof(TInterceptor));

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static async Task<TInterface> ActivateAsync(Tuple ctorParamz, CancellationToken cancellation = default) =>
            (TInterface) await Instance.ActivateAsync(ctorParamz, cancellation);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static TInterface Activate(Tuple ctorParamz) =>
            (TInterface) Instance.Activate(ctorParamz);
    }
}