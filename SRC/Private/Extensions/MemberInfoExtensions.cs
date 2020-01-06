/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class MemberInfoExtensions
    {
        public static string GetFullName(this MemberInfo src) => $"{ProxySyntaxGeneratorBase.CreateType(src.DeclaringType).ToFullString()}.{src.Name}";
    }
}
