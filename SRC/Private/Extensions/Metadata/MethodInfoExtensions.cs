/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class MethodInfoExtensions
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
            _ => throw new InvalidOperationException(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        public static IEnumerable<Type> GetDeclaringInterfaces(this MethodBase src) => src.ReflectedType.IsInterface
            ? Array.Empty<Type>()
            : src
                .GetImplementedInterfaceMethods()
                .Select(static m => m.ReflectedType);

        public static IEnumerable<MethodBase> GetImplementedInterfaceMethods(this MethodBase src)
        {
            //
            // As of C# 11 interfaces may have static abstract methods... We don't deal with
            // the implementors.
            //

            if (src.IsStatic || src is not MethodInfo method /*ctor*/)
                yield break;

            Type reflectedType = src.ReflectedType;
            if (reflectedType.IsInterface)
                yield break;

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
                    .IndexOf(method);

                if (mapIndex >= 0) 
                    yield return mapping.InterfaceMethods[mapIndex.Value];
            }
        }

        public static MethodInfo? GetOverriddenMethod(this MethodInfo method)
        {
            /*
            if (method.IsVirtual)
            {
                MethodInfo overriddenMethod = method.GetBaseDefinition();
                return overriddenMethod != method
                    ? overriddenMethod
                    : null;
            }
            */

            if (method.IsGenericMethod)
                method = method.GetGenericMethodDefinition();

            //
            // GetBaseDefinition() won't work for "new" override as well as it always return the declaring
            // method instead of the overridden one.
            //

            Type[] paramz = method
                .GetParameters()
                .Select(static p => p.ParameterType)
                .ToArray();

            foreach (Type baseType in method.DeclaringType.GetBaseTypes())
            {
                //
                // baseType.GetMethod(method.Name, types: paramz) won't for for generic methods
                //

                foreach(MethodInfo baseMethod in  baseType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance)))
                {
                    if (baseMethod.Name != method.Name)
                        continue;

                    if (baseMethod.IsGenericMethod)
                    {
                        if (!method.IsGenericMethod || baseMethod.GetGenericArguments().Length != method.GetGenericArguments().Length)
                            continue;

                        //
                        // We don't need to compare the generic parameters
                        //
                    }

                    ParameterInfo[] baseParamz = baseMethod.GetParameters();
                    if (baseParamz.Length != paramz.Length)
                        continue;

                    for (int i = 0; i < baseParamz.Length; i++)
                    {
                        Type
                            baseParam = baseParamz[i].ParameterType,
                            param = paramz[i];

                        if (param.IsGenericParameter ? param.GetGenericParameterIndex() != baseParam.GetGenericParameterIndex() : param != baseParam)
                            goto next;
                    }

                    return baseMethod;
                    next:;
                }
            }
            return null;
        }

        //
        // Similar logic is provided by Solti.Utils.Primitives, too. But we don't want to ship
        // that library with our source generator, so reimplement it.
        //

        public static MethodInfo ExtractFrom<T>(Expression<Action<T>> expression) => ((MethodCallExpression) expression.Body).Method;

        public static bool IsVirtual(this MethodBase method) =>
            method.IsVirtual && !method.IsFinal && !method.ReflectedType.IsInterface;
    }
}
