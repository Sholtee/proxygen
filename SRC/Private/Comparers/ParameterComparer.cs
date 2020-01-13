/********************************************************************************
* ParameterComparer.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is passed as a type parameter that has a new constraint.")]
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
