/********************************************************************************
* IAssemblyInfoComparer.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    using Primitives;

    internal sealed class IAssemblyInfoComparer : ComparerBase<IAssemblyInfoComparer, IAssemblyInfo>
    {
        public override int GetHashCode(IAssemblyInfo obj) => obj.Name.ToString().GetHashCode();
    }
}
