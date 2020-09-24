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
            Debug.Assert(!src.IsGenericType || src.IsGenericTypeDefinition);
            return TypeNameReplacer.Replace(src.IsNested ? src.Name : src.ToString(), string.Empty);
        }

        public static IEnumerable<TMember> ListMembers<TMember>(this Type src, bool includeNonPublic = false) where TMember : MemberInfo
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

            return GetMembers(src);

            IEnumerable<TMember> GetMembers(Type t) => t.GetMembers(flags).OfType<TMember>();
        }

        public static IEnumerable<Assembly> GetReferences(this Type src)
        {
            var result = new HashSet<Assembly>(src.GetBasicReferences());

            foreach (MemberInfo member in src.ListMembers<MemberInfo>()) 
            {
                switch (member) 
                {
                    case MethodBase methodBase: // ctor, method
                    {
                        //
                        // Generikus parameterek lenyegtelenek
                        //

                        foreach (Type param in methodBase.GetParameters().Select(param => param.ParameterType))
                            AddAsmsFrom(param);

                        if (methodBase is MethodInfo method && method.ReturnType != typeof(void))
                            AddAsmsFrom(method.ReturnType);

                        continue;
                    }
                    case FieldInfo field: 
                    {
                        AddAsmsFrom(field.FieldType);
                        continue;
                    }
                    case PropertyInfo property:
                    {
                        AddAsmsFrom(property.PropertyType);
                        continue;
                    }
                    case EventInfo evt:
                    {
                        AddAsmsFrom(evt.EventHandlerType);
                        continue;
                    }
                }
            }

            return result;

            void AddAsmsFrom(Type t) 
            {
                foreach (Assembly asm in t.GetBasicReferences())
                    result.Add(asm);
            }
        }

        public static IEnumerable<Assembly> GetBasicReferences(this Type src) 
        {
            var result = new HashSet<Assembly>(new[] { src.Assembly });

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            if (src.IsGenericType)
                foreach (Type type in src.GetGenericArguments().Where(t => !t.IsGenericParameter))
                    foreach (Assembly asm in type.GetBasicReferences())
                        result.Add(asm);

            //
            // Az os (osztaly) szerepelhet masik szerelvenyben. "BaseType" csak az os osztalyokat adja vissza
            // megvalositott interfaceket nem.
            //

            for(Type? baseType = src.BaseType; baseType != null; baseType = baseType.BaseType)
                foreach (Assembly asm in baseType.GetBasicReferences())
                    result.Add(asm);

            return result;
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
            Debug.Assert(!src.IsGenericType || src.IsGenericTypeDefinition);

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
