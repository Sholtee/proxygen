/********************************************************************************
* ITypeInfo.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal interface ITypeInfo: IHasName
    {
        IAssemblyInfo? DeclaringAssembly { get; }
        bool IsVoid { get; }
        RefType RefType { get; }
        /// <summary>
        /// Returns true if the type is nested and not a generic parameter.
        /// </summary>
        bool IsNested { get; }
        /// <summary>
        /// The type represents a generic parameter (for e.g.: "T" in <see cref="List{T}"/>).
        /// </summary>
        bool IsGenericParameter { get; }
        bool IsInterface { get; }
        bool IsClass { get; }
        bool IsFinal { get; }
        bool IsAbstract { get; }
        /// <summary>
        /// The <see cref="FullName"/> of the type followed by the identity of the containing assembly. The result should not reflect the type arguments.
        /// </summary>
        string? AssemblyQualifiedName { get; }
        /// <summary>
        /// The name of the type including its enclosing types and namespace. The result should not reflect the type arguments.
        /// </summary>
        string? FullName { get; }
        /// <summary>
        /// Returns the underlying element type (for e.g.: <see cref="int"/> for <i>int*</i>). Yields non null for pointers, ref types and arrays.
        /// </summary>
        ITypeInfo? ElementType { get; }
        IHasName? ContainingMember { get; }
        /// <summary>
        /// Returns the declaring types starting with the closest one. In case of non nested types this property returns an empty list.
        /// </summary>
        IReadOnlyList<ITypeInfo> EnclosingTypes { get; }
        IReadOnlyList<ITypeInfo> Interfaces { get; }
        /// <summary>
        /// Returns the base types starting with the closest one.
        /// </summary>
        IReadOnlyList<ITypeInfo> Bases { get; }
        IReadOnlyList<IPropertyInfo> Properties { get; }
        IReadOnlyList<IEventInfo> Events { get; }
        IReadOnlyList<IMethodInfo> Methods { get; }
        IReadOnlyList<IConstructorInfo> Constructors { get; }
    }

    internal interface IGenericTypeInfo : ITypeInfo, IGeneric { }

    internal interface IArrayTypeInfo : ITypeInfo
    {
        int Rank { get; }
    }
}
