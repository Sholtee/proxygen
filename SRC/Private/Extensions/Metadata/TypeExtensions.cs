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

        public static bool IsPointer(this Type src) => src.IsByRef ? src.GetElementType().IsPointer() : src.IsPointer;

        public static bool IsGenericParameter(this Type src) => (src.GetElementType(recurse: true) ?? src).IsGenericParameter;

        public static IEnumerable<TMember> ListMembers<TMember>(this Type src, bool includeNonPublic = false, bool includeStatic = false) where TMember : MemberInfo
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface)
                //
                // A "BindingFlags.NonPublic" es "BindingFlags.FlattenHierarchy" nem ertelmezett interface-ekre (es explicit 
                // implementaciokra).
                //

                return GetMembers(src).Concat
                (
                    src.GetInterfaces().SelectMany(GetMembers)
                );
          
            flags |= BindingFlags.FlattenHierarchy;
            if (includeNonPublic) flags |= BindingFlags.NonPublic;
            if (includeStatic) flags |= BindingFlags.Static;

            return GetMembers(src);

            IEnumerable<TMember> GetMembers(Type t) => t.GetMembers(flags).OfType<TMember>();
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

        public static bool EqualsTo(this Type src, Type that) 
        {
            if (src.IsGenericParameter != that.IsGenericParameter)
                return false;

            if (!src.IsGenericParameter)
                return src.Equals(that);

            return src switch 
            {
                _ when src.DeclaringMethod is not null && that.DeclaringMethod is not null =>
                    src.DeclaringMethod.GetGenericArguments().IndexOf(src) == that.DeclaringMethod.GetGenericArguments().IndexOf(that),
                _ when src.DeclaringType is not null && that.DeclaringType is not null =>
                    src.DeclaringType.GetGenericArguments().IndexOf(src) == that.DeclaringType.GetGenericArguments().IndexOf(that),
                _ => false
            };
        }
    }
}
