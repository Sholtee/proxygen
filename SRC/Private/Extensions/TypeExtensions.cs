/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

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
            Debug.Assert(!src.IsGenericType || src.IsGenericTypeDefinition || !src.GetOwnGenericArguments().Any());
            return TypeNameReplacer.Replace(src.IsNested ? src.Name : src.ToString(), string.Empty);
        }

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

        public static IEnumerable<ConstructorInfo> GetPublicConstructors(this Type src) 
        {
            ConstructorInfo[] constructors = src.GetConstructors();
            if (!constructors.Any())
                throw new InvalidOperationException(Resources.NO_PUBLIC_CTOR);

            return constructors;
        }
    }
}
