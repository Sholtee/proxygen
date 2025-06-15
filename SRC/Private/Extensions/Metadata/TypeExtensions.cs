/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    /// <summary>
    /// Helper methods for the <see cref="Type"/> class.
    /// </summary>
    internal static class TypeExtensions
    {
        //
        // https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names
        //
        // "&": by ref parameter
        // "*": pointer
        // "`d": generic type where "d" is an integer
        // "[T, TT]": generic parameter
        // "[<PropName_1>xXx, <PropName_2>xXx]": props belong to an anon object
        //

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Regex FTypeNameReplacer = new(@"\&|\*|`\d+(\[[\w,<>]+\])?", RegexOptions.Compiled);

        /// <summary>
        /// Gets the friendly name of the given <see cref="Type"/>. Friendly name doesn't contain references for generic arguments, pointer features or the enclosing type.
        /// </summary>
        public static string GetFriendlyName(this Type src)
        {
            if (src.GetInnermostElementType()?.IsGenericType is true)
                src = src.GetGenericDefinition();

            return FTypeNameReplacer.Replace
            (
                src.IsNested()
                    ? src.Name 
                    : src.ToString(), 
                string.Empty
            );
        }

        /// <summary>
        /// Gets the generic definition of the given. Handles pointer types properly.
        /// </summary>
        public static Type GetGenericDefinition(this Type src)
        {
            return src switch
            {
                { IsArray: true } => GetGenericDefinitionCore(src).MakeArrayType(src.GetArrayRank()),
                { IsByRef: true } => GetGenericDefinitionCore(src).MakeByRefType(),
                { IsPointer: true } => GetGenericDefinitionCore(src).MakePointerType(),
                _ => src.GetGenericTypeDefinition()
            };

            static Type GetGenericDefinitionCore(Type src) => src.GetInnermostElementType()!.GetGenericDefinition();
        }

        /// <summary>
        /// Gets the qualified name of the given <see cref="Type"/>. Handles pointer types properly.
        /// </summary>
        public static string? GetQualifiedName(this Type src) 
        {
            src = src.GetInnermostElementType() ?? src;

            if (src.IsGenericType)
                src = src.GetGenericDefinition();

            return src.FullName;
        }

        /// <summary>
        /// Resolves the given pointer <see cref="Type"/> by returning the inner most element <see cref="Type"/>.
        /// </summary>
        public static Type? GetInnermostElementType(this Type src) 
        {
            Type? prev = null;

            for (Type? current = src; (current = current!.GetElementType()) is not null;)
                prev = current;

            return prev;
        }

        /// <summary>
        /// Gets the enclosing type if the given type. Handles generic parents properly.
        /// </summary>
        public static Type? GetEnclosingType(this Type src) 
        {
            src = src.GetInnermostElementType() ?? src;

            Type? enclosingType = src.DeclaringType;
            if (enclosingType is null)
                return null;

            if (src.IsGenericParameter)
                return enclosingType;

            //
            // "Cica<T>.Mica<TT>.Kutya" counts as generic, too: In open form it is returned as Cica<T>.Mica<TT>.Kutya<T, TT>
            // while in closed as "Cica<T>.Mica<TT>.Kutya<TConcrete1, TConcrete2>".
            //

            int gaCount = enclosingType.GetGenericArguments().Length;
            if (gaCount is 0)
                return enclosingType;

            Type[] gas = new Type[gaCount];
            Array.Copy(src.GetGenericArguments(), 0, gas, 0, gaCount);

            return enclosingType.MakeGenericType(gas);
        }

        /// <summary>
        /// Returns true if the given <see cref="Type"/> is nested.
        /// </summary>
        public static bool IsNested(this Type src) =>
            //
            // Types (for instance arrays) derived from embedded types are not embedded anymore.
            //

            (src.GetInnermostElementType() ?? src).IsNested;

        /// <summary>
        /// Returns true if the given <see cref="Type"/> represents a class and not a generic parameter.
        /// </summary>
        public static bool IsClass(this Type src) => !src.IsGenericParameter && src.IsClass;

        /// <summary>
        /// Returns true if the given <see cref="Type"/> is abstract.
        /// </summary>
        public static bool IsAbstract(this Type src) => src.IsAbstract && !src.IsSealed; // IL representation of static classes are "sealed abstract"

        /// <summary>
        /// Enumerates the methods defined on the given <see cref="Type"/>
        /// </summary>
        public static IEnumerable<MethodInfo> ListMethods(this Type src, bool includeStatic = false) => src
            .ListMembersInternal
            (
                static (t, f) => t.GetMethods(f),
                static m => m,
                includeStatic
            )
            .Where(static m => !m.IsSpecialName);

        /// <summary>
        /// Enumerates the properties defined on the given <see cref="Type"/>
        /// </summary>
        public static IEnumerable<PropertyInfo> ListProperties(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            static (t, f) => t.GetProperties(f),
            static prop =>
            {
                if (prop.GetMethod is null)
                    return prop.SetMethod;

                if (prop.SetMethod is null)
                    return prop.GetMethod;

                //
                // Higher visibility has the precedence
                //

                return prop.GetMethod.GetAccessModifiers() > prop.SetMethod.GetAccessModifiers()
                    ? prop.GetMethod
                    : prop.SetMethod;
            },
            includeStatic
        );

        /// <summary>
        /// Enumerates the events defined on the given <see cref="Type"/>
        /// </summary>
        public static IEnumerable<EventInfo> ListEvents(this Type src, bool includeStatic = false) => src.ListMembersInternal
        (
            static (t, f) => t.GetEvents(f),
            
            //
            // Events always have Add & Remove method declared
            //

            static e => e.AddMethod,
            includeStatic
        );

        /// <summary>
        /// The core member enumerator. It searches the whole hierarchy and includes explicitly implemented interface members as well.
        /// </summary>
        private static IEnumerable<TMember> ListMembersInternal<TMember>
        (
            this Type src, 
            Func<Type, BindingFlags, TMember[]> getter, 
            Func<TMember, MethodInfo> getUnderlyingMethod,
            bool includeStatic
        ) where TMember: MemberInfo
        {
            if (src.IsGenericParameter)
                yield break;

            BindingFlags flags = 
                BindingFlags.Public |

                //
                // BindingFlags.FlattenHierarchy returns public and protected members only. Unfortunately explicit
                // implementations are private.
                // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.bindingflags?view=net-9.0#fields
                //

                //BindingFlags.FlattenHierarchy |

                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly;

            //
            // As of NET6_0 we may declare static members on interfaces
            //

            if (includeStatic)
                flags |= BindingFlags.Static;

            if (src.IsInterface)
                foreach (TMember member in GetMembers())
                    yield return member;
            else
            {
                HashSet<MethodInfo> overriddenMethods = [];

                //
                // Order matters: we're processing the hierarchy towards the ancestor
                //

                foreach (TMember member in GetMembers())
                {
                    MethodInfo underlyingMethod = getUnderlyingMethod(member);

                    //
                    // When we encounter a virtual method, return only the last override
                    //

                    if (underlyingMethod.GetOverriddenMethod() is MethodInfo overriddenMethod && !overriddenMethods.Add(overriddenMethod))
                        continue;

                    if (overriddenMethods.Contains(underlyingMethod))
                        continue;

                    //
                    // We don't want to return private members
                    //

                    if (underlyingMethod.GetAccessModifiers() > AccessModifiers.Private)
                        yield return member;
                }
            }

            IEnumerable<TMember> GetMembers() => src.GetHierarchy().SelectMany(t => getter(t, flags));
        }

        /// <summary>
        /// Returns the constructors declared by user code on the given <see cref="Type"/>.
        /// </summary>
        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type)
        {
            //
            // Array type really sucks: All its constructors are generated by the compiler so from symbols
            // they cannot be retrieved.
            //

            if (type.IsArray)
                yield break;

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                yield return ctor;
        }

        /// <summary>
        /// Enumerates all the base <see cref="Type"/>s.
        /// </summary>
        public static IEnumerable<Type> GetBaseTypes(this Type type) 
        {
            for (Type? baseType = type; (baseType = baseType!.GetBaseType()) is not null; )
                yield return baseType;
        }

        /// <summary>
        /// Returns all the interfaces, that were implemented or inherited by the current <see cref="Type"/>. Handles generic parameters properly.
        /// </summary>
        public static IEnumerable<Type> GetAllInterfaces(this Type type) => !type.IsGenericParameter
            ? type.GetInterfaces()
            : [];

        /// <summary>
        /// Returns the base of the given <see cref="Type"/>. Handles generic parameters properly.
        /// </summary>
        public static Type? GetBaseType(this Type src) => !src.IsGenericParameter
            ? src.BaseType
            : null;

        /// <summary>
        /// Returns the class or interface hierarchy starting from the current <see cref="Type"/>.
        /// </summary>
        public static IEnumerable<Type> GetHierarchy(this Type src)
        {
            yield return src;

            foreach (Type t in src.IsInterface ? src.GetAllInterfaces() : src.GetBaseTypes())
                yield return t;
        }

        /// <summary>
        /// Returns true if the given <see cref="Type"/> represents a delegate (for instance a <see cref="Func{TResult}"/> or <see cref="Action"/>).
        /// </summary>
        public static bool IsDelegate(this Type src) =>
            (src.GetInnermostElementType() ?? src).GetBaseTypes().Contains(typeof(Delegate)) && src != typeof(MulticastDelegate);

        /// <summary>
        /// Returns the generic arguments that are explicitly declared on the given <see cref="Type"/>. For instance
        /// <code>
        /// class Parent&lt;T&gt;
        /// {
        ///     class Child&lt;TT&gt; {}
        /// }
        /// typeof(Parent&lt;int&gt;.Child&lt;string&gt;).GetOwnGenericArguments() // [typeof(string)]
        /// </code>
        /// </summary>
        public static IEnumerable<Type> GetOwnGenericArguments(this Type src)
        {
            if (!src.IsGenericType)
                yield break;

            //
            // "Cica<T>.Mica<TT>.Kutya" counts as generic, too: In open form it is returned as Cica<T>.Mica<TT>.Kutya<T, TT>
            // while in closed as "Cica<T>.Mica<TT>.Kutya<TConcrete1, TConcrete2>".
            //

            Type[] 
                closedArgs = src.GetGenericArguments(),
                openArgs = (src = src.GetGenericTypeDefinition()).GetGenericArguments();

            for(int i = 0; i < openArgs.Length; i++)
            {
                Type openArg = openArgs[i];

                bool own = true;
                for (Type? parent = src; (parent = parent!.DeclaringType) is not null;)
                    //
                    // GetGenericArguments() will return empty array if "parent" is not generic
                    //

                    if (parent.GetGenericArguments().Any(arg => openArg.IsGenericParameter ? arg.IsGenericParameter && arg.Name == openArg.Name : arg == openArg))
                    {
                        own = false;
                        break;
                    }

                if (own) 
                    yield return closedArgs[i];
            } 
        }

        /// <summary>
        /// Associates <see cref="AccessModifiers"/> to the given <see cref="Type"/>.
        /// </summary>
        /// <remarks>Since this method may use reflection to determine the result, callers better cache the returned data</remarks>
        public static AccessModifiers GetAccessModifiers(this Type src)
        {
            src = src.GetInnermostElementType() ?? src;

            AccessModifiers am = src switch
            {
                { IsPublic: true, IsVisible: true } or { IsNestedPublic: true } => AccessModifiers.Public,
                { IsNestedFamily: true } => AccessModifiers.Protected,
                { IsNestedFamORAssem: true } => AccessModifiers.Protected | AccessModifiers.Internal,
                { IsNestedFamANDAssem: true }  => AccessModifiers.Protected | AccessModifiers.Private,
                { IsNestedAssembly: true } or { IsVisible: false, IsNested: false } => AccessModifiers.Internal,
                { IsNestedPrivate: true } => AccessModifiers.Private,
                _ => throw new InvalidOperationException(Resources.UNDETERMINED_ACCESS_MODIFIER)
            };

            if (src.IsGenericParameter)
                return am;

            //
            // Generic arguments may impact the visibility.
            //

            if (src.IsConstructedGenericType)
                foreach (Type ga in src.GetGenericArguments())
                    UpdateAm(ref am, ga);

            Type? enclosingType = src.GetEnclosingType();
            if (enclosingType is not null)
                UpdateAm(ref am, enclosingType);

            return am;

            static void UpdateAm(ref AccessModifiers am, Type t)
            {
                AccessModifiers @new = t.GetAccessModifiers();
                if (@new < am)
                    am = @new;
            }
        }

        /// <summary>
        /// Associates <see cref="RefType"/> to the given <see cref="Type"/>.
        /// </summary>
        /// <remarks>Since this method may inspect attributes to determine the result, callers better cache the returned data.</remarks>
        public static RefType GetRefType(this Type src) => src switch
        {
#if NETSTANDARD2_1_OR_GREATER
            { IsByRefLike: true }
#else
            _ when src
                    .GetCustomAttributes()
                    .Select(static ca => ca.GetType().FullName)
                    .Contains("System.Runtime.CompilerServices.IsByRefLikeAttribute", StringComparer.OrdinalIgnoreCase)
#endif
                => RefType.Ref, // ref struct
            { IsPointer: true } => RefType.Pointer,
            { IsArray: true } => RefType.Array,
            _ => RefType.None
        };

        //
        // IsFunctionPointer is available in net8.0+ only
        //

        private static Func<Type, bool> GetIsFunctionPointerCore()
        {
            PropertyInfo? prop = typeof(Type).GetProperty("IsFunctionPointer");
            ParameterExpression type = Expression.Parameter(typeof(Type), nameof(type));
            return Expression.Lambda<Func<Type, bool>>
            (
                body: prop is null ? Expression.Constant(false) : Expression.Property(type, prop),
                type
            ).Compile();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Func<Type, bool> FIsFunctionPointerCore = GetIsFunctionPointerCore();

        /// <summary>
        /// Returns true if the given <see cref="Type"/> is a function pointer.
        /// </summary>
        public static bool IsFunctionPointer(this Type src) => FIsFunctionPointerCore(src);

        /// <summary>
        /// Returns the generic constraints associated with the given generic parameter.
        /// </summary>
        /// <param name="src">The generic parameter</param>
        /// <param name="declaringMember">Member on which the generic parameter is declared.</param>
        public static IEnumerable<Type> GetGenericConstraints(this Type src, MemberInfo declaringMember)
        {
            //
            // We can't query the declaring type using the src as the DeclaringType property never
            // returns specialized generic.
            //
            // Type declaringType = src.DeclaringMethod?.DeclaringType ?? src.DeclaringType;
            //

            Type declaringType = declaringMember switch
            {
                MethodInfo method => method.DeclaringType,
                Type type => type.DeclaringType,
                _ => throw new InvalidOperationException()
            };

            foreach (Type gpc in src.GetGenericParameterConstraints())
            {      
                //
                // We don't want a
                //     "where TT : struct, global::System.ValueType"
                //

                if (gpc == typeof(ValueType) && src.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                    continue;

                if (declaringType.IsConstructedGenericType is true)
                {
                    //
                    // Get the specialized constraint from the declaring member
                    // (note that gpc.DeclaringXxX always returns the generic definition)
                    //

                    int position = declaringType.GetGenericTypeDefinition().GetGenericArguments().IndexOf(gpc);
                    if (position >= 0)
                    {
                        yield return declaringType.GetGenericArguments()[position];
                        continue;
                    }
                }

                yield return gpc;
            }
        }

        /// <summary>
        /// Returns the index of the given generic parameter:
        /// <code>
        /// class Foo&lt;T, TT&gt; {}
        /// typeof(Foo&lt;T, TT&gt;).GetGenericArguments()[1].GetGenericParameterIndex() // == 1
        /// </code>
        /// </summary>
        public static int GetGenericParameterIndex(this Type src)
        {
            if (!src.IsGenericParameter)
                return 0;

            return src.DeclaringMethod is not null
                ? GetIndex(src.DeclaringMethod.GetGenericArguments(), src)
                : GetIndex(src.DeclaringType.GetGenericArguments(), src) * -1;

            static int GetIndex(IEnumerable<Type> gas, Type src)
            {
                int result = gas.Select(static t => t.Name).IndexOf(src.Name);
                Debug.Assert(result >= 0);

                return result + 1;
            }
        }

        /// <summary>
        /// Checks the given <see cref="Type"/>s for equality. Handles generic parameters properly.
        /// </summary>
        public static bool EqualsTo(this Type src, Type that)
        {
            if (!GetBasicProps(src).Equals(GetBasicProps(that)))
                return false;

            Type? srcElement = src.GetElementType();
            if (srcElement is not null)
            {
                Type? thatElement = that.GetElementType();
                return thatElement is not null && srcElement.EqualsTo(thatElement);
            }

            if (src.IsGenericType)
                return 
                    that.IsGenericType && 
                    src.GetGenericTypeDefinition().Equals(that.GetGenericTypeDefinition()) && 
                    src.GetGenericArguments().SequenceEqual(that.GetGenericArguments(), TypeComparer.Instance);

            if (src.IsGenericParameter)
                return that.IsGenericParameter && src.GetGenericParameterIndex() == that.GetGenericParameterIndex();
                
            return src == that;

            static object GetBasicProps(Type t) => new
            {
                t.IsPrimitive,
                t.IsPointer,
                t.IsEnum,
                t.IsValueType,
                t.IsArray,
                t.IsByRef
            };
        }
    }
}
