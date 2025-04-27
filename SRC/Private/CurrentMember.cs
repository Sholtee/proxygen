/********************************************************************************
* CurrentMember.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    /// <summary>
    /// Contains helpers related to the currently executing member (method, property or event accessor). Intended for private use only.
    /// </summary>
    public static class CurrentMember  // this class is referenced by the generated proxies so it must be public
    {
        /// <summary>
        /// Gets the base definition of the currently executing member.
        /// </summary>
        /// <returns>Returns false if the <paramref name="memberInfo"/> is not null</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool GetBase(ref ExtendedMemberInfo memberInfo)
        {
            if (memberInfo is not null)
                return false;

            //
            // Get the calling method
            //

            MethodInfo callingMethod = (MethodInfo) new StackTrace().GetFrame(1).GetMethod();

            memberInfo = new ExtendedMemberInfo
            (
                callingMethod.GetOverriddenMethod() ?? throw new InvalidOperationException(Resources.NOT_VIRTUAL)
            );

            return true;
        }

        /// <summary>
        /// Gets the interface member that is implemented by the currently executing method.
        /// </summary>
        /// <returns>Returns false if the <paramref name="memberInfo"/> is not null</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool GetImplementedInterfaceMethod(ref ExtendedMemberInfo memberInfo)
        {
            if (memberInfo is not null)
                return false;

            //
            // Get the calling method
            //

            MethodInfo callingMethod = (MethodInfo) new StackTrace().GetFrame(1).GetMethod();

            MethodInfo[] implementedInterfaceMethods = [.. callingMethod.GetImplementedInterfaceMethods()];
            if (implementedInterfaceMethods.Length is not 1)
                throw new InvalidOperationException(string.Format(Resources.AMBIGUOUS_MATCH, callingMethod));

            memberInfo = new ExtendedMemberInfo(implementedInterfaceMethods[0]);
            return true;
        }
    }
}
