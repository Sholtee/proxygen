/********************************************************************************
* IGeneric.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IGeneric 
    {
        bool IsGenericDefinition { get; }
        IGeneric GenericDefinition { get; }
        IReadOnlyList<ITypeInfo> GenericArguments { get; }
    }
}
