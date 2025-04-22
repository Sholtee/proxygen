/********************************************************************************
* DuckGenerator{TInterface, TTarget}.cs                                         *
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
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context) => new DuckSyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
            context
        );
    }

    /// <summary>
    /// Type generator for creating a proxy that wraps the <typeparamref name="TTarget"/> to implement the <typeparamref name="TInterface"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface for which the proxy will be created.</typeparam>
    /// <typeparam name="TTarget">The target implementing all the <typeparamref name="TInterface"/> members.</typeparam>
    [SupportsSourceGeneration]
    public sealed class DuckGenerator<TInterface, TTarget>: Generator<DuckGenerator, DuckGenerator<TInterface, TTarget>> where TInterface: class
    {
        /// <inheritdoc/>
        protected override DuckGenerator GetConcreteGenerator() => new(typeof(TInterface), typeof(TTarget));

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static async Task<TInterface> ActivateAsync(TTarget target, CancellationToken cancellation = default) =>
            (TInterface) await Instance.ActivateAsync(target ?? throw new ArgumentNullException(nameof(target)), cancellation);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static TInterface Activate(TTarget target) =>
            (TInterface) Instance.Activate(target ?? throw new ArgumentNullException(nameof(target)));
    }
}