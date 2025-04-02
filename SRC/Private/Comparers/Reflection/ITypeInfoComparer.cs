/********************************************************************************
* ITypeInfoComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class ITypeInfoComparer : ComparerBase<ITypeInfoComparer, ITypeInfo>
    {
        public override bool Equals(ITypeInfo x, ITypeInfo y)
        {
            string
                name1 = x.AssemblyQualifiedName ?? x.Name,
                name2 = y.AssemblyQualifiedName ?? y.Name;

            return name1.Equals(name2, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(ITypeInfo obj) => (obj.AssemblyQualifiedName ?? obj.Name).GetHashCode
        (
#if !NETSTANDARD2_0
            StringComparison.OrdinalIgnoreCase
#endif
        );
    }
}
