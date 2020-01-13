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

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is passed as a type parameter that has a new constraint.")]
    internal sealed class ArgumentComparer : ComparerBase<ArgumentComparer, Type>
    {
        //
        // Generikus argumentumnak nincs teljes neve ezert a lenti sor jol kezeli a fenti
        // problemat.
        //

        [SuppressMessage("Globalization", "CA1307: Specify StringComparison", Justification = "Type names are not localized.")]
        public override int GetHashCode(Type obj) => (obj.FullName ?? obj.Name).GetHashCode();
    }
}
