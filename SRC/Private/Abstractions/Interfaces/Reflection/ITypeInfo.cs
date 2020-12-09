/********************************************************************************
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
        ITypeInfo? ElementType { get; }
        IReadOnlyList<ITypeInfo> EnclosingTypes { get; }
        IReadOnlyList<ITypeInfo> Interfaces { get; }
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
