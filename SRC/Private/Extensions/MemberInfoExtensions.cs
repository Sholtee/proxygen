/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
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
    }
}
