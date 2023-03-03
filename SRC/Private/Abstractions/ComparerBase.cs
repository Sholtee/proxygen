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

    internal abstract class ComparerBase<TConcreteComparer, T> : IEqualityComparer<T> where TConcreteComparer : ComparerBase<TConcreteComparer, T>, new()
    {
        public abstract bool Equals(T x, T y);

        public abstract int GetHashCode(T obj);

        public static TConcreteComparer Instance { get; } = new TConcreteComparer();
    }
}
