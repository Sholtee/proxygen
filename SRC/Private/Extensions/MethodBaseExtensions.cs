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
                // Ha a metodus privat akkor biztos nem interface metodus.
                //

                if (src.GetDeclaringType().IsInterface()) return AccessModifiers.Explicit;
#endif
                return AccessModifiers.Private;
            }

            throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER);
        }

        public static Type GetDeclaringType(this MethodBase src) 
        {
#if !NETSTANDARD1_6
            Type reflectedType = src.ReflectedType;

            foreach (Type iface in reflectedType.GetInterfaces())
            {
                InterfaceMapping mapping = reflectedType.GetInterfaceMap(iface);

                //
                // Ha a metodus resze egy interface implementacionak akkor ezt az interface-t
                // tekintjuk deklaralo tipusnak.
                //

                if (mapping.TargetMethods.Contains(src))
                    return iface;
            }
#endif
            return src.DeclaringType;
        }
    }
}
