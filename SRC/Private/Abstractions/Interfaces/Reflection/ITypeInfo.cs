﻿/********************************************************************************
* ITypeInfo.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ITypeInfo: IHasName
    {
        IAssemblyInfo DeclaringAssembly { get; }
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

    public interface IGenericTypeInfo : ITypeInfo, IGeneric { }

    public interface IArrayTypeInfo : ITypeInfo
    {
        int Rank { get; }
    }
}