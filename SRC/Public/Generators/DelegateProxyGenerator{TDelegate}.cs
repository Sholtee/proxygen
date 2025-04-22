/********************************************************************************
* DelegateProxyGenerator{TDelegate}.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    file sealed class SupportsSourceGenerationAttribute : SupportsSourceGenerationAttributeBase
    {
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context) => new DelegateProxySyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            context
        );
    }

    /// <summary>
    /// Type generator for creating proxies that intercept delegate invocations.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate to be proxied</typeparam>
    [SupportsSourceGeneration]
    public sealed class DelegateProxyGenerator<TDelegate> : Generator<DelegateProxyGenerator, DelegateProxyGenerator<TDelegate>> where TDelegate : Delegate
    {
        /// <inheritdoc/>
        protected override DelegateProxyGenerator GetConcreteGenerator() => new(typeof(TDelegate));

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static async Task<TDelegate> ActivateAsync(IInterceptor interceptor, TDelegate @delegate, CancellationToken cancellation = default) =>
            (TDelegate) await Instance.ActivateAsync(interceptor, @delegate, cancellation);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static TDelegate Activate(IInterceptor interceptor, TDelegate @delegate) =>
            (TDelegate) Instance.Activate(interceptor, @delegate);
    }
}