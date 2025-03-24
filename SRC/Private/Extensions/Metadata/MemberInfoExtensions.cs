/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Mono.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class MemberInfoExtensions
    {
        private static readonly Regex
            //
            // The name of explicit implementation is in the form of "Namespace.Interface.Tag"
            //

            FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        //
        // Can't extract setters from expressions: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
        //

        public static ExtendedMemberInfo ExtractFrom(Delegate accessor, int callIndex)
        {
            MethodInfo method = accessor
                .Method
                .GetInstructions()
                .Where(static instruction => instruction.OpCode == OpCodes.Callvirt)
                .Select(static instruction => (MethodInfo) instruction.Operand)
                .ElementAt(callIndex);

            return new ExtendedMemberInfo(method);
        }

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
