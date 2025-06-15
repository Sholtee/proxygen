/********************************************************************************
* ITypeInfoExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class ITypeInfoExtensions
    {
        //
        // Generic arguments have no impact on "GUID" property
        //

        /// <summary>
        /// Calculates the MD5 hash code of the given type.
        /// </summary>
        public static string GetMD5HashCode(this ITypeInfo src) => new ITypeInfo[] { src }.GetMD5HashCode();

        /// <summary>
        /// Calculates the combined MD5 hash code of the given types.
        /// </summary>
        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
        public static string GetMD5HashCode(this IEnumerable<ITypeInfo> types)
        {
            using MD5 md5 = MD5.Create();

            foreach (ITypeInfo type in types)
                type.Hash(md5);

            return md5.Stringify("X2");         
        }

        /// <summary>
        /// Calculates the hash code of the given type using custom transformation.
        /// </summary>
        public static void Hash(this ITypeInfo src, ICryptoTransform transform)
        {
            if (src.Flags.HasFlag(TypeInfoFlags.IsGenericParameter))
                //
                // class Foo<T> { void Method(T x); } | class Foo {void Method<T>(T x); }
                //

                transform.Update($"generic:{src.GetGenericParameterIndex()}");

            else if (src.RefType > RefType.None)
            {
                transform.Update(src.RefType.ToString());

                //
                // ref structs have no element type
                //

                src.ElementType?.Hash(transform);    
            }

            if (src is IGenericTypeInfo generic)
                foreach (ITypeInfo ga in generic.GenericArguments)
                    ga.Hash(transform);

            if (src.QualifiedName is not null)
                transform.Update(src.QualifiedName);
        }

        /// <summary>
        /// Determines the equality of the given two types.
        /// </summary>
        public static bool EqualsTo(this ITypeInfo src, ITypeInfo that)
        {
            if (src.Flags.HasFlag(TypeInfoFlags.IsGenericParameter))
                return src.GetGenericParameterIndex() == that.GetGenericParameterIndex();

            if (src.ElementType is null != that.ElementType is null)
                return false;

            if (src.ElementType is not null)
                //
                // Arrays and pointers have no QualifiedName property.
                //

                return
                    src.RefType == that.RefType &&
                    src.ElementType.EqualsTo(that.ElementType!);

            if (src.QualifiedName != that.QualifiedName)
                return false;

            //
            // Qualified name contains the arity, too so we don't need to double check that
            //

            if (src is IGenericTypeInfo genericSrc)
            {
                IGenericTypeInfo genericThat = (IGenericTypeInfo) that;

                for (int i = 0; i < genericSrc.GenericArguments.Count; i++)
                    if (!genericSrc.GenericArguments[i].EqualsTo(genericThat.GenericArguments[i]))
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the index of the given generic parameter similar to <see cref="TypeExtensions.GetGenericParameterIndex(Type)"/>:
        /// <code>
        /// class Foo&lt;T, TT&gt; {}
        /// typeof(Foo&lt;T, TT&gt;).GetGenericArguments()[1].GetGenericParameterIndex() // == 1
        /// </code>
        /// </summary>
        public static int GetGenericParameterIndex(this ITypeInfo src) 
        {
            if (!src.Flags.HasFlag(TypeInfoFlags.IsGenericParameter))
                return 0;

            return src.ContainingMember switch
            {
                IGenericTypeInfo type => GetIndex(src, type),
                IGenericMethodInfo method => GetIndex(src, method) * -1,  // return a negative value for parameters defined on methods
                _ => 0
            };

            static int GetIndex<T>(ITypeInfo src, IGeneric<T> generic) where T: IGeneric<T>
            {
                src = src.GetInnermostElementType() ?? src;

                int result = generic.GenericArguments.Select(static ga => ga.Name).IndexOf(src.Name);
                Debug.Assert(result >= 0, $"Generic parameter with name '{src.Name}' must be included in GenericArguments list");

                return result + 1;
            }
        }

        /// <summary>
        /// Gets the inner most element type in case of pointer types. For instance int*[] this method returns the <see cref="ITypeInfo"/> for the <see cref="int"/> type.
        /// </summary>
        public static ITypeInfo? GetInnermostElementType(this ITypeInfo src)
        {
            ITypeInfo? prev = null;

            for (ITypeInfo? current = src; (current = current!.ElementType) is not null;)
                prev = current;

            return prev;
        }

        /// <summary>
        /// Gets the constructors associated with the given type. Takes the implicit default constructor into account if necessary.
        /// </summary>
        /// <exception cref="InvalidOperationException">In case there is no accessible constructor</exception>
        public static IEnumerable<IConstructorInfo> GetConstructors(this ITypeInfo src, AccessModifiers minAccessibility)
        {
            //
            // Return the default constructor from the base if available
            //

            if (src.Constructors.Count is 0)
            {
                IEnumerable<IConstructorInfo> defaultCtor = src.BaseType!.GetConstructors(AccessModifiers.Protected);
                Debug.Assert(defaultCtor.Count() is 1, "A type cannot have more than one default constructor");
                Debug.Assert(defaultCtor.Single().Parameters.Count is 0, "Default constructor cannot have arguments");

                yield return defaultCtor.Single();
            }
            else
            {
                int found = 0;

                foreach (IConstructorInfo ctor in src.Constructors)
                    if (ctor.AccessModifiers >= minAccessibility)
                    {
                        yield return ctor;
                        found++;
                    }

                if (found is 0)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NO_ACCESSIBLE_CTOR, src.QualifiedName));
            }
        }

        private static IEnumerable<ITypeInfo> IterateOn(this ITypeInfo src, Func<ITypeInfo, ITypeInfo?> selector) 
        {
            for (ITypeInfo? type = selector(src); type is not null; type = selector(type))
                yield return type;
        }

        /// <summary>
        /// Gets the base types of the given type
        /// </summary>
        public static IEnumerable<ITypeInfo> GetBaseTypes(this ITypeInfo src) => src.IterateOn(static x => x.BaseType);

        /// <summary>
        /// Gets the enclosing types of the given nested type.
        /// </summary>
        public static IEnumerable<ITypeInfo> GetEnclosingTypes(this ITypeInfo src) => src.IterateOn(static x => x.EnclosingType);

        /// <summary>
        /// Returns the parent types if the given nested type. Similar to the <see cref="GetEnclosingTypes(ITypeInfo)"/> method but it returns the result in reverse order.
        /// </summary>
        public static IEnumerable<ITypeInfo> GetParentTypes(this ITypeInfo src) => src.GetEnclosingTypes().Reverse();

        /// <summary>
        /// Returns true if <paramref name="src"/> is a descendant type of <paramref name="type"/> or <paramref name="type"/> is an interface and <paramref name="src"/> implements it.
        /// </summary>
        public static bool IsAccessibleFrom(this ITypeInfo src, ITypeInfo type) =>
            type.EqualsTo(src) ||
            type.Interfaces.Any(iface => iface.EqualsTo(src)) ||
            type.GetBaseTypes().Any(baseType => baseType.EqualsTo(src));

        /// <summary>
        /// Converts the given <see cref="ITypeInfo"/> to Roslyn's <see cref="ITypeSymbol"/> interface.
        /// </summary>
        public static ITypeSymbol ToSymbol(this ITypeInfo src, Compilation compilation)
        {
            INamedTypeSymbol? symbol;

            if (src.EnclosingType is not null)
            {
                int arity = (src as IGenericTypeInfo)?.GenericArguments?.Count ?? 0;

                symbol = src
                    .EnclosingType
                    .ToSymbol(compilation)
                    .GetTypeMembers(src.Name, arity)
                    .Single();
            }
            else
            {
                //
                // Array and pointer cannot be queried by name
                //

                switch (src.RefType)
                {
                    case RefType.Array:
                        IArrayTypeInfo ar = (IArrayTypeInfo) src;
                        return compilation.CreateArrayTypeSymbol(ToSymbol(ar.ElementType!, compilation), ar.Rank);
                    case RefType.Pointer:
                        return compilation.CreatePointerTypeSymbol(ToSymbol(src.ElementType!, compilation));
                }

                //
                // QualifiedName doesn't reflect generic arguments so it can be passed to GetTypeByMetadataName()
                //

                symbol = compilation.GetTypeByMetadataName(src.QualifiedName ?? throw new NotSupportedException());

                if (symbol is null)
                {
                    TypeLoadException ex = new(string.Format(Resources.Culture, Resources.TYPE_NOT_FOUND, src.QualifiedName));

                    //
                    // It's hard to debug a source generator so...
                    //

                    ex.Data["containingAsm"] = src.DeclaringAssembly?.Name;
                    ex.Data["references"] = string.Join($",{Environment.NewLine}", compilation.References.Select(static @ref => @ref.Display));

                    throw ex;
                }
            }

            if (src is IGenericTypeInfo generic && !generic.IsGenericDefinition)
            {
                ITypeSymbol[] gaSymbols = [.. generic.GenericArguments.Select(ga => ToSymbol(ga, compilation))];
                return symbol.Construct(gaSymbols);
            }

            return symbol;
        }

        /// <summary>
        /// Converts the given <see cref="ITypeInfo"/> to <see cref="Type"/> instance.
        /// </summary>
        public static Type ToMetadata(this ITypeInfo src)
        {
            //
            // QualifiedName doesn't reflect generic arguments so can be used together with GetType()
            //

            Type queried = Type.GetType(src.AssemblyQualifiedName, throwOnError: true);

            if (src is IGenericTypeInfo generic && generic.IsGenericDefinition)
                return queried;

            if (queried.IsGenericType)
            {
                List<Type> gas = []; 

                foreach (ITypeInfo parent in src.GetParentTypes())
                    ReadGenericArguments(parent, gas);

                ReadGenericArguments(src, gas);

                return queried.MakeGenericType(gas.ToArray());
            }

            return queried;

            static void ReadGenericArguments(ITypeInfo src, IList<Type> gas)
            {
                if (src is not IGenericTypeInfo generic)
                    return;

                foreach (ITypeInfo ga in generic.GenericArguments)
                    gas.Add
                    (
                        ga.ToMetadata()
                    );
            }
        }
    }
}
