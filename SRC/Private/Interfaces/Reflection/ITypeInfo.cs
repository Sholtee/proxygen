/********************************************************************************
* ITypeInfo.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Represents an abstract type declaration
    /// </summary>
    internal interface ITypeInfo: IHasName
    {
        /// <summary>
        /// The assembly in which the type is declared. In case of generic types this property returns the ASM of the unbound generic type.
        /// </summary>
        IAssemblyInfo? DeclaringAssembly { get; }  // TODO: this should never be null

        /// <summary>
        /// The visibility of this type.
        /// </summary>
        AccessModifiers AccessModifiers { get; }

        /// <summary>
        /// The read-only boolean properties of this type.
        /// </summary>
        TypeInfoFlags Flags { get; }

        /// <summary>
        /// Returns whether this type is a reference type (such as <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct">ref struct</see>s or <see href="=https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#pointer-types">pointer</see>s)
        /// </summary>
        RefType RefType { get; }

        /// <summary>
        /// The <see cref="QualifiedName"/> of the type followed by the identity of the containing assembly. The result should not reflect the type arguments.
        /// </summary>
        string? AssemblyQualifiedName { get; }
        /// <summary>
        /// The name of the type including its enclosing types and namespace. The result should not reflect the type arguments.
        /// </summary>
        /// <remarks>The value returned by this property is suitable to be passed to <see cref="System.Type.GetType(string)"/> or <see cref="Microsoft.CodeAnalysis.Compilation.GetTypeByMetadataName(string)"/>.</remarks>
        string? QualifiedName { get; }
        /// <summary>
        /// Returns the underlying element type (for e.g.: <see cref="int"/> for <i>int*</i>). Yields non null for pointers, ref types and arrays.
        /// </summary>
        ITypeInfo? ElementType { get; }

        /// <summary>
        /// The member that contains this type. For generic types this member can be a method (for instance given T from "Foo&lt;T&gt;()", this property returns the Foo method). For nested types this member is the parent type.
        /// </summary>
        IHasName? ContainingMember { get; }

        /// <summary>
        /// For nested types this property returns the parent type
        /// </summary>
        ITypeInfo? EnclosingType { get; }

        /// <summary>
        /// The implemented interfaces.
        /// </summary>
        IReadOnlyList<ITypeInfo> Interfaces { get; }

        /// <summary>
        /// The base type (null for typeof(object))
        /// </summary>
        ITypeInfo? BaseType { get; }

        /// <summary>
        /// The properties (including private, static and inherited ones) declared on this type.
        /// </summary>
        IReadOnlyList<IPropertyInfo> Properties { get; }

        /// <summary>
        /// The events (including private, static and inherited ones) declared on this type.
        /// </summary>
        IReadOnlyList<IEventInfo> Events { get; }

        /// <summary>
        /// The methods (including private, static and inherited ones) declared on this type.
        /// </summary>
        IReadOnlyList<IMethodInfo> Methods { get; }

        /// <summary>
        /// The constructors declared on this type
        /// </summary>
        IReadOnlyList<IConstructorInfo> Constructors { get; }
    }

    /// <summary>
    /// Generic type info
    /// </summary>
    internal interface IGenericTypeInfo : ITypeInfo, IGeneric<IGenericTypeInfo> { }

    /// <summary>
    /// Array type info
    /// </summary>
    internal interface IArrayTypeInfo : ITypeInfo
    {
        /// <summary>
        /// The rank of the array. For instance this property returns 3 for <code>int[,,]</code>
        /// </summary>
        int Rank { get; }
    }
}
