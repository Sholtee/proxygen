/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Generators
{
    using Internals;

    /// <summary>
    /// Type generator for creating a proxy that wraps the <typeparamref name="TTarget"/> to implement the <typeparamref name="TInterface"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface for which the proxy will be created.</typeparam>
    /// <typeparam name="TTarget">The target implementing all the <typeparamref name="TInterface"/> members.</typeparam>
    public sealed class DuckGenerator<TInterface, TTarget>: Generator<TInterface, DuckGenerator<TInterface, TTarget>> where TInterface: class
    {
        /// <inheritdoc/>
        protected override Generator GetConcreteGenerator() => new DuckGenerator(typeof(TInterface), typeof(TTarget));
    }
}