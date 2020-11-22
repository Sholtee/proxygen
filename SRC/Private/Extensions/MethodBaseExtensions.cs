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
        public static AccessModifiers GetAccessModifiers(this MethodBase src) => src switch
        {
            _ when src.IsFamily => AccessModifiers.Protected,
            _ when src.IsAssembly => AccessModifiers.Internal,
            _ when src.IsFamilyOrAssembly => AccessModifiers.Protected | AccessModifiers.Internal,
            _ when src.IsPublic => AccessModifiers.Public,
            _ when src.IsPrivate && src.GetDeclaringType().IsInterface => AccessModifiers.Explicit,
            _ when src.IsPrivate => AccessModifiers.Private,
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        public static Type GetDeclaringType(this MethodBase src) 
        {
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

            return src.DeclaringType;
        }
    }
}
