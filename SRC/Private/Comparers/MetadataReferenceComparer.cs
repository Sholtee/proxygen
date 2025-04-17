/********************************************************************************
* MetadataReferenceComparer.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataReferenceComparer : ComparerBase<MetadataReferenceComparer, MetadataReference>
    {
        public override bool Equals(MetadataReference x, MetadataReference y) =>
            x.Display is not null &&
            y.Display is not null &&
            x.Display.Equals(y.Display, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode(MetadataReference obj) => obj.Display?.GetHashCode() ?? 0;
    }
}
