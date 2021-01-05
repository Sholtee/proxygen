/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.Proxy.Internals
{
    internal static partial class TypeExtensions
    {
        //
        // "&":  referencia szerinti parameter
        // "`d": generikus tipus ahol "d" egesz szam
        // "[T, TT]": generikus parameterek
        // "[<PropName_1>xXx, <PropName_2>xXx]": anonim oljektum property-k
        //

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+(\[[\w,<>]+\])?", RegexOptions.Compiled);

        public static string GetFriendlyName(this Type src)
        {
            if (src.IsByRef)
                return src.GetElementType()!.GetFriendlyName();

            if (src.IsGenericType)
                src = src.GetGenericTypeDefinition();

            return TypeNameReplacer.Replace(src.IsNested()
                ? src.Name 
                : src.ToString(), string.Empty);
        }

        public static string? GetFullName(this Type src) 
        {
            if (src.IsByRef)
                return src.GetElementType()!.GetFullName();

            if (src.IsGenericType)
                src = src.GetGenericTypeDefinition();

            return src.FullName;
        }

        public static Type? GetElementType(this Type src, bool recurse) 
        {
            Type? prev = null;

            for (Type? current = src; (current = current.GetElementType()) != null;)
            {
                if (!recurse) return current;
                prev = current;
            }

            return prev;
        }

        public static bool IsNested(this Type src) =>
            //
            // GetElementType()-os csoda azert kell mert beagyazott tipusbol kepzett (pl) tomb
            // mar nem beagyazott tipus.
            //

            src.GetElementType(recurse: true)?.IsNested ?? src.IsNested;

        public static bool IsClass(this Type src) => !src.IsGenericParameter && src.IsClass;

        public static bool IsAbstract(this Type src) => src.IsAbstract && !src.IsSealed; // statikus osztalyok IL szinten "sealed abstract"-k

        public static IEnumerable<MethodInfo> ListMethods(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            (t, f) => t.GetMethods(f),
            m => m.GetAccessModifiers(),

            //
            // Metodus visszaterese lenyegtelen, csak a nev, parameter tipusa es atadasa kell
            //

            m =>
            {
                var hk = new HashCode();

                foreach (var descr in m.GetParameters().Select(p => new { p.ParameterType, ParameterKind = p.GetParameterKind() }))
                {
                    hk.Add(descr);
                }

                return new
                {
                    m.Name,
                    ParamzHash = hk.ToHashCode()
                };
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
            p => p.Name, // tipus lenyegtelen, tulajdonsagbol adott nevvel csak egy db lehet adott tipusban
            includeStatic
        );

        public static IEnumerable<EventInfo> ListEvents(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            (t, f) => t.GetEvents(f),
            e => (e.AddMethod ?? e.RemoveMethod).GetAccessModifiers(),
            e => e.Name, // tipus lenyegtelen, esemeny adott nevvel csak egy db lehet adott tipusban
            includeStatic
        );

        private static IEnumerable<TMember> ListMembersInternal<TMember>(
            this Type src, 
            Func<Type, BindingFlags, TMember[]> getter, 
            Func<TMember, AccessModifiers> getVisibility, 
            Func<TMember, object> getDescriptor, 
            bool includeStatic)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface)
                //
                // - A "BindingFlags.NonPublic" es "BindingFlags.FlattenHierarchy" nem
                //   ertelmezett interface-ekre.
                // - Ez a megoldas a "new" kulcsszo altal elrejtett tagokat is visszaadja 
                //   (ami szukseges interface-ek eseteben).
                //

                return src.GetInterfaces().Append(src).SelectMany(GetMembers);

            //
            // A BindingFlags.FlattenHierarchy csak a publikus es vedett tagokat adja vissza
            // az os osztalyokbol, privatot nem, viszont az explicit implementaciok privat
            // tagok... 
            //

            //flags |= BindingFlags.FlattenHierarchy;
            flags |= BindingFlags.NonPublic;
            if (includeStatic) flags |= BindingFlags.Static;

            var returnedMembers = new HashSet<object>();

            //
            // Sorrend fontos, a leszarmazottol haladunk az os fele
            //

            return new[] { src }.Concat(src.GetBaseTypes()).SelectMany(GetMembers).Where
            (
                //
                // Ha meg korabban nem volt visszaadva ("new", "override" miatt) es nem is privat akkor
                // jok vagyunk.
                //

                m => getVisibility(m) is not AccessModifiers.Private && returnedMembers.Add
                (
                    getDescriptor(m)
                )
            );

            IEnumerable<TMember> GetMembers(Type t) => getter(t, flags);
        }

        public static IEnumerable<Type> GetParents(this Type type)
        {
            //
            // "Cica<T>.Mica<TT>.Kutya" is generikusnak minosul: Generikus formaban Cica<T>.Mica<TT>.Kutya<T, TT>
            // mig tipizaltan "Cica<T>.Mica<T>.Kutya<TConcrete1, TConcrete2>".
            // Ami azert lassuk be igy eleg szopas.
            //

            IEnumerable<Type> genericArgs = type.GetGenericArguments();

            for (Type parent = type; (parent = parent.DeclaringType) != null;)
            {
                int gaCount = parent.GetGenericArguments().Length;

                yield return gaCount == 0
                    ? parent
                    : parent.MakeGenericType
                    (
                        genericArgs.Take(gaCount).ToArray()
                    );
            }
        }

        public static IEnumerable<Type> GetEnclosingTypes(this Type type) => type.GetParents().Reverse();

        public static IEnumerable<Type> GetBaseTypes(this Type type) 
        {
            for (Type? baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                yield return baseType;
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
                for (Type? parent = src; (parent = parent.DeclaringType) != null;)
                    //
                    // Ha "parent" nem generikus akkor a GetGenericArguments() ures tombot ad vissza
                    //

                    if (parent.GetGenericArguments().Contains(openArgs[i], ArgumentComparer.Instance))
                    {
                        own = false;
                        break;
                    }
                if (own) yield return closedArgs[i];
            } 
        }
    }
}
