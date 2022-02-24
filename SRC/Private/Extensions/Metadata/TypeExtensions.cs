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

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|\*|`\d+(\[[\w,<>]+\])?", RegexOptions.Compiled);

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
            // GetElementType()-os csoda azert kell mert beagyazott tipusbol kepzett (pl) tomb
            // mar nem beagyazott tipus.
            //

            src.GetInnermostElementType()?.IsNested ?? src.IsNested;

        public static bool IsClass(this Type src) => !src.IsGenericParameter && src.IsClass;

        public static bool IsAbstract(this Type src) => src.IsAbstract && !src.IsSealed; // statikus osztalyok IL szinten "sealed abstract"-k

        public static IEnumerable<MethodInfo> ListMethods(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            (t, f) => t.GetMethods(f),
            MethodBaseExtensions.GetAccessModifiers,

            //
            // Metodus visszaterese lenyegtelen, csak a nev, parameter tipusa es atadasa, valamint a generikus argumentumok
            // szama a lenyeges.
            //

            m =>
            {
                HashCode hk = new();

                foreach (ParameterInfo p in m.GetParameters())
                {
                    hk.Add(new 
                    { 
                        p.ParameterType, 
                        ParameterKind = p.GetParameterKind() 
                    });
                }

                hk.Add(new
                {
                    m.Name,
                    m.GetGenericArguments().Length,
                    m.IsStatic, // ugyanolyan nevvel es parameterekkel lehet statikus es nem statikus is
                });

                return hk.ToHashCode();
            },
            includeStatic
        );

        public static IEnumerable<PropertyInfo> ListProperties(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            (t, f) => t.GetProperties(f),

            //
            // A nagyobb lathatosagut tekintjuk mervadonak
            //

            p => (AccessModifiers) Math.Max((int) (p.GetMethod?.GetAccessModifiers() ?? AccessModifiers.Unknown), (int) (p.SetMethod?.GetAccessModifiers() ?? AccessModifiers.Unknown)),
            p => new { p.Name, (p.GetMethod ?? p.SetMethod).IsStatic }.GetHashCode(),
            includeStatic
        );

        public static IEnumerable<EventInfo> ListEvents(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            (t, f) => t.GetEvents(f),
            e => (e.AddMethod ?? e.RemoveMethod).GetAccessModifiers(),
            e => new { e.Name, (e.AddMethod ?? e.RemoveMethod).IsStatic }.GetHashCode(),
            includeStatic
        );

        private static IEnumerable<TMember> ListMembersInternal<TMember>(
            this Type src, 
            Func<Type, BindingFlags, TMember[]> getter, 
            Func<TMember, AccessModifiers> getVisibility, 
            Func<TMember, int> getHashCode, 
            bool includeStatic) where TMember: MemberInfo
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface)
            {
                foreach (Type t in src.GetHierarchy())
                {
                    foreach (TMember member in getter(t, flags))
                    {
                        yield return member;
                    }
                }
                yield break;
            }

            //
            // A BindingFlags.FlattenHierarchy csak a publikus es vedett tagokat adja vissza az os osztalyokbol,
            // privatot nem, viszont az explicit implementaciok privat tagok... 
            //

            //flags |= BindingFlags.FlattenHierarchy;
            flags |= BindingFlags.NonPublic;
            if (includeStatic)
                flags |= BindingFlags.Static;

            HashSet<int> returnedMembers = new();

            //
            // Sorrend fontos, a leszarmazottol haladunk az os fele
            //

            foreach (Type t in src.GetHierarchy())
            {
                foreach (TMember member in getter(t, flags))
                {
                    //
                    // Ha meg korabban nem volt visszaadva ("new", "override" miatt) es nem is privat akkor
                    // jok vagyunk.
                    //

                    if (getVisibility(member) is not AccessModifiers.Private && returnedMembers.Add(getHashCode(member)))
                        yield return member;
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
            if (!src.IsGenericType) yield break;

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
    }
}
