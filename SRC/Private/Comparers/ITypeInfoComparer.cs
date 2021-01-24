﻿/********************************************************************************
* ITypeInfoComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal sealed class ITypeInfoComparer : ComparerBase<ITypeInfoComparer, ITypeInfo>
    {
        public override int GetHashCode(ITypeInfo obj) => (obj.AssemblyQualifiedName ?? obj.Name).GetHashCode();
    }
}