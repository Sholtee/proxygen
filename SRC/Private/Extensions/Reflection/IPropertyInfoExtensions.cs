/********************************************************************************
* IPropertyInfoExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="IPropertyInfo"/> interface.
    /// </summary>
    internal static class IPropertyInfoExtensions
    {
        /// <summary>
        /// Sorts the given property list by name and signature.
        /// </summary>
        public static IEnumerable<IPropertyInfo> Sort(this IEnumerable<IPropertyInfo> self) => self.OrderBy(static p =>
        {
            List<IMethodInfo> accessors = [];
            if (p.SetMethod is not null)
                accessors.Add(p.SetMethod);
            if (p.GetMethod is not null)
                accessors.Add(p.GetMethod);

            return $"{p.Name}_{accessors.GetMD5HashCode()}";
        });
    }
}
