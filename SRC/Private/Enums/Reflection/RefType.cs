/********************************************************************************
* RefType.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// If a particular type is a reference type this enum represents the pointer kind.
    /// </summary>
    /// <remarks>Reference types always have element type associated.</remarks>
    internal enum RefType 
    {
        /// <summary>
        /// The given type is not a reference type
        /// </summary>
        None = 0,
        
        /// <summary>
        /// The type is a <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct">ref struct</see>
        /// </summary>
        Ref, // Type.IsByRef has no counterpart in INamedTypeInfo (see: PassingByReference_ShouldNotAffectTheParameterType test) so this flag is applied on "ref struct"s only

        /// <summary>
        /// The type is a <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#pointer-types">pointer</see>
        /// </summary>
        Pointer,

        /// <summary>
        /// The type is an <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/arrays">array</see>
        /// </summary>
        Array
    }
}
