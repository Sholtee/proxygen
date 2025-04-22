/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.Proxy.Internals
{
    internal static class MemberInfoExtensions
    {
        private static readonly Regex
            //
            // The name of explicit implementation is in the form of "Namespace.Interface.Tag"
            //

            FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
