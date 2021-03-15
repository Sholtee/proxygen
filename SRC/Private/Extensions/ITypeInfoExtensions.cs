/********************************************************************************
* ITypeInfoExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
        public static string GetMD5HashCode(this ITypeInfo src)
        {
            using MD5 md5 = MD5.Create();

            Hash(src, md5);

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

                    qualifiedName ??= t.RefType.ToString();
                }

                if (qualifiedName is null)
                    return;

                byte[] inputBuffer = Encoding.UTF8.GetBytes(qualifiedName);

                transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, inputBuffer, 0);

                if (t is IGenericTypeInfo generic)
                    foreach (ITypeInfo ga in generic.GenericArguments)
                    {
                        Hash(ga, transform);
                    }
            }
        }

        public static bool EqualsTo(this ITypeInfo src, ITypeInfo that)
        {
            if (src.IsGenericParameter)
                return src.GetGenericParameterIndex() == that.GetGenericParameterIndex();

            if (src.ElementType is not null)
                //
                // Tombbel v mutatoval van dolgunk. A FullName property-t itt nem vizsgalhatjuk
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
                IGenericTypeInfo type => GetIndex(type),
                IGenericMethodInfo method => GetIndex(method) * -1,  // ha a parameter metoduson van definialva akkor negativ szam
                _ => 0
            };

            int GetIndex(IGeneric generic) => generic
                .GenericArguments
                .Select(ga => ga.Name)
                .IndexOf(src.Name) + 1 ?? 0; // (int?) null + 1 == null 
        }

        public static IEnumerable<IConstructorInfo> GetPublicConstructors(this ITypeInfo src)
        {
            IEnumerable<IConstructorInfo> ctors = src.Constructors.Where(ctor => ctor.AccessModifiers == AccessModifiers.Public);

            if (!ctors.Any())
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NO_PUBLIC_CTOR, src.QualifiedName));

            return ctors;
        }

        private static IEnumerable<ITypeInfo> IterateOn(this ITypeInfo src, Func<ITypeInfo, ITypeInfo?> selector) 
        {
            for (ITypeInfo? type = selector(src); type != null; type = selector(type))
                yield return type;
        }

        public static IEnumerable<ITypeInfo> GetBaseTypes(this ITypeInfo src) => src.IterateOn(x => x.BaseType);

        public static IEnumerable<ITypeInfo> GetEnclosingTypes(this ITypeInfo src) => src.IterateOn(x => x.EnclosingType);

        public static IEnumerable<ITypeInfo> GetParentTypes(this ITypeInfo src) => src.GetEnclosingTypes().Reverse();

        public static ITypeSymbol ToSymbol(this ITypeInfo src, Compilation compilation)
        {
            INamedTypeSymbol? symbol;

            if (src.GetEnclosingTypes().Any())
            {
                int arity = (src as IGenericTypeInfo)?.GenericArguments?.Count ?? 0;

                symbol = ToSymbol(src.GetParentTypes().Last(), compilation)
                    .GetTypeMembers(src.Name, arity)
                    .Single();
            }
            else
            {
                //
                // Tombot es mutatot nem lehet lekerdezni nev alapjan
                //

                switch (src.RefType)
                {
                    case RefType.Array:
                        IArrayTypeInfo ar = (IArrayTypeInfo)src;
                        return compilation.CreateArrayTypeSymbol(ToSymbol(ar.ElementType!, compilation), ar.Rank);
                    case RefType.Pointer:
                        return compilation.CreatePointerTypeSymbol(ToSymbol(src.ElementType!, compilation));
                }

                //
                // A GetTypeByMetadataName() nem mukodik lezart generikusokra, de ez nem is gond
                // mert a FullName a nyilt generikus tipushoz tartozo nevet adja vissza
                //

                symbol = compilation.GetTypeByMetadataName(src.QualifiedName ?? throw new NotSupportedException());

                if (symbol is null)
                {
                    var ex = new TypeLoadException(string.Format(Resources.Culture, Resources.TYPE_NOT_FOUND, src.QualifiedName));

                    //
                    // SourceGenerator-ba nem lehet beleDEBUGolni ezert...
                    //

                    ex.Data["containingAsm"] = src.DeclaringAssembly?.Name;
                    ex.Data["references"] = string.Join($",{Environment.NewLine}", compilation.References.Select(@ref => @ref.Display));

                    throw ex;
                }
            }

            if (src is IGenericTypeInfo generic && !generic.IsGenericDefinition)
            {
                ITypeSymbol[] gaSymbols = generic
                    .GenericArguments
                    .Select(ga => ToSymbol(ga, compilation))
                    .ToArray();

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
                Type[] gas = src
                    .GetParentTypes()
                    .Append(src)
                    .OfType<IGenericTypeInfo>()
                    .SelectMany(g => g.GenericArguments.Select(ToMetadata))
                    .ToArray();

                return queried.MakeGenericType(gas);
            }

            return queried;
        }
    }
}
