/********************************************************************************
* IPropertyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IPropertyInfo: IMemberInfo, IHasType
    {
        IMethodInfo? GetMethod { get; }
        IMethodInfo? SetMethod { get; }
        IReadOnlyList<IParameterInfo> Indices { get; }
    }
}
