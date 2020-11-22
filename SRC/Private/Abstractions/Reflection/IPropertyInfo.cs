/********************************************************************************
* IPropertyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IPropertyInfo: IMemberInfo, IHasType
    {
        IMethodInfo? GetMethod { get; }
        IMethodInfo? SetMethod { get; }
        IReadOnlyList<IParameterInfo> Indices { get; }
    }
}
