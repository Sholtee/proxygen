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
        //Ref,  // FIXME: ezt nem kene kikommentelni de ugy tunik a Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban (lasd: PassingByReference_ShouldNotAffectTheParameterType test)
        Pointer,
        Array
    }
}
