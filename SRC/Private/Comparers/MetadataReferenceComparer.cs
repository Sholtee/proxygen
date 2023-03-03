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
        public override bool Equals(MetadataReference x, MetadataReference y)
        {
            if (x.Display is null || y.Display is null)
                //
                // Cannot determine equality, treat them unequal
                //

                return false;

            return x.Display.Equals(y.Display, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(MetadataReference obj) => obj.Display?.GetHashCode() ?? 0;
    }
}
