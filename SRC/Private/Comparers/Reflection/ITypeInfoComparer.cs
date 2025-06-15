/********************************************************************************
* ITypeInfoComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Determines the equality of <see cref="ITypeInfo"/> instances by comparing their qualified names
    /// </summary>
    internal sealed class ITypeInfoComparer : ComparerBase<ITypeInfoComparer, ITypeInfo>
    {
        /// <inheritdoc/>
        public override bool Equals(ITypeInfo x, ITypeInfo y)
        {
            string
                name1 = x.AssemblyQualifiedName ?? x.Name,
                name2 = y.AssemblyQualifiedName ?? y.Name;

            return name1.Equals(name2, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override int GetHashCode(ITypeInfo obj) => (obj.AssemblyQualifiedName ?? obj.Name).GetHashCode
        (
#if NETSTANDARD2_1_OR_GREATER
            StringComparison.OrdinalIgnoreCase
#endif
        );
    }
}
