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
        //

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+(\[[\w,]+\])?", RegexOptions.Compiled);

        public static string GetFriendlyName(this Type src)
        {
            Debug.Assert(!src.IsGenericType() || src.IsGenericTypeDefinition());
            return TypeNameReplacer.Replace(src.IsNested ? src.Name : src.ToString(), string.Empty);
        }

        public static IEnumerable<TMember> ListMembers<TMember>(this Type src, Func<Type, BindingFlags, TMember[]> backend, bool includeNonPublic = false) where TMember : MemberInfo
        {
            IEnumerable<TMember> ifaceMembers = null;

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface() | includeNonPublic)
            {
                //
                // A "BindingFlags.NonPublic" es "BindingFlags.FlattenHierarchy" nem ertelmezett interface-ekre es explicit 
                // implementaciokra.
                //

                ifaceMembers = src.GetInterfaces().SelectMany(iface => backend(iface, flags));

                if (src.IsInterface()) 
                    return backend(src, flags).Concat(ifaceMembers);
            }
          
            flags |= BindingFlags.FlattenHierarchy;
            if (includeNonPublic) flags |= BindingFlags.NonPublic; // explicit implementaciokat nem adja vissza

            IEnumerable<TMember> classMembers = backend(src, flags);

            if (includeNonPublic)
                //
                // Explicit interface implementaciok azok akiknek nincs meg a parja a osztaly tagok
                // kozt.
                //
                // TODO: FIXME: 
                //   Ez nem fogja megtalalni azokat az explicit tagokat akik mellet van 
                //   azonos szignaturaval rendelkezo osztaly tag.
                //

                return classMembers.Concat(
                    ifaceMembers.Where(ifaceMember => !classMembers.Any(classMember => classMember.SignatureEquals(ifaceMember))));

            return classMembers;
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

        public static ConstructorInfo GetApplicableConstructor(this Type src)
        {
            ConstructorInfo[] ctors = src.GetConstructors();
            if (ctors.Length != 1)
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_AMBIGUITY, src));

            return ctors[0];
        }
    }
}