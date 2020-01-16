/********************************************************************************
* MethodBaseExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class MethodBaseExtensions
    {
        public static AccessModifiers GetAccessModifiers(this MethodBase src) 
        {
            if (src.IsPrivate) return AccessModifiers.Private;
            if (src.IsFamily) return AccessModifiers.Protected;
            if (src.IsAssembly) return AccessModifiers.Internal;
            if (src.IsFamilyOrAssembly) return AccessModifiers.Protected | AccessModifiers.Internal;
            if (src.IsPublic) return AccessModifiers.Public;

            throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER);
        }
    }
}
