/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

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
            _ when src.IsPrivate && src.GetImplementedInterfaceMethods().Some() => AccessModifiers.Explicit,
            _ when src.IsPrivate => AccessModifiers.Private,
            #pragma warning disable CA2201 // In theory we should never reach here.
            _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
            #pragma warning restore CA2201
        };

        private static readonly Regex FIsSpecial = new("^(op|get|set|add|remove)_\\w+", RegexOptions.Compiled);

        public static bool IsSpecial(this MethodBase src) =>
            //
            // NET6_0-tol interface-nek lehet absztrakt statikus tagja:
            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/static-abstract-interface-methods
            //
            // Ha ezeken operatort definialunk akkor az nem lesz SpecialName (sztem mondjuk az kene legyen).
            //

            src.IsSpecialName || (src.GetAccessModifiers() is AccessModifiers.Explicit && src.IsStatic && FIsSpecial.IsMatch(src.StrippedName()));

        public static IEnumerable<Type> GetDeclaringInterfaces(this MethodBase src) => src.ReflectedType.IsInterface
            ? Array.Empty<Type>()
            : src
                .GetImplementedInterfaceMethods()
                .Convert(m => m.ReflectedType);

        public static IEnumerable<MethodBase> GetImplementedInterfaceMethods(this MethodBase src)
        {
            if (src is not MethodInfo method)
                yield break; // ctor

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

        //
        // GetBaseDefinition() nem mukodik ha nem virtualis metodust irtunk felul (lasd "new" kulcsszo).
        //

        public static MethodInfo? GetOverriddenMethod(this MethodInfo method)
        {
            Type[] paramz = method
                .GetParameters()
                .ConvertAr(static p => p.ParameterType);

            for (Type? baseType = method.ReflectedType; (baseType = baseType?.BaseType) is not null;)
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
                    // A default binder nem a pontos szignaturara keres hanem a kompatibilisre, ami baszottul
                    // nagy kulonbseg: pl typeof(Object).GetMethod("Equals", new[] { typeof(Akarmi) }) sose null,
                    // hisz az Object.Equals(object) barmivel hivhato (tehat azt kapjuk vissza).
                    // Na mar most, az kurva elet hogy en nem irok sajat bindert u h +1 check
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
    }
}
