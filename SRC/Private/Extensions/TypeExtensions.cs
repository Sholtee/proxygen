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
        //

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+(\[[\w,]+\])?", RegexOptions.Compiled);

        public static string GetFriendlyName(this Type src)
        {
            Debug.Assert(!src.IsGenericType() || src.IsGenericTypeDefinition());
            return TypeNameReplacer.Replace(src.IsNested ? src.Name : src.ToString(), string.Empty);
        }
#if !NETSTANDARD1_6
        public static IReadOnlyDictionary<MethodBase, TMember> GetMembersByAccessor<TMember>(this Type src) where TMember: MemberInfo 
        {
            Dictionary<MethodBase, TMember> result = new Dictionary<MethodBase, TMember>();

            foreach (TMember member in src
                .GetMembers(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic)
                .OfType<TMember>())
            {
                switch (member) 
                {
                    /*
                    case ConstructorInfo constructor:
                        result.Add(constructor, member);
                        break;
                    */
                    case MethodInfo method:
                        result.Add(method, member);
                        break;
                    case PropertyInfo property:
                        if (property.GetMethod != null) result.Add(property.GetMethod, member);
                        if (property.SetMethod != null) result.Add(property.SetMethod, member);
                        break;
                    case EventInfo @event:
                        if (@event.AddMethod != null) result.Add(@event.AddMethod, member);
                        if (@event.RemoveMethod != null) result.Add(@event.RemoveMethod, member);
                        break;
                    default:
                        continue;
                }
            }

            return result;
        }

        public static IReadOnlyDictionary<TMember, TMember> GetInterfaceMappings<TMember>(this Type src) where TMember: MemberInfo
        {
            Debug.Assert(src.IsClass());

            var result = new Dictionary<TMember, TMember>();

            IReadOnlyDictionary<MethodBase, TMember> classMembers = src.GetMembersByAccessor<TMember>();

            foreach (Type iface in src.GetInterfaces())
            {
                IReadOnlyDictionary<MethodBase, TMember> ifaceMembers = iface.GetMembersByAccessor<TMember>();

                InterfaceMapping mapping = src.GetInterfaceMap(iface);

                for (int i = 0; i < mapping.InterfaceMethods.Length; i++) 
                {
                    //
                    // Biztosan letezik
                    //

                    TMember ifaceMember = ifaceMembers[mapping.InterfaceMethods[i]];

                    //
                    // Letezhetnek olyan tagok amik tobb modon is hozzaferhetok (pl.: Property.[Get|Set]Method)
                    // es amiatt mar fel lehet veve.
                    //

                    if (result.ContainsKey(ifaceMember)) continue;

                    //
                    // Explicit implementacioknal pl nem letezik.
                    //

                    classMembers.TryGetValue(mapping.TargetMethods[i], out var classMember);

                    //
                    // Mappolas felvetele (ertek lehet NULL).
                    //

                    result.Add(ifaceMember, classMember);
                }
            }

            return result;
        }
#endif
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

        public static ConstructorInfo GetApplicableConstructor(this Type src, string assemblyName)
        {
            ConstructorInfo[] ctors = src
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
                .Where(ctor => !ctor.IsPrivate) // protected es internal tagokat meg visszaadja
                .ToArray();

            if (ctors.Length != 1)
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_AMBIGUITY, src));

            ConstructorInfo result = ctors[0];

            //
            // Ez atengedi azt ha a deklaralo tipus maga nem lathato viszont ide akkor mar el sem kene jussunk.
            //

            Visibility.Check(result, assemblyName, allowProtected: true);

            return result;
        }
    }
}
