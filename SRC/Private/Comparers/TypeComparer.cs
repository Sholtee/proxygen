/********************************************************************************
* TypeComparer.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    //
    // Custom comparer required as (for instance) typeof(List<>).GetGenericArguments()[0] != typeof(IList<>).GetGenericArguments()[0]
    //

    internal sealed class TypeComparer : ComparerBase<TypeComparer, Type>
    {
        public override bool Equals(Type x, Type y) =>
            (x.FullName ?? x.Name) == (y.FullName ?? y.Name) && (x.Assembly?.FullName == y.Assembly?.FullName);

        public override int GetHashCode(Type type) => new
        {
            Name = type.FullName ?? type.Name,
            Assembly = type.Assembly?.FullName
        }.GetHashCode();
    }
}
