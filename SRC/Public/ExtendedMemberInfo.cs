/********************************************************************************
* ExtendedMemberInfo.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Member info containing the backing method
    /// </summary>
    /// <param name="Method">The method represented by the <see cref="Member"/></param>
    /// <param name="Member">The original member</param>
    public sealed record ExtendedMemberInfo(MethodInfo Method, MemberInfo Member);
}
