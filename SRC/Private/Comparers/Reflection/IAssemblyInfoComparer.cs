/********************************************************************************
* IAssemblyInfoComparer.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Determines the equality of <see cref="IAssemblyInfo"/> instances by comparing their names.
    /// </summary>
    internal sealed class IAssemblyInfoComparer : ComparerBase<IAssemblyInfoComparer, IAssemblyInfo>
    {
        /// <inheritdoc/>
        public override bool Equals(IAssemblyInfo x, IAssemblyInfo y) => x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override int GetHashCode(IAssemblyInfo obj) => obj.Name.GetHashCode
        (
#if NETSTANDARD2_1_OR_GREATER
            StringComparison.OrdinalIgnoreCase
#endif
        );
    }
}
