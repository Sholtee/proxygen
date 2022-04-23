﻿/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Mono.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed record ExtendedMemberInfo(MethodInfo Method, MemberInfo Member);

    internal static class MemberInfoExtensions
    {
        private static MemberInfo DoExtractFrom(LambdaExpression expr) 
        {
            Expression body = expr.Body;

            start:
            switch(body)
            {
                case UnaryExpression convert:
                    body = convert.Operand;
                    goto start;
                case MemberExpression member: // Event, Field, Prop
                    return member.Member;
                case MethodCallExpression call:
                    return call.Method;
                case BinaryExpression binary:
                    body = binary.Left;
                    goto start;
                default: throw new NotSupportedException();
            }
        }

        public static MemberInfo ExtractFrom(Expression<Action> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Action<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T, object?>> expr) => DoExtractFrom(expr);

        //
        // settert es esemenyt kifejezesekbol nem fejthetunk ki: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
        //

        private static readonly ConcurrentDictionary<Delegate, ExtendedMemberInfo> FMemberInfoCache = new();

        public static ExtendedMemberInfo ExtractFrom(Delegate accessor) => FMemberInfoCache.GetOrAdd(accessor, static accessor =>
        {
            Debug.Assert(accessor.Target is null);

            MethodInfo method = (MethodInfo) accessor
                .Method
                .GetInstructions()
                .Single(instruction => instruction.OpCode == OpCodes.Callvirt)!
                .Operand;

            return new ExtendedMemberInfo
            (
                method,   
                    method
                        .DeclaringType
                        .GetProperties()
                        .Single(prop => prop.SetMethod == method || prop.GetMethod == method, throwOnEmpty: false) ??

                    method
                        .DeclaringType
                        .GetEvents()
                        .Single(evt => evt.AddMethod == method || evt.RemoveMethod == method, throwOnEmpty: false) ??

                    (MemberInfo) method
            );
        });

        //
        // Explicit implementacional a nev "Nevter.Interface.Tag" formaban van
        //

        private static readonly Regex FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
