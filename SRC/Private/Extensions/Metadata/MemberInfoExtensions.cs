/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        //
        // Can't extract setters from expressions: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
        //

        public static ExtendedMemberInfo ExtractFrom(Delegate accessor, int callIndex = 0)
        {
            MethodInfo method = accessor
                .Method
                .GetInstructions()
                .Where(static instruction => instruction.OpCode == OpCodes.Callvirt)
                .Select(static instruction => (MethodInfo) instruction.Operand)
                .ElementAt(callIndex);

            return new ExtendedMemberInfo
            (
                method,
                ExtractFrom(method)
            );
        }

        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<MethodInfo, MemberInfo>> FMethodMemberBindings = new();

        public static MemberInfo ExtractFrom(MethodInfo method)
        {
            if (method.IsSpecialName)
            {
                IReadOnlyDictionary<MethodInfo, MemberInfo> bindings = FMethodMemberBindings.GetOrAdd(method.DeclaringType, static t =>
                {
                    BindingFlags flags = t.IsInterface
                        ? BindingFlags.Instance | BindingFlags.Public
                        : BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

                    Dictionary<MethodInfo, MemberInfo> bindings = new();

                    foreach (MemberInfo member in t.GetMembers(flags))
                    {
                        switch (member)
                        {
                            case PropertyInfo prop:
                                if (prop.GetMethod is not null)
                                    bindings.Add(prop.GetMethod, member);
                                if (prop.SetMethod is not null)
                                    bindings.Add(prop.SetMethod, member);
                                break;
                            case EventInfo evt:
                                if (evt.AddMethod is not null)
                                    bindings.Add(evt.AddMethod, member);
                                if (evt.RemoveMethod is not null)
                                    bindings.Add(evt.RemoveMethod, member);
                                break;
                        }
                    }

                    return bindings;
                });

                if (bindings.TryGetValue(method, out MemberInfo member))
                    return member;

                Debug.Assert(false, $"Member could not be determined for method: {method}");
            }

            return method;
        }

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
