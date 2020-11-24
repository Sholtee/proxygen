/********************************************************************************
* IGeneric.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IGeneric 
    {
        bool IsGenericDefinition { get; }
        IGeneric GenericDefinition { get; }
        IGeneric Close(params ITypeInfo[] genericArgs);
        IReadOnlyList<ITypeInfo> GenericArguments { get; }
    }
}
