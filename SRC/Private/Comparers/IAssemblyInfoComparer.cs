/********************************************************************************
* IAssemblyInfoComparer.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class IAssemblyInfoComparer : ComparerBase<IAssemblyInfoComparer, IAssemblyInfo>
    {
        public override int GetHashCode(IAssemblyInfo obj) => obj.Name.GetHashCode
        (
#if !NETSTANDARD2_0
            StringComparison.OrdinalIgnoreCase
#endif
        );
    }
}
