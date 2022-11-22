/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    file sealed class SupportsSourceGenerationAttribute : SupportsSourceGenerationAttributeBase
    {
        public override ProxyUnitSyntaxFactory CreateMainUnit(INamedTypeSymbol generator, Compilation compilation, ReferenceCollector? referenceCollector) => new ProxySyntaxFactory
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
    /// <typeparam name="TInterceptor">An <see cref="InterfaceInterceptor{TInterface}"/> descendant that has at least one public constructor.</typeparam>
    [SupportsSourceGeneration]
    public sealed class ProxyGenerator<TInterface, TInterceptor> : Generator<TInterface, ProxyGenerator<TInterface, TInterceptor>> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        /// <inheritdoc/>
        protected override Generator GetConcreteGenerator() => new ProxyGenerator(typeof(TInterface), typeof(TInterceptor));
    }
}