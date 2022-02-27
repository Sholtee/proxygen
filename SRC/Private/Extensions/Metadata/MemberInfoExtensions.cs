/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

using Mono.Reflection;

namespace Solti.Utils.Proxy.Internals
{
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

        public static MemberInfo ExtractFrom(MethodInfo accessor, MemberTypes memberType, out MethodInfo method) // settert es esemenyt kifejezesekbol nem fejthetunk ki: https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0832
        {
            foreach (Instruction instruction in accessor.GetInstructions())
            {
                if (instruction.OpCode == OpCodes.Callvirt)
                {
                    method = (MethodInfo) instruction.Operand;

                    switch (memberType)
                    {
                        case MemberTypes.Property:
                            foreach (PropertyInfo prop in method.DeclaringType.GetProperties()) // nem hasznalhatunk SingleOrDefault()-ot mert lambdaban nem hivatkozhatunk by ref parametert [CS1628]
                            {
                                if (prop.SetMethod == method || prop.GetMethod == method)
                                    return prop;
                            }
                            break;
                        case MemberTypes.Event:
                            foreach (EventInfo evt in method.DeclaringType.GetEvents())
                            {
                                if (evt.AddMethod == method || evt.RemoveMethod == method)
                                    return evt;
                            }
                            break;
                        case MemberTypes.Method:
                            return method;
                    }
                }
            }

            throw new NotSupportedException();
        }

        //
        // Explicit implementacional a nev "Nevter.Interface.Tag" formaban van
        //

        private static readonly Regex FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
