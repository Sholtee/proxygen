/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static partial class TypeExtensions
    {
        //
        // https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names
        //
        // "&": by ref parameter
        // "*": pointer
        // "`d": generic type where "d" is an integer
        // "[T, TT]": generic parameter
        // "[<PropName_1>xXx, <PropName_2>xXx]": props belong to an anon object
        //

        private static readonly Regex TypeNameReplacer = new(@"\&|\*|`\d+(\[[\w,<>]+\])?", RegexOptions.Compiled);

        public static string GetFriendlyName(this Type src)
        {
            if (src.GetInnermostElementType()?.IsGenericType is true)
                src = src.GetGenericDefinition();

            return TypeNameReplacer.Replace
            (
                src.IsNested()
                    ? src.Name 
                    : src.ToString(), 
                string.Empty
            );
        }

        public static Type GetGenericDefinition(this Type src)  // works with GenericType<TConcrete>[] too
        {
            if (src.IsArray)
            {
                int rank = src.GetArrayRank();
                src = src.GetElementType().GetGenericDefinition();
                return src.MakeArrayType(rank);
            }

            if (src.IsByRef)
            {
                src = src.GetElementType().GetGenericDefinition();
                return src.MakeByRefType();
            }

            if (src.IsPointer)
            {
                src = src.GetElementType().GetGenericDefinition();
                return src.MakePointerType();
            }

            return src.GetGenericTypeDefinition();
        }

        public static string? GetQualifiedName(this Type src) 
        {
            src = src.GetInnermostElementType() ?? src;

            if (src.IsGenericType)
                src = src.GetGenericDefinition();

            return src.FullName;
        }

        public static Type? GetInnermostElementType(this Type src) 
        {
            Type? prev = null;

            for (Type? current = src; (current = current!.GetElementType()) is not null;)
            {
                prev = current;
            }

            return prev;
        }

        public static Type? GetEnclosingType(this Type src) 
        {
            src = src.GetInnermostElementType() ?? src;

            if (src.IsGenericParameter)
                return null;

            Type? enclosingType = src.DeclaringType;
            if (enclosingType is null)
                return null;

            //
            // "Cica<T>.Mica<TT>.Kutya" counts as generic, too: In open form it is returned as Cica<T>.Mica<TT>.Kutya<T, TT>
            // while in closed as "Cica<T>.Mica<TT>.Kutya<TConcrete1, TConcrete2>".
            //

            int gaCount = enclosingType.GetGenericArguments().Length;
            if (gaCount is 0)
                return enclosingType;

            Type[] gas = new Type[gaCount];
            Array.Copy(src.GetGenericArguments(), 0, gas, 0, gaCount);

            return enclosingType.MakeGenericType(gas);
        }

        public static bool IsNested(this Type src) =>
            //
            // Types (for instance arrays) derived from embedded types are not embedded anymore.
            //

            (src.GetInnermostElementType() ?? src).IsNested;

        public static bool IsClass(this Type src) => !src.IsGenericParameter && src.IsClass;

        public static bool IsAbstract(this Type src) => src.IsAbstract && !src.IsSealed; // IL representation of static classes are "sealed abstract"

        public static IEnumerable<MethodInfo> ListMethods(this Type src, bool includeStatic = false, bool skipSpecial = true)
        {
            IEnumerable<MethodInfo> methods = src.ListMembersInternal
            (
                static (t, f) => t.GetMethods(f),
                static m => m,
                static m => m.GetOverriddenMethod(),
                includeStatic
            );

            if (skipSpecial)
                methods = methods.Where(static m => !m.IsSpecialName);

            return methods;
        }

        public static IEnumerable<PropertyInfo> ListProperties(this Type src, bool includeStatic = false)
        {
            return src.ListMembersInternal
            (
                static (t, f) => t.GetProperties(f),
                GetUnderlyingMethod,
                static p => GetUnderlyingMethod(p).GetOverriddenMethod(),
                includeStatic
            );

            //
            // Higher visibility has the precedence
            //

            static MethodInfo GetUnderlyingMethod(PropertyInfo prop)
            {
                if (prop.GetMethod is null)
                    return prop.SetMethod;

                if (prop.SetMethod is null)
                    return prop.GetMethod;

                return prop.GetMethod.GetAccessModifiers() > prop.SetMethod.GetAccessModifiers()
                    ? prop.GetMethod
                    : prop.SetMethod;
            }
        }

        public static IEnumerable<EventInfo> ListEvents(this Type src, bool includeStatic = false)
        {
            return src.ListMembersInternal
            (
                static (t, f) => t.GetEvents(f),
                GetUnderlyingMethod,
                static e => GetUnderlyingMethod(e).GetOverriddenMethod(),
                includeStatic
            );

            //
            // Higher visibility has the precedence
            //

            static MethodInfo GetUnderlyingMethod(EventInfo evt)
            {
                if (evt.AddMethod is null)
                    return evt.RemoveMethod;

                if (evt.RemoveMethod is null)
                    return evt.AddMethod;

                return evt.AddMethod.GetAccessModifiers() > evt.RemoveMethod.GetAccessModifiers()
                    ? evt.AddMethod
                    : evt.RemoveMethod;
            }
        }

        private static IEnumerable<TMember> ListMembersInternal<TMember>(
            this Type src, 
            Func<Type, BindingFlags, TMember[]> getter, 
            Func<TMember, MethodInfo> getUnderlyingMethod, 
            Func<TMember, MethodInfo?> getOverriddenMethod, 
            bool includeStatic) where TMember: MemberInfo
        {
            if (src.IsGenericParameter)
                yield break;

            BindingFlags flags = 
                BindingFlags.Public |

                //
                // BindingFlags.FlattenHierarchy will return public and protected members only. Unfortunately
                // explicit implementations are private
                //

                //BindingFlags.FlattenHierarchy |

                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly;

            //
            // As of NET6_0 we may declare static members on interfaces
            //

            if (includeStatic)
                flags |= BindingFlags.Static;

            if (src.IsInterface)
            {
                foreach (Type t in src.GetHierarchy())
                {
                    foreach (TMember member in getter(t, flags))
                    {
                        yield return member;
                    }
                }
            }
            else
            {
                HashSet<MethodInfo> overriddenMethods = new();

                //
                // Order matters: we're processing the hierarchy towards the ancestor
                //

                foreach (Type t in src.GetHierarchy())
                {
                    foreach (TMember member in getter(t, flags))
                    {
                        MethodInfo? 
                            overriddenMethod = getOverriddenMethod(member),
                            underlyingMethod = getUnderlyingMethod(member);

                        if (overriddenMethod is not null)
                            overriddenMethods.Add(overriddenMethod);

                        if (overriddenMethods.Contains(underlyingMethod))
                            continue;

                        //
                        // If it was not yielded before (due to "new" or "override") and not private then we are fine.
                        //

                        if (underlyingMethod.GetAccessModifiers() > AccessModifiers.Private)
                            yield return member;
                    }
                }
            }
        }

        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type)
        {
            //
            // Array type really sucks: All its constructors are generated by the compiler so from symbols
            // they cannot be retrieved.
            //

            if (type.IsArray)
                yield break;

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (ctor.GetAccessModifiers() > AccessModifiers.Private)
                    yield return ctor;
            }
        }

        public static IEnumerable<Type> GetBaseTypes(this Type type) 
        {
            for (Type? baseType = type.GetBaseType(); baseType is not null; baseType = baseType.GetBaseType())
                yield return baseType;
        }

        public static IEnumerable<Type> GetAllInterfaces(this Type type) => !type.IsGenericParameter
            ? type.GetInterfaces()
            : Array.Empty<Type>();

        public static Type? GetBaseType(this Type src) => !src.IsGenericParameter
            ? src.BaseType
            : null;

        public static IEnumerable<Type> GetHierarchy(this Type src)
        {
            yield return src;

            foreach (Type t in src.IsInterface ? src.GetAllInterfaces() : src.GetBaseTypes())
            {
                yield return t;
            }
        }

        public static IEnumerable<Type> GetOwnGenericArguments(this Type src)
        {
            if (!src.IsGenericType)
                yield break;

            //
            // "Cica<T>.Mica<TT>.Kutya" counts as generic, too: In open form it is returned as Cica<T>.Mica<TT>.Kutya<T, TT>
            // while in closed as "Cica<T>.Mica<TT>.Kutya<TConcrete1, TConcrete2>".
            //

            IReadOnlyList<Type> 
                closedArgs = src.GetGenericArguments(),
                openArgs = (src = src.GetGenericTypeDefinition()).GetGenericArguments();

            for(int i = 0; i < openArgs.Count; i++)
            {
                bool own = true;
                for (Type? parent = src; (parent = parent!.DeclaringType) is not null;)
                {
                    //
                    // GetGenericArguments() may return empty array if "parent" is not generic
                    //

                    if (parent.GetGenericArguments().Any(arg => ArgumentComparer.Instance.Equals(arg, openArgs[i])))
                    {
                        own = false;
                        break;
                    }
                }
                if (own) 
                    yield return closedArgs[i];
            } 
        }

        public static AccessModifiers GetAccessModifiers(this Type src)
        {
            src = src.GetInnermostElementType() ?? src;

            AccessModifiers am = src switch
            {
                _ when (src.IsPublic && src.IsVisible) || src.IsNestedPublic => AccessModifiers.Public,
                _ when src.IsNestedFamily => AccessModifiers.Protected,
                _ when src.IsNestedFamORAssem => AccessModifiers.Protected | AccessModifiers.Internal,
                _ when src.IsNestedFamANDAssem => AccessModifiers.Protected | AccessModifiers.Private,
                _ when src.IsNestedAssembly || (!src.IsVisible && !src.IsNested) => AccessModifiers.Internal,
                _ when src.IsNestedPrivate => AccessModifiers.Private,
                #pragma warning disable CA2201 // In theory we should never reach here.
                _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
                #pragma warning restore CA2201
            };

            //
            // Generic arguments may impact the visibility.
            //

            if (src.IsConstructedGenericType)
            {
                foreach (Type ga in src.GetGenericArguments())
                {
                    AccessModifiers gaAm = ga.GetAccessModifiers();
                    if (gaAm < am)
                        am = gaAm;
                }
            }

            Type? enclosingType = src.GetEnclosingType();
            if (enclosingType is not null)
            {
                AccessModifiers etAm = enclosingType.GetAccessModifiers();
                if (etAm < am)
                    am = etAm;
            }

            return am;
        }

        public static RefType GetRefType(this Type src) => src switch
        {
            _ when
            #if NETSTANDARD2_1_OR_GREATER
                src.IsByRefLike ||
            #endif
                src.GetCustomAttributes().Any(static ca => ca.GetType().FullName?.Equals("System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparison.OrdinalIgnoreCase) is true) => RefType.Ref, // ref struct
            _ when src.IsPointer => RefType.Pointer,
            _ when src.IsArray => RefType.Array,
            _ => RefType.None
        };

        //
        // IsFunctionPointer is available in net8.0+ only
        //

        private static Func<Type, bool> GetIsFunctionPointerCore()
        {
            PropertyInfo? prop = typeof(Type).GetProperty("IsFunctionPointer");
            ParameterExpression type = Expression.Parameter(typeof(Type), nameof(type));
            return Expression.Lambda<Func<Type, bool>>
            (
                body: prop is null ? Expression.Constant(false) : Expression.Property(type, prop),
                type
            ).Compile();
        }

        private static readonly Func<Type, bool> FIsFunctionPointerCore = GetIsFunctionPointerCore();

        public static bool IsFunctionPointer(this Type src) => FIsFunctionPointerCore(src);
    }
}
