/********************************************************************************
* MetadataReferenceComparer.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Determines the equality of <see cref="MetadataReference"/> instances by comparing their locations
    /// </summary>
    internal sealed class MetadataReferenceComparer : ComparerBase<MetadataReferenceComparer, MetadataReference>
    {
        /// <inheritdoc/>
        public override bool Equals(MetadataReference x, MetadataReference y) =>
            x.Display is not null &&
            y.Display is not null &&
            x.Display.Equals(y.Display, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override int GetHashCode(MetadataReference obj) => obj.Display?.GetHashCode() ?? 0;
    }
}
