/********************************************************************************
* MethodBaseExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class MethodBaseExtensions
    {
        public static AccessModifiers GetAccessModifiers(this MethodBase src) => src switch
        {
            _ when src.IsFamily => AccessModifiers.Protected,
            _ when src.IsAssembly => AccessModifiers.Internal,
            _ when src.IsFamilyOrAssembly => AccessModifiers.Protected | AccessModifiers.Internal,
            _ when src.IsFamilyAndAssembly => AccessModifiers.Protected | AccessModifiers.Private,
            _ when src.IsPublic => AccessModifiers.Public,
            _ when src.IsPrivate && src.GetImplementedInterfaceMethods().Any() => AccessModifiers.Explicit,
            _ when src.IsPrivate => AccessModifiers.Private,
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        public static IEnumerable<Type> GetDeclaringInterfaces(this MethodBase src) => src.ReflectedType.IsInterface
            ? Array.Empty<Type>()
            : src
                .GetImplementedInterfaceMethods()
                .Select(m => m.ReflectedType);

        public static IEnumerable<MethodBase> GetImplementedInterfaceMethods(this MethodBase src)
        {
            Type reflectedType = src.ReflectedType;

            foreach (Type iface in reflectedType.GetInterfaces())
            {
                //
                // https://docs.microsoft.com/en-us/dotnet/api/system.type.getinterfacemap?view=netcore-3.1#exceptions
                //

                if (iface.IsGenericType && reflectedType.IsArray)
                    continue;

                InterfaceMapping mapping = reflectedType.GetInterfaceMap(iface);

                int? mapIndex = mapping
                    .TargetMethods
                    .IndexOf(src);

                if (mapIndex >= 0) 
                    yield return mapping.InterfaceMethods[mapIndex.Value];
            }
        }

        public static bool SignatureEquals(this MethodInfo src, MethodInfo that, bool ignoreVisibility = false)
        {
            //
            // T ClassA<T>.Foo() != T ClassB<TT, T>.Foo()
            //

            if (src.DeclaringType.GetGenericArguments().Length != that.DeclaringType.GetGenericArguments().Length)
                return false;

            if (!GetMethodBasicAttributes(src).Equals(GetMethodBasicAttributes(that)))
                return false;

            if (!src.ReturnType.EqualsTo(that.ReturnType))
                return false;

            IReadOnlyList<ParameterInfo>
                paramsA = src.GetParameters(),
                paramsB = that.GetParameters();

            if (paramsA.Count != paramsB.Count)
                return false;

            for (int i = 0; i < paramsA.Count; i++)
                if (!paramsA[i].EqualsTo(paramsB[i]))
                    return false;

            return true;

            object GetMethodBasicAttributes(MethodInfo m) => new
            {
                m.Name,
                m.IsStatic,
                m.IsSpecialName,
                Arity = m.GetGenericArguments().Length,
                Accessibility = !ignoreVisibility
                    ? m.GetAccessModifiers()
                    : (AccessModifiers?) null
            };
        }
    }
}
