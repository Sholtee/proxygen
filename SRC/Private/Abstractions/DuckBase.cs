/********************************************************************************
* DuckBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Defines the base class for ducks.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    [SuppressMessage("Design", "CA1012:Abstract types should not have public constructors", Justification = "Proxygenerator needs public constructor")]
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
        public DuckBase(T target) =>  // ne "protected" legyen
            Target = target ?? throw new ArgumentNullException(nameof(target));
    }
}
