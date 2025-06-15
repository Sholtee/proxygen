/********************************************************************************
* IEventInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="IEventInfo"/> interface.
    /// </summary>
    internal static class IEventInfoExtensions
    {
        /// <summary>
        /// Sorts the given event list using the event names.
        /// </summary>
        public static IEnumerable<IEventInfo> Sort(this IEnumerable<IEventInfo> self) => self
            //
            // Events always have add & remove methods assigned
            //

            .OrderBy(static e => $"{e.Name}_{e.AddMethod.GetMD5HashCode()}");
    }
}
