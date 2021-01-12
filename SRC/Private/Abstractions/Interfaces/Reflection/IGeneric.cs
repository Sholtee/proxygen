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
        /// <summary>
        /// Returns true for unbound generics false otherwise.
        /// </summary>
        bool IsGenericDefinition { get; }
        /// <summary>
        /// Gets the unbound definition.
        /// </summary>
        IGeneric GenericDefinition { get; }
        /// <summary>
        /// Substitutes the type arguments with the values provided by the <paramref name="genericArgs"/> parameter.
        /// </summary>
        IGeneric Close(params ITypeInfo[] genericArgs);
        /// <summary>
        /// Returns the explicitly declared generic arguments (e.g.: in case of <i>Generic{T}.Nested{TT}</i> this property returns <i>TT</i> for the nested type).
        /// </summary>
        IReadOnlyList<ITypeInfo> GenericArguments { get; }
    }
}
