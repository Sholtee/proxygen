/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class MemberInfoExtensions
    {
        public static string GetFullName(this MemberInfo src) => $"{ProxySyntaxGeneratorBase.CreateType(src.DeclaringType).ToFullString()}.{src.Name}";

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
                default:
                    Debug.Fail("Unknown body");
                    return null;
            }
        }

        public static MemberInfo ExtractFrom(Expression<Action> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Action<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T, object>> expr) => DoExtractFrom(expr);
    }
}
