/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            { IsFamily: true } => AccessModifiers.Protected,
            { IsAssembly: true } => AccessModifiers.Internal,
            { IsFamilyOrAssembly: true } => AccessModifiers.Protected | AccessModifiers.Internal,
            { IsFamilyAndAssembly: true } => AccessModifiers.Protected | AccessModifiers.Private,
            { IsPublic: true } => AccessModifiers.Public,
            { IsPrivate: true } when src.GetImplementedInterfaceMethods().Any() => AccessModifiers.Explicit,
            { IsPrivate: true} => AccessModifiers.Private,
            _ => throw new InvalidOperationException(Resources.UNDETERMINED_ACCESS_MODIFIER)
        };

        public static IEnumerable<Type> GetDeclaringInterfaces(this MethodBase src) => src.ReflectedType.IsInterface
            ? Array.Empty<Type>()
            : src
                .GetImplementedInterfaceMethods()
                .Select(static m => m.ReflectedType);

        public static IEnumerable<MethodInfo> GetImplementedInterfaceMethods(this MethodBase src)
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

                int mapIndex = mapping
                    .TargetMethods
                    .IndexOf(method);

                if (mapIndex >= 0) 
                    yield return mapping.InterfaceMethods[mapIndex];
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

            Debug.Assert(!method.IsGenericMethod || method.IsGenericMethodDefinition, "The original method cannot be closed generic");

            //
            // GetBaseDefinition() won't work for "new" override as well as it always return the declaring
            // method instead of the overridden one.
            //

            Type[] paramz = [..method.GetParameters().Select(static p => p.ParameterType)];

            foreach (Type baseType in method.ReflectedType.GetBaseTypes())
            {
                //
                // baseType.GetMethod(method.Name, types: paramz) won't work for generic methods
                //

                foreach(MethodInfo baseMethod in  baseType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance)))
                {
                    if (baseMethod.Name != method.Name)
                        continue;

                    if (baseMethod.IsGenericMethod)
                    {
                        Debug.Assert(baseMethod.IsGenericMethodDefinition, "The inspected method cannot be closed generic");

                        //
                        // We don't need to compare the generic parameters, just check the arity
                        //

                        if (!method.IsGenericMethod || baseMethod.GetGenericArguments().Length != method.GetGenericArguments().Length)
                            continue;
                    }

                    if (baseMethod.GetParameters().Select(static p => p.ParameterType).SequenceEqual(paramz, TypeComparer.Instance))
                        return baseMethod;
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
