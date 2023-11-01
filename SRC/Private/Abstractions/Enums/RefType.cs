/********************************************************************************
* RefType.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Internals
{
    internal enum RefType 
    {
        None = 0,
        Ref, // Type.IsByRef has no counterpart in INamedTypeInfo (see: PassingByReference_ShouldNotAffectTheParameterType test) so this flag is applied on "ref struct"s only
        Pointer,
        Array
    }
}
