/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Mono.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed record ExtendedMemberInfo(MethodInfo Method, MemberInfo Member);

    internal static class MemberInfoExtensions
    {

        private static readonly Regex
            //
            // The name of explicit implementation is in the form of "Namespace.Interface.Tag"
            //

            FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline),
            FGetPrefix = new("^(get|set|add|remove)_", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        //
        // Can't extract setters from expressions: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
        //

        public static ExtendedMemberInfo ExtractFrom(Delegate accessor, int callIndex = 0)
        {
            MethodInfo method = accessor
                .Method
                .GetInstructions()
                .ConvertAr
                (
                    convert: static instruction => (MethodInfo) instruction.Operand,
                    drop: static instruction => instruction.OpCode != OpCodes.Callvirt
                )[callIndex];

            return new ExtendedMemberInfo
            (
                method,
                ExtractFrom(method)
            );
        }

        public static MemberInfo ExtractFrom(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;

            BindingFlags bindingFlags = declaringType.IsInterface
                ? BindingFlags.Instance | BindingFlags.Public
                : BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

            MemberInfo? member = null;

            if (method.IsSpecialName)
            {
                Match prefix = FGetPrefix.Match(method.StrippedName());

                if (prefix.Success && prefix.Groups.Count > 1)
                {
                    member = prefix.Groups[1].Value.ToLower() switch
                    {
                        "get" or "set" => declaringType
                            .GetProperties(bindingFlags)
                            .Single(prop => prop.SetMethod == method || prop.GetMethod == method, throwOnEmpty: false)!,

                        "add" or "remove" => declaringType
                            .GetEvents(bindingFlags)
                            .Single(evt => evt.AddMethod == method || evt.RemoveMethod == method, throwOnEmpty: false)!,

                        _ => null
                    };
                }

                if (member is null)
                    Trace.TraceWarning($"Unsupported special method: {method}");
            }

            return member ?? method;
        }

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
