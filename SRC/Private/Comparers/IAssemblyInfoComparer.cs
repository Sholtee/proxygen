﻿/********************************************************************************
* IAssemblyInfoComparer.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class IAssemblyInfoComparer : ComparerBase<IAssemblyInfoComparer, IAssemblyInfo>
    {
        public override bool Equals(IAssemblyInfo x, IAssemblyInfo y) => x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode(IAssemblyInfo obj) => obj.Name.GetHashCode
        (
#if !NETSTANDARD2_0
            StringComparison.OrdinalIgnoreCase
#endif
        );
    }
}
