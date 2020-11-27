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
        bool IsByRef { get; }
        bool IsNested { get; }
        bool IsGenericParameter { get; }
        bool IsInterface { get; }
        string? AssemblyQualifiedName { get; }
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
