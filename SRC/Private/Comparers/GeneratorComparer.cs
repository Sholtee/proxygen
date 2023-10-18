/********************************************************************************
* GeneratorComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class GeneratorComparer : ComparerBase<GeneratorComparer, Generator>
    {
        public override bool Equals(Generator x, Generator y) => x.Id == y.Id;

        public override int GetHashCode(Generator obj) => obj.Id.GetHashCode();
    }
}
