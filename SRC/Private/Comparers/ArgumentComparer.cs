/********************************************************************************
* ArgumentComparer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    //
    // Custom comparer required as (for instance) typeof(List<>).GetGenericArguments()[0] != typeof(IList<>).GetGenericArguments()[0]
    //

    internal sealed class ArgumentComparer : ComparerBase<ArgumentComparer, Type>
    {
        public override bool Equals(Type x, Type y)
        {
            string
                name1 = x.FullName ?? x.Name,
                name2 = y.FullName ?? y.Name;

            return name1.Equals(name2, StringComparison.OrdinalIgnoreCase);
        }

        //
        // Generic arguments don't have FullName
        //

        public override int GetHashCode(Type obj) => (obj.FullName ?? obj.Name).GetHashCode();
    }
}
