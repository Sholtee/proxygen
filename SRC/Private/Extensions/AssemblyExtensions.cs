/********************************************************************************
* AssemblyExtensions.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class AssemblyExtensions
    {
        public static IEnumerable<Assembly> GetReferences(this Assembly asm) => asm.GetReferencedAssemblies().Select(Assembly.Load);
    }
}
