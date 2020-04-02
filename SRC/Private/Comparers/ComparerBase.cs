/********************************************************************************
* ComparerBase.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class ComparerBase<TConcreteComparer, T> : IEqualityComparer<T> where TConcreteComparer : ComparerBase<TConcreteComparer, T>, new()
    {
        public bool Equals(T x, T y) => ReferenceEquals(x, y) || GetHashCode(x) == GetHashCode(y);
        public abstract int GetHashCode(T obj);
        public static TConcreteComparer Instance { get; } = new TConcreteComparer();
    }
}
