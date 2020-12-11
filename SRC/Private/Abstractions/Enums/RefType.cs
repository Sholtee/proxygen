/********************************************************************************
* RefType.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum RefType 
    {
        None = 0,
        //Ref,  // FIXME: ezt nem kene kikommentelni de ugy tunik a Type.IsByRef-nek nincs megfeleloje az INamedTypeInfo-ban (lasd: PassingByReference_ShouldNotAffectTheParameterType test)
        Pointer
    }
}
