/********************************************************************************
* ProxyGenerator{TClass}.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        public override ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context) => new ClassProxySyntaxFactory
        (
            SymbolTypeInfo.CreateFrom(generator.TypeArguments[0], compilation),
            context
        );
    }

    /// <summary>
    /// Type generator for creating proxies that intercept class method calls.
    /// </summary>
    /// <typeparam name="TClass">The class to be proxied</typeparam>
    [SupportsSourceGeneration]
    public sealed class ProxyGenerator<TClass> : Generator<TClass, ClassProxyGenerator, ProxyGenerator<TClass>> where TClass : class
    {
        /// <inheritdoc/>
        protected override ClassProxyGenerator GetConcreteGenerator() => new(typeof(TClass));

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static async Task<TClass> ActivateAsync(IInterceptor interceptor, Tuple ctorParamz, CancellationToken cancellation = default) =>
            (TClass) await Instance.ActivateAsync(interceptor, ctorParamz, cancellation);

        /// <summary>
        /// Creates an instance of the generated type.
        /// </summary>
        public static TClass Activate(IInterceptor interceptor, Tuple ctorParamz) =>
            (TClass) Instance.Activate(interceptor, ctorParamz);
    }
}