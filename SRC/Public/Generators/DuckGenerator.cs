/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Generators
{
    using Abstractions;
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
        public DuckGenerator() => SyntaxFactory = new DuckSyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(typeof(TInterface)),
            MetadataTypeInfo.CreateFrom(typeof(TTarget)),
            MetadataAssemblyInfo.CreateFrom(typeof(DuckGenerator<,>).Assembly),
            TypeResolutionStrategy.AssemblyName,
            TypeResolutionStrategy.Type,
            MetadataTypeInfo.CreateFrom(GetType())
        );

        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public override IUnitSyntaxFactory SyntaxFactory { get; }
    }
}