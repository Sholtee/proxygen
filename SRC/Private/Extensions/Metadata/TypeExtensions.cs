/********************************************************************************
* TypeExtensions.cs                                                             *
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

    internal static partial class TypeExtensions
    {
        //
        // https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names
        //
        // "&": referencia szerinti parameter
        // "*": mutato parameter
        // "`d": generikus tipus ahol "d" egesz szam
        // "[T, TT]": generikus parameterek
        // "[<PropName_1>xXx, <PropName_2>xXx]": anonim oljektum property-k
        //

        private static readonly Regex TypeNameReplacer = new(@"\&|\*|`\d+(\[[\w,<>]+\])?", RegexOptions.Compiled);

        public static string GetFriendlyName(this Type src)
        {
            if (src.IsByRef)
                return src.GetElementType()!.GetFriendlyName();

            if (src.IsGenericType)
                src = src.GetGenericTypeDefinition();

            return TypeNameReplacer.Replace
            (
                src.IsNested()
                    ? src.Name 
                    : src.ToString(), 
                string.Empty
            );
        }

        public static string? GetQualifiedName(this Type src) 
        {
            if (src.HasElementType)
                return src.GetElementType()!.GetQualifiedName();

            if (src.IsGenericType)
                src = src.GetGenericTypeDefinition();

            return src.FullName?.TrimEnd('&');
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
            if (src.IsGenericParameter)
                return null;

            Type? enclosingType = src.DeclaringType;
            if (enclosingType is null)
                return null;

            //
            // "Cica<T>.Mica<TT>.Kutya" is generikusnak minosul: Generikus formaban Cica<T>.Mica<TT>.Kutya<T, TT>
            // mig tipizaltan "Cica<T>.Mica<T>.Kutya<TConcrete1, TConcrete2>".
            // Ami azert lassuk be igy eleg szopas.
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
            // GetInnermostElementType()-os csoda azert kell mert beagyazott tipusbol kepzett (pl) tomb
            // mar nem beagyazott tipus.
            //

            src.GetInnermostElementType()?.IsNested ?? src.IsNested;

        public static bool IsClass(this Type src) => !src.IsGenericParameter && src.IsClass;

        public static bool IsAbstract(this Type src) => src.IsAbstract && !src.IsSealed; // statikus osztalyok IL szinten "sealed abstract"-k

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
                methods = methods.Convert(static m => m, static m => m.IsSpecial());

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
            // A nagyobb lathatosagut tekintjuk mervadonak
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
            // A nagyobb lathatosagut tekintjuk mervadonak
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
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            //
            // NET6_0-tol kezdve lehet statikusokat deklaralni interface-eken is
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
                //
                // A BindingFlags.FlattenHierarchy csak a publikus es vedett tagokat adja vissza az os osztalyokbol,
                // privatot nem, viszont az explicit implementaciok privat tagok... 
                //

                //flags |= BindingFlags.FlattenHierarchy;
                flags |= BindingFlags.NonPublic;

                HashSet<MethodInfo> overriddenMethods = new();

                //
                // Sorrend fontos, a leszarmazottol haladunk az os fele
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
                        // Ha meg korabban nem volt visszaadva ("new", "override" miatt) es nem is privat akkor
                        // jok vagyunk.
                        //

                        if (underlyingMethod.GetAccessModifiers() > AccessModifiers.Private)
                            yield return member;
                    }
                }
            }
        }

        public static IEnumerable<Type> GetBaseTypes(this Type type) 
        {
            for (Type? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
                yield return baseType;
        }

        public static IEnumerable<Type> GetHierarchy(this Type src)
        {
            yield return src;

            foreach (Type t in src.IsInterface ? src.GetInterfaces() : src.GetBaseTypes())
            {
                yield return t;
            }
        }

        public static IEnumerable<Type> GetOwnGenericArguments(this Type src)
        {
            if (!src.IsGenericType)
                yield break;

            //
            // "Cica<T>.Mica<TT>.Kutya" is generikusnak minosul: Generikus formaban Cica<T>.Mica<TT>.Kutya<T, TT>
            // mig tipizaltan "Cica<T>.Mica<T>.Kutya<TConcrete1, TConcrete2>".
            // Ami azert lassuk be igy eleg szopas.
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
                    // Ha "parent" nem generikus akkor a GetGenericArguments() ures tombot ad vissza
                    //

                    if (parent.GetGenericArguments().Some(arg => ArgumentComparer.Instance.Equals(arg, openArgs[i])))
                    {
                        own = false;
                        break;
                    }
                }
                if (own) 
                    yield return closedArgs[i];
            } 
        }

        public static AccessModifiers GetAccessModifiers(this Type src) => (src = src.HasElementType ? src.GetInnermostElementType()! : src) switch
        {
            //
            // Mi van ha a korul zart tipus publikus viszont a korul zaro maga nem (lasd IsVisible)?
            // Ezt most en onkenyesen akkor publikusnak veszem
            //

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
    }
}
