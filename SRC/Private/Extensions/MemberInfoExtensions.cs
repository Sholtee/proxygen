/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Mono.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class MemberInfoExtensions
    {
        public static string GetFullName(this MemberInfo src) => $"{ProxySyntaxFactoryBase.CreateType(src.DeclaringType).ToFullString()}.{src.Name}";

        public static bool IsStatic(this MemberInfo src) 
        {
            switch (src)
            {
                case MethodInfo method:
                    return method.IsStatic;
                case FieldInfo field:
                    return field.IsStatic;
                case PropertyInfo property:
                    return (property.GetMethod ?? property.SetMethod ?? throw new InvalidOperationException()).IsStatic;
                case EventInfo @event:
                    return (@event.AddMethod ?? @event.RemoveMethod ?? throw new InvalidOperationException()).IsStatic;
                default:
                    Debug.Fail("Unknown member type");
                    return false;
            }
        }

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
                default:
                    Debug.Fail("Unknown body");
                    return null!;
            }
        }

        public static MemberInfo ExtractFrom(Expression<Action> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Action<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T, object?>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom(MethodInfo method, MemberTypes memberType) // settert es esemenyt kifejezesekbol nem fejthetunk ki: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
        {
            Instruction? call = method.GetInstructions().SingleOrDefault(instruction => instruction.OpCode == OpCodes.Callvirt);

            if (call != null)
            {
                method = (MethodInfo) call.Operand;

                switch (memberType) 
                {
                    case MemberTypes.Property:
                        PropertyInfo? property = method.DeclaringType.GetProperties().SingleOrDefault(prop => prop.SetMethod == method || prop.GetMethod == method);
                        if (property != null) return property;
                        break;
                    case MemberTypes.Event:
                        EventInfo? evt = method.DeclaringType.GetEvents().SingleOrDefault(evt => evt.AddMethod == method || evt.RemoveMethod == method);
                        if (evt != null) return evt;
                        break;
                    case MemberTypes.Method:
                        return method;
                }
            }

            Debug.Fail("Member could not be determined");
            return null!;
        }

        //
        // Explicit implementacional a nev "Nevter.Interface.Tag" formaban van
        //

        private static readonly Regex FStripper = new Regex("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
