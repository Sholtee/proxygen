﻿/********************************************************************************
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
            Debug.Assert(!src.IsGenericType() || src.IsGenericTypeDefinition());
            return TypeNameReplacer.Replace(src.IsNested ? src.Name : src.ToString(), string.Empty);
        }

        public static IEnumerable<TMember> ListMembers<TMember>(this Type src, bool includeNonPublic = false) where TMember : MemberInfo
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface())
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

            return GetMembers(src);

            IEnumerable<TMember> GetMembers(Type t) => t.GetMembers(flags).OfType<TMember>();
        }

        //
        // TODO: szukiteni a szerelvenyek halmazat (csak azokat adjuk vissza amitol "src" tenylegesen fugg is)
        //

        public static IEnumerable<Assembly> GetReferences(this Type src)
        {
            Assembly declaringAsm = src.Assembly();

            var references = new[] { declaringAsm }.Concat(declaringAsm.GetReferences());

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            if (src.IsGenericType())
                foreach (Type type in src.GetGenericArguments().Where(t => !t.IsGenericParameter))
                    references = references.Concat(type.GetReferences());

            return references.Distinct();
        }

        public static IEnumerable<Type> GetParents(this Type type)
        {
            return GetParentsInternal().Reverse();

            IEnumerable<Type> GetParentsInternal()
            {
                for (Type parent = type.DeclaringType; parent != null; parent = parent.DeclaringType)
                    yield return parent;
            }
        }

        public static IEnumerable<Type> GetOwnGenericArguments(this Type src)
        {
            Debug.Assert(!src.IsGenericType() || src.IsGenericTypeDefinition());

            return src
                .GetGenericArguments()
                .Except
                (
                    src.DeclaringType?.GetGenericArguments() ?? Array.Empty<Type>(), 
                    ArgumentComparer.Instance
                );
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
