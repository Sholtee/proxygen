/********************************************************************************
* ArgumentComparer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    //
    // Sajat comparer azert kell mert pl List<T> es IList<T> eseten typeof(List<T>).GetGenericArguments[0] != typeof(IList<T>).GetGenericArguments[0] 
    // testzoleges "T"-re.
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
        // Generikus argumentumnak nincs teljes neve ezert a lenti sor jol kezeli a fenti
        // problemat.
        //

        [SuppressMessage("Globalization", "CA1307: Specify StringComparison", Justification = "Type names are not localized.")]
        public override int GetHashCode(Type obj) => (obj.FullName ?? obj.Name).GetHashCode();
    }
}
