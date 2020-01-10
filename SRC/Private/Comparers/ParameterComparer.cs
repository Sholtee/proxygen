/********************************************************************************
* ParameterComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class ParameterComparer : ComparerBase<ParameterComparer, ParameterInfo>
    {
        public override int GetHashCode(ParameterInfo obj) => new
        {
            //
            // Lasd ArgumentComparer
            //

            Name = obj.ParameterType.FullName ?? obj.ParameterType.Name,
            obj.Attributes // IN, OUT, stb

            //
            // Parameter neve nem erdekel bennunket (azonos tipussal es attributumokkal ket parametert
            // azonosnak veszunk).
            //

        }.GetHashCode();
    }
}
