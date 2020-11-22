/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
        public static string GetFullName(this MemberInfo src) => $"{new SyntaxFactoryBase().CreateType(MetadataTypeInfo.CreateFrom(src.DeclaringType)).ToFullString()}.{src.Name}";

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

            throw new NotSupportedException();
        }

        //
        // Explicit implementacional a nev "Nevter.Interface.Tag" formaban van
        //

        private static readonly Regex FStripper = new Regex("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
