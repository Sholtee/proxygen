/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class PropertyInfoExtensions
    {
        public static bool IsIndexer(this PropertyInfo src) => src.GetIndexParameters().Any();
    }
}
