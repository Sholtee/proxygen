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
using System.Text.RegularExpressions;

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
                default:
                    Debug.Fail("Unknown body");
                    return null;
            }
        }

        public static MemberInfo ExtractFrom(Expression<Action> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Action<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T>> expr) => DoExtractFrom(expr);

        public static MemberInfo ExtractFrom<T>(Expression<Func<T, object>> expr) => DoExtractFrom(expr);

        public static bool SignatureEquals(this MemberInfo self, MemberInfo that) 
        {
            if (ReferenceEquals(self, that)) // igaz ha mindketto NULL  
                return true;

            if (self == null || that == null) 
                return false;

            if (self.StrippedName() != that.StrippedName()) 
                return false;

            if (self is MethodInfo methodA && that is MethodInfo methodB)
            {
                methodA = EnsureNotSpecialized(methodA);
                methodB = EnsureNotSpecialized(methodB);

                return
                    methodA.GetGenericArguments().SequenceEqual(methodB.GetGenericArguments(), ArgumentComparer.Instance) &&
                    methodA.GetParameters().SequenceEqual(methodB.GetParameters(), ParameterComparer.Instance) &&
                    ArgumentComparer.Instance.Equals(methodA.ReturnType, methodB.ReturnType); // visszateres lehet generikus => ArgumentComparer
            }

            if (self is PropertyInfo propA && that is PropertyInfo propB)
                return
                    propA.PropertyType == propB.PropertyType &&
                    propA.CanWrite == propB.CanWrite         &&
                    propA.CanRead == propB.CanRead           &&

                    //
                    // Indexer property-knel meg kell egyezniuk az index parameterek
                    // sorrendjenek es tipusanak.
                    //

                    propA
                        .GetIndexParameters()
                        .Select(p => p.ParameterType)
                        .SequenceEqual
                        (
                            propB.GetIndexParameters().Select(p => p.ParameterType)
                        );

            if (self is EventInfo evtA && that is EventInfo evtB)
                return evtA.EventHandlerType == evtB.EventHandlerType;

            return false;
        }

        private static MethodInfo EnsureNotSpecialized(MethodInfo method) => method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

        //
        // Explicit implementacional a nev "Nevter.Interface.Tag" formaban van
        //

        private static readonly Regex FStripper = new Regex("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this MemberInfo self) => FStripper.Match(self.Name).Value;
    }
}
