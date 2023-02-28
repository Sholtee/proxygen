/********************************************************************************
* ITypeInfoExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class ITypeInfoExtensions
    {
        //
        // A "GUID" property generikus tipus lezart es nyitott valtozatanal ugyanaz
        //

        public static string GetMD5HashCode(this ITypeInfo src) => GetMD5HashCode(types: src);

        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
        public static string GetMD5HashCode(params ITypeInfo[] types)
        {
            using MD5 md5 = MD5.Create();

            for (int i = 0; i < types.Length; i++)
            {
                Hash(types[i], md5);
            }

            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            StringBuilder sb = new();

            for (int i = 0; i < md5.Hash.Length; i++)
            {
                sb.Append(md5.Hash[i].ToString("X2", null));
            }

            return sb.ToString();

            static void Hash(ITypeInfo t, ICryptoTransform transform)
            {
                string? qualifiedName = t.QualifiedName;

                if (t.RefType > RefType.None)
                {
                    Hash(t.ElementType!, transform);

                    qualifiedName += t.RefType.ToString();
                }

                if (qualifiedName is null)
                    return;

                byte[] inputBuffer = Encoding.UTF8.GetBytes(qualifiedName);

                transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, inputBuffer, 0);

                if (t is not IGenericTypeInfo generic)
                    return;

                for (int i = 0; i < generic.GenericArguments.Count; i++)
                {
                    Hash(generic.GenericArguments[i], transform);
                }
            }
        }

        public static bool EqualsTo(this ITypeInfo src, ITypeInfo that)
        {
            if (src.IsGenericParameter)
                return src.GetGenericParameterIndex() == that.GetGenericParameterIndex();

            if (src.ElementType is null != that.ElementType is null)
                return false;

            if (src.ElementType is not null)
                //
                // Tombbel v mutatoval van dolgunk. A QualifiedName property-t itt nem vizsgalhatjuk
                // mert mindket esetben NULL lesz.
                //

                return
                    src.RefType == that.RefType &&
                    src.ElementType.EqualsTo(that.ElementType!);

            if (src.QualifiedName != that.QualifiedName)
                return false;

            //
            // Ha a nevuk ugyanaz akkor az aritasuk is
            //

            if (src is IGenericTypeInfo genericSrc)
            {
                IGenericTypeInfo genericThat = (IGenericTypeInfo) that;

                for (int i = 0; i < genericSrc.GenericArguments.Count; i++)
                {
                    if (!genericSrc.GenericArguments[i].EqualsTo(genericThat.GenericArguments[i]))
                        return false;
                }
            }

            return true;
        }

        public static int GetGenericParameterIndex(this ITypeInfo src) 
        {
            if (!src.IsGenericParameter)
                return 0;

            return src.ContainingMember switch
            {
                IGenericTypeInfo type => GetIndex(src, type),
                IGenericMethodInfo method => GetIndex(src, method) * -1,  // ha a parameter metoduson van definialva akkor negativ szam
                _ => 0
            };

            static int GetIndex<T>(ITypeInfo src, IGeneric<T> generic) where T: IGeneric<T>
            {
                int index = 0;
                foreach (ITypeInfo ga in generic.GenericArguments)
                {
                    index++;
                    if (ga.Name == src.Name)
                        return index;
                }
                return 0;
            }
        }

        public static IEnumerable<IConstructorInfo> GetPublicConstructors(this ITypeInfo src)
        {
            int found = 0;
            foreach (IConstructorInfo ctor in src.Constructors)
            {
                if (ctor.AccessModifiers is AccessModifiers.Public)
                {
                    yield return ctor;
                    found++;
                }
            }
            if (found is 0)
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NO_PUBLIC_CTOR, src.QualifiedName));
        }

        private static IEnumerable<ITypeInfo> IterateOn(this ITypeInfo src, Func<ITypeInfo, ITypeInfo?> selector) 
        {
            for (ITypeInfo? type = selector(src); type is not null; type = selector(type))
                yield return type;
        }

        public static IEnumerable<ITypeInfo> GetBaseTypes(this ITypeInfo src) => src.IterateOn(x => x.BaseType);

        public static IEnumerable<ITypeInfo> GetEnclosingTypes(this ITypeInfo src) => src.IterateOn(x => x.EnclosingType);

        public static IEnumerable<ITypeInfo> GetParentTypes(this ITypeInfo src) => new Stack<ITypeInfo>(src.GetEnclosingTypes());

        public static bool IsAccessibleFrom(this ITypeInfo src, ITypeInfo type) =>
            type.EqualsTo(src) ||
            type.Interfaces.Some(iface => iface.EqualsTo(src)) ||
            type.GetBaseTypes().Some(baseType => baseType.EqualsTo(src));

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
                    .Single()!;
            }
            else
            {
                //
                // Tombot es mutatot nem lehet lekerdezni nev alapjan
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
                // A GetTypeByMetadataName() nem mukodik lezart generikusokra, de ez nem is gond
                // mert a QualifiedName a nyilt generikus tipushoz tartozo nevet adja vissza
                //

                symbol = compilation.GetTypeByMetadataName(src.QualifiedName ?? throw new NotSupportedException());

                if (symbol is null)
                {
                    TypeLoadException ex = new(string.Format(Resources.Culture, Resources.TYPE_NOT_FOUND, src.QualifiedName));

                    //
                    // SourceGenerator-ba nem lehet beleDEBUGolni ezert...
                    //

                    ex.Data["containingAsm"] = src.DeclaringAssembly?.Name;
                    ex.Data["references"] = string.Join($",{Environment.NewLine}", compilation.References.Convert(static @ref => @ref.Display));

                    throw ex;
                }
            }

            if (src is IGenericTypeInfo generic && !generic.IsGenericDefinition)
            {
                ITypeSymbol[] gaSymbols = generic
                    .GenericArguments
                    .ConvertAr(ga => ToSymbol(ga, compilation));

                return symbol.Construct(gaSymbols);
            }

            return symbol;
        }

        public static Type ToMetadata(this ITypeInfo src)
        {
            //
            // Az AssemblyQualifiedName a nyilt generikus tipushoz tartozo nevet adja vissza
            //

            Type queried = Type.GetType(src.AssemblyQualifiedName, throwOnError: true);

            if (src is IGenericTypeInfo generic && generic.IsGenericDefinition)
                return queried;

            if (queried.IsGenericType)
            {
                List<Type> gas = new(); 

                foreach (ITypeInfo parent in src.GetParentTypes())
                {
                    ReadGenericArguments(parent, gas);
                }

                ReadGenericArguments(src, gas);

                return queried.MakeGenericType(gas.ConvertAr(static ga => ga));

                static void ReadGenericArguments(ITypeInfo src, IList<Type> gas)
                {
                    if (src is not IGenericTypeInfo generic)
                        return;

                    foreach (ITypeInfo ga in generic.GenericArguments)
                    {
                         gas.Add
                         (
                             ga.ToMetadata()
                         );
                    }
                }
            }

            return queried;
        }
    }
}
