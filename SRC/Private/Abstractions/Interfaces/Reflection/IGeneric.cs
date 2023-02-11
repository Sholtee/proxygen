/********************************************************************************
* IGeneric.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IGeneric<TDescendant> where TDescendant: IGeneric<TDescendant>
    {
        /// <summary>
        /// Returns true for unbound generics false otherwise.
        /// </summary>
        bool IsGenericDefinition { get; }
        /// <summary>
        /// Gets the unbound definition.
        /// </summary>
        TDescendant GenericDefinition { get; }
        /// <summary>
        /// Substitutes the type arguments with the values provided by the <paramref name="genericArgs"/> parameter.
        /// </summary>
        TDescendant Close(params ITypeInfo[] genericArgs);
        /// <summary>
        /// Returns the explicitly declared generic arguments (e.g.: in case of <i>Generic{T}.Nested{TT}</i> this property returns <i>TT</i> for the nested type).
        /// </summary>
        IReadOnlyList<ITypeInfo> GenericArguments { get; }
        /// <summary>
        /// A list of <see cref="IGenericConstraint"/> and <see cref="ITypeInfo"/> objects.
        /// </summary>
        IReadOnlyDictionary<ITypeInfo, IReadOnlyList<object>> GenericConstraints { get; }
    }
}
