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
        Ref, // A Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban (lasd: PassingByReference_ShouldNotAffectTheParameterType test) ezert csak pl "ref struct"-ra igaz
        Pointer,
        Array
    }
}
