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
        IReadOnlyList<ITypeInfo> DeclaringInterfaces { get; }
        IParameterInfo ReturnValue { get; }
        bool IsSpecial { get; }
        bool IsFinal { get; }
        AccessModifiers AccessModifiers { get; }
    }

    internal interface IConstructorInfo : IMethodInfo { }

    internal interface IGenericMethodInfo : IMethodInfo, IGeneric<IGenericMethodInfo> { }
}
