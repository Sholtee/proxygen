/********************************************************************************
* DuckBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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
        public DuckBase(T target)  // ne "protected" legyen
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Target = target;
        }
    }
}
