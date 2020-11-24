/********************************************************************************
* IMethodInfo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IMethodInfo: IMemberInfo
    {
        IReadOnlyList<IParameterInfo> Parameters { get; }
        IParameterInfo ReturnValue { get; }
        bool IsSpecial { get; }
        AccessModifiers AccessModifiers { get; }
    }

    public interface IConstructorInfo : IMethodInfo { }

    public interface IGenericMethodInfo : IMethodInfo, IGeneric { }
}
