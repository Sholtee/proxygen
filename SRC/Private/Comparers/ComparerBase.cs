/********************************************************************************
* ComparerBase.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    //
    // Don't use Solti.Utils.Primitives.ComparerBase otherwise we should provide that library for Roslyn too.
    //

    /// <summary>
    /// Base class of equality comparers
    /// </summary>
    internal abstract class ComparerBase<TConcreteComparer, T> : IEqualityComparer<T> where TConcreteComparer : ComparerBase<TConcreteComparer, T>, new()
    {
        /// <inheritdoc/>
        public abstract bool Equals(T x, T y);

        /// <inheritdoc/>
        public abstract int GetHashCode(T obj);

        /// <summary>
        /// The singleton instance of this comparer.
        /// </summary>
        public static TConcreteComparer Instance { get; } = new TConcreteComparer();
    }
}
