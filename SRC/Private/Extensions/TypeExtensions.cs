/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        public static IEnumerable<TMember> ListMembers<TMember>(this Type src, Func<Type, BindingFlags, TMember[]> factory, bool includeNonPublic = false) where TMember : MemberInfo
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface())
                return factory(src, flags)
                    .Concat
                    (
                        src.GetInterfaces().SelectMany(iface => factory(iface, flags))
                    );

            //
            // A "BindingFlags.NonPublic" es "BindingFlags.FlattenHierarchy" nem ertelmezett interface-ekre.
            //

            flags |= BindingFlags.FlattenHierarchy;
            if (includeNonPublic) flags |= BindingFlags.NonPublic;

            return factory(src, flags);
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
                    GenericArgumentComparer.Instance
                );
        }

        //
        // Sajat comparer azert kell mert pl List<T> es IList<T> eseten typeof(List<T>).GetGenericArguments[0] != typeof(IList<T>).GetGenericArguments[0] 
        // testzoleges "T"-re.
        //

        private sealed class GenericArgumentComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type x, Type y) => GetHashCode(x) == GetHashCode(y);

            //
            // Generikus argumentumnak nincs teljes neve ezert a lenti sor jol kezeli a fenti
            // problemat.
            //

            [SuppressMessage("Globalization", "CA1307: Specify StringComparison", Justification = "Type names are not localized.")]
            public int GetHashCode(Type obj) => (obj.FullName ?? obj.Name).GetHashCode();

            public static GenericArgumentComparer Instance { get; } = new GenericArgumentComparer();
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
