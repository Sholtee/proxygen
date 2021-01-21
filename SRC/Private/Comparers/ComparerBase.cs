/********************************************************************************
* ComparerBase.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class ComparerBase<TConcreteComparer, T> : IEqualityComparer<T> where TConcreteComparer : ComparerBase<TConcreteComparer, T>, new()
    {
        public virtual bool Equals(T x, T y) => ReferenceEquals(x, y) || GetHashCode(x) == GetHashCode(y);

        public abstract int GetHashCode(T obj);

        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Every descendant must have its own Instance value.")]
        public static TConcreteComparer Instance { get; } = new TConcreteComparer();
    }
}
