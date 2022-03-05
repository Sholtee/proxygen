/********************************************************************************
* MetadataReferenceComparer.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataReferenceComparer : ComparerBase<MetadataReferenceComparer, MetadataReference>
    {
        public override int GetHashCode(MetadataReference obj) => new { Type = obj.GetType(), obj.Display }.GetHashCode();
    }
}
