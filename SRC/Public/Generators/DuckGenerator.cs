/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating proxies that let <typeparamref name="TTarget"/> behaves like a <typeparamref name="TInterface"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface to which the proxy will be created.</typeparam>
    /// <typeparam name="TTarget">The target who implements all the <typeparamref name="TInterface"/> members.</typeparam>
    public sealed class DuckGenerator<TInterface, TTarget>: TypeGenerator<DuckGenerator<TInterface, TTarget>> where TInterface: class
    {
        /// <summary>
        /// Creates a new <see cref="DuckGenerator{TInterface, TTarget}"/> instance
        /// </summary>
        public DuckGenerator() : base
        (
            new EmbeddedTypeResolutionStrategy(typeof(DuckGenerator<TInterface, TTarget>)),
            new RuntimeCompiledTypeResolutionStrategy
            (
                typeof(DuckGenerator<TInterface, TTarget>),
                new DuckSyntaxFactory
                (
                    MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                    MetadataTypeInfo.CreateFrom(typeof(TTarget)),
                    $"Generated_{MetadataTypeInfo.CreateFrom(typeof(DuckGenerator<TInterface, TTarget>)).GetMD5HashCode()}",
                    OutputType.Module,
                    MetadataTypeInfo.CreateFrom(typeof(DuckGenerator<TInterface, TTarget>))
                )
            )
        )
        { }
    }
}