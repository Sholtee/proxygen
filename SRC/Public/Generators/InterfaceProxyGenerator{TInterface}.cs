/********************************************************************************
* InterfaceProxyGenerator{TInterface}.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    file sealed class SupportsSourceGenerationAttribute : SupportsSourceGenerationAttributeBase
    {
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context) => new InterfaceProxySyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            context
        );
    }

    /// <summary>
    /// Type generator for creating proxies that intercept interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface for which the proxy will be created.</typeparam>
    [SupportsSourceGeneration]
    public sealed class InterfaceProxyGenerator<TInterface> : Generator<TInterface, InterfaceProxyGenerator, InterfaceProxyGenerator<TInterface>> where TInterface : class
    {
        /// <inheritdoc/>
        protected override InterfaceProxyGenerator GetConcreteGenerator() => new(typeof(TInterface));

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static async Task<TInterface> ActivateAsync(IInterceptor interceptor, object? target = null, CancellationToken cancellation = default) =>
            (TInterface) await Instance.ActivateAsync(interceptor, target, cancellation);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static TInterface Activate(IInterceptor interceptor, object? target = null) =>
            (TInterface) Instance.Activate(interceptor, target);
    }
}