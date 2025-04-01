/********************************************************************************
* IEventInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IEventInfoExtensions
    {
        public static IEnumerable<IEventInfo> Sort(this IEnumerable<IEventInfo> self) => self
            //
            // Events always have add & remove methods assigned
            //

            .OrderBy(static e => $"{e.Name}_{e.AddMethod.GetMD5HashCode()}");
    }
}
