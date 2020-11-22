/********************************************************************************
* IMethodInfo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IMethodInfo: IMemberInfo
    {
        IReadOnlyList<IParameterInfo> Parameters { get; }
        IParameterInfo ReturnValue { get; }
        bool IsSpecial { get; }
        AccessModifiers AccessModifiers { get; }
    }

    internal interface IConstructorInfo : IMethodInfo { }

    internal interface IGenericMethodInfo : IMethodInfo, IGeneric { }
}
