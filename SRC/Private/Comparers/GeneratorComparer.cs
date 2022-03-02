/********************************************************************************
* GeneratorComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class GeneratorComparer : ComparerBase<GeneratorComparer, Generator>
    {
        public override int GetHashCode(Generator obj) => obj.GetHashCode();
    }
}
