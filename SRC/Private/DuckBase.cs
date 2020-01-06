/********************************************************************************
* DuckBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Defines the base class for duck typing.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    public abstract class DuckBase<T>: IHasTarget<T>
    {
        /// <summary>
        /// The target.
        /// </summary>
        public T Target { get; }

        /// <summary>
        /// Creates a new <see cref="DuckBase{T}"/> instance.
        /// </summary>
        /// <param name="target">The target of the entity being created.</param>
        public DuckBase(T target) => Target = target; // ne Protected legyen
    }
}
