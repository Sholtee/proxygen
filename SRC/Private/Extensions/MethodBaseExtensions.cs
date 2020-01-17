/********************************************************************************
* MethodBaseExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class MethodBaseExtensions
    {
        public static AccessModifiers GetAccessModifiers(this MethodBase src) 
        {          
            if (src.IsFamily) return AccessModifiers.Protected;
            if (src.IsAssembly) return AccessModifiers.Internal;
            if (src.IsFamilyOrAssembly) return AccessModifiers.Protected | AccessModifiers.Internal;
            if (src.IsPublic) return AccessModifiers.Public;
            if (src.IsPrivate)
            {

#if !NETSTANDARD1_6
                //
                // Nem kell MemberInfo-n definialni mert a tulajdonsagok ([Get|Set]Method) es 
                // esemenyek ([Add|Remove]Method) is visszavezethetok metodusokra.
                //

                Type declaringType = src.DeclaringType;

                //
                // Nem kell vizsgalni h a "declaringType" interface e mivel az interface-ek 
                // metodusai sose privatak.
                //

                foreach (Type iface in declaringType.GetInterfaces())
                {
                    InterfaceMapping mapping = declaringType.GetInterfaceMap(iface);
                    if (mapping.TargetMethods.Any(impl => impl == src)) return AccessModifiers.Explicit;
                }
#endif
                return AccessModifiers.Private;
            }

            throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER);
        }

        public static Type GetDeclaringType(this MethodBase src) 
        {
            Type declaringType = src.DeclaringType;
#if !NETSTANDARD1_6
            foreach (Type iface in declaringType.GetInterfaces())
            {
                InterfaceMapping mapping = declaringType.GetInterfaceMap(iface);

                //
                // Ha a metodust resze egy interface implementacionak akkor ezt az interface-t
                // tekintjuk deklaralo tipusnak.
                //

                if (mapping.TargetMethods.Contains(src))
                    return iface;
            }
#endif
            return declaringType;
        }
    }
}
