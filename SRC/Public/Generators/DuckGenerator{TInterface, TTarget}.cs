/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    file sealed class SupportsSourceGenerationAttribute : SupportsSourceGenerationAttributeBase
    {
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, Compilation compilation, ReferenceCollector? referenceCollector) => new DuckSyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[1], compilation),
            compilation.Assembly.Name,
            OutputType.Unit,
            SymbolAssemblyInfo.CreateFrom(generator.ContainingAssembly, compilation),
            referenceCollector
        );
    }

    /// <summary>
    /// Type generator for creating a proxy that wraps the <typeparamref name="TTarget"/> to implement the <typeparamref name="TInterface"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface for which the proxy will be created.</typeparam>
    /// <typeparam name="TTarget">The target implementing all the <typeparamref name="TInterface"/> members.</typeparam>
    [SupportsSourceGeneration]
    public sealed class DuckGenerator<TInterface, TTarget>: Generator<TInterface, DuckGenerator<TInterface, TTarget>> where TInterface: class
    {
        /// <inheritdoc/>
        protected override Generator GetConcreteGenerator() => new DuckGenerator(typeof(TInterface), typeof(TTarget));
    }
}