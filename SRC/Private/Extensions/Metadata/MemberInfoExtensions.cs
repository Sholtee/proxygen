/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Defines helper methods for the <see cref="MemberInfo"/> class
    /// </summary>
    internal static class MemberInfoExtensions
    {
        private static readonly Regex
            //
            // The name of explicit implementation is in the form of "Namespace.Interface.Member"
            //

            FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Removes any unnecessary parts from the member name when the member belongs to an explicit implementation.
        /// </summary>
        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
