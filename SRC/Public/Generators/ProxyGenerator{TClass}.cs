/********************************************************************************
* ProxyGenerator{TClass}.cs                                                     *
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
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, Compilation compilation, ReferenceCollector? referenceCollector) => new ClassProxySyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            compilation.Assembly.Name,
            OutputType.Unit,
            referenceCollector
        );
    }

    /// <summary>
    /// Type generator for creating proxies that intercept class method calls.
    /// </summary>
    /// <typeparam name="TClass">The class to be proxied</typeparam>
    [SupportsSourceGeneration]
    public sealed class ProxyGenerator<TClass> : Generator<TClass, ProxyGenerator<TClass>> where TClass : class
    {
        /// <inheritdoc/>
        protected override Generator GetConcreteGenerator() => new ProxyGenerator(typeof(TClass));

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static Task<TClass> ActivateAsync(IInterceptor interceptor, Tuple ctorParamz, CancellationToken cancellation = default) =>
            ActivateAsync(ctorParamz, cancellation);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static TClass Activate(IInterceptor interceptor, Tuple ctorParamz) =>
            Activate(ctorParamz);
    }
}