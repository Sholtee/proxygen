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
            #pragma warning disable CA2201 // In theory we should never reach here.
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
            #pragma warning restore CA2201
        };

        public static IEnumerable<Type> GetDeclaringInterfaces(this MethodBase src) => src.ReflectedType.IsInterface
            ? Array.Empty<Type>()
            : src
                .GetImplementedInterfaceMethods()
                .Select(static m => m.ReflectedType);

        public static IEnumerable<MethodBase> GetImplementedInterfaceMethods(this MethodBase src)
        {
            if (src is not MethodInfo method)
                yield break; // ctor

            Type reflectedType = src.ReflectedType;
            if (reflectedType.IsInterface)
                yield break;

            //
            // As of C# 11 interfaces may have static abstract methods... We don't deal with
            // the implementors.
            //

            if (src.IsStatic)
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

            //
            // GetBaseDefinition() won't work for "new" override.
            //

            Type[] paramz = method
                .GetParameters()
                .Select(static p => p.ParameterType)
                .ToArray();

            foreach (Type baseType in method.ReflectedType.GetBaseTypes())
            {
                MethodInfo? overriddenMethod = baseType.GetMethod
                (
                    method.Name,
                    bindingAttr:
                        BindingFlags.DeclaredOnly |
                        (method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic) |
                        (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance),
                    binder: null,
                    callConvention: method.CallingConvention,
                    types: paramz,
                    modifiers: null
                );
                if (overriddenMethod is not null)
                {
                    //
                    // The default binder searches for COMPATIBLE not exact signature match [for instance
                    // typeof(Object).GetMethod("Equals", new[] { typeof(/*Any type*/) }) is never null]
                    //
                    // I won't create a custom binder so +1 check...
                    //

                    ParameterInfo[] overriddenParamz = overriddenMethod.GetParameters();

                    bool match = true;
                    for (int i = 0; i < overriddenParamz.Length; i++)
                    {
                        if (overriddenParamz[i].ParameterType != paramz[i])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                        return overriddenMethod;
                }
            }
            return null;
        }

        //
        // Similar logic is provided by Solti.Utils.Primitives, too. But we don't want to ship
        // that library with our source generator, so reimplement it.
        //

        public static MethodInfo ExtractFrom<T>(Expression<Action<T>> expression) => ((MethodCallExpression) expression.Body).Method;
    }
}
