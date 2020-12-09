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
        /// <summary>
        /// <paramref name="genericArgs"/> should contain values for explicitly declared type arguments only.
        /// </summary>
        IGeneric Close(params ITypeInfo[] genericArgs);
        /// <summary>
        /// This property should contain the explicitly declared arguments only.
        /// </summary>
        IReadOnlyList<ITypeInfo> GenericArguments { get; }
    }
}
