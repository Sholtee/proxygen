/********************************************************************************
* ITypeSymbolExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal static class ITypeSymbolExtensions
    {
        public static bool IsInterface(this ITypeSymbol src) => src.TypeKind is TypeKind.Interface;

        private static readonly IReadOnlyList<TypeKind> ClassTypes = new[] 
        {
            TypeKind.Class,
            TypeKind.Array,
            TypeKind.Delegate,
            TypeKind.Pointer
        };

        public static bool IsClass(this ITypeSymbol src) => ClassTypes.IndexOf(src.TypeKind) >= 0;

        private static readonly IReadOnlyList<TypeKind> SealedTypes = new[]
        {
            TypeKind.Array
        };

        public static bool IsFinal(this ITypeSymbol src) => src.IsSealed || src.IsStatic || SealedTypes.IndexOf(src.TypeKind) >= 0;

        public static ITypeSymbol? GetEnclosingType(this ITypeSymbol src)
        {
            src = src.GetElementType(recurse: true) ?? src;

            return !src.IsGenericParameter()
                ? src.ContainingType
                : null;
        }

        public static string GetFriendlyName(this ITypeSymbol src) => src switch
        {
            //
            // nint => System.IntPtr, (T Item1, TT item2) => System.Tuple<T, TT>
            //

            _ when src.IsTupleType || src.IsNativeIntegerType => src.ContainingNamespace.ToString() + Type.Delimiter + src.Name,
            _ when src is INamedTypeSymbol named && named.IsBoundNullable() => named.ConstructedFrom.GetFriendlyName(),
            _ when src is IPointerTypeSymbol pointer => pointer.PointedAtType.GetFriendlyName(),

            //
            // nint[,] => System.IntPtr[,]
            //

            _ when src is IArrayTypeSymbol array => $"{array.ElementType.GetFriendlyName()}[{(array.Rank - 1).Times(static () => ',').Join()}]",
            _ => src.ToDisplayString
            (
                new SymbolDisplayFormat
                (
                    typeQualificationStyle: src.IsNested()
                        ? SymbolDisplayTypeQualificationStyle.NameOnly
                        : SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
                )
            )
        };

        public static bool IsBoundNullable(this INamedTypeSymbol src) =>
            src.ConstructedFrom?.SpecialType is SpecialType.System_Nullable_T &&
            !SymbolEqualityComparer.Default.Equals(src.ConstructedFrom, src);

        public static bool IsGenericType(this ITypeSymbol src) => src is INamedTypeSymbol named && named.TypeArguments.Some();

        public static IEnumerable<ITypeSymbol> GetParents(this ITypeSymbol src) 
        {
            src = src.GetElementType(recurse: true) ?? src; // Array has no containing type but array item does

            for (ITypeSymbol parent = src; (parent = parent!.ContainingType) is not null;)
            {
                yield return parent;
            }
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol src)
        {
            return src.IsInterface()
                ? src.GetAllInterfaces()
                : GetBaseTypes();

            IEnumerable<ITypeSymbol> GetBaseTypes()
            {
                for (ITypeSymbol? baseType = src.BaseType; baseType is not null; baseType = baseType.BaseType)
                {
                    yield return baseType;
                }
            }
        }

        public static IEnumerable<ITypeSymbol> GetHierarchy(this ITypeSymbol src)
        {
            yield return src;

            foreach (ITypeSymbol baseType in src.GetBaseTypes())
            {
                yield return baseType;
            }
        }

        public static IEnumerable<IMethodSymbol> ListMethods(this ITypeSymbol src, bool includeStatic = false, bool skipSpecial = true)
        {
            IEnumerable<IMethodSymbol> methods = src.ListMembersInternal<IMethodSymbol>
            (
                static m => m,
                static m => m.GetOverriddenMethod(),
                includeStatic
            );

            if (skipSpecial)
                methods = methods.Convert
                (
                    static m => m,
                    static m => m.IsSpecial()
                );

            return methods;
        }

        public static IEnumerable<IPropertySymbol> ListProperties(this ITypeSymbol src, bool includeStatic = false)
        {
            return src.ListMembersInternal<IPropertySymbol>
            (
                GetUnderlyingMethod,
                static p => GetUnderlyingMethod(p).GetOverriddenMethod(),
                includeStatic
            );

            //
            // Higher visibility has the precendence
            //

            static IMethodSymbol GetUnderlyingMethod(IPropertySymbol prop)
            {
                if (prop.GetMethod is null)
                    return prop.SetMethod!;

                if (prop.SetMethod is null)
                    return prop.GetMethod;

                return prop.GetMethod.GetAccessModifiers() > prop.SetMethod.GetAccessModifiers()
                    ? prop.GetMethod
                    : prop.SetMethod;
            }
        }

        public static IEnumerable<IEventSymbol> ListEvents(this ITypeSymbol src, bool includeStatic = false)
        {
            return src.ListMembersInternal<IEventSymbol>
            (
                GetUnderlyingMethod,
                static e => GetUnderlyingMethod(e).GetOverriddenMethod(),
                includeStatic
            );

            //
            // Higher visibility has the precendence
            //

            static IMethodSymbol GetUnderlyingMethod(IEventSymbol evt)
            {
                if (evt.AddMethod is null)
                    return evt.RemoveMethod!;

                if (evt.RemoveMethod is null)
                    return evt.AddMethod;

                return evt.AddMethod.GetAccessModifiers() > evt.RemoveMethod.GetAccessModifiers()
                    ? evt.AddMethod
                    : evt.RemoveMethod;
            }
        }

        private static IEnumerable<TMember> ListMembersInternal<TMember>(
            this ITypeSymbol src, 
            Func<TMember, IMethodSymbol> getUnderlyingMethod,
            Func<TMember, IMethodSymbol?> getOverriddenMethod,
            bool includeStatic) where TMember : ISymbol
        {
            if (src.IsInterface())
            {
                foreach (ITypeSymbol t in src.GetHierarchy())
                {
                    foreach (ISymbol symbol in t.GetMembers())
                    {
                        if (symbol is TMember member)
                            yield return member;
                    }
                }
            }
            else
            {
                //
                // TODO: implement IMethodSymbolComparer
                //

                #pragma warning disable RS1024 // Compare symbols correctly
                HashSet<IMethodSymbol> overriddenMethods = new();
                #pragma warning restore RS1024

                //
                // Order matters: we're processing the hierarchy towards the ancestor
                //

                foreach (ITypeSymbol t in src.GetHierarchy())
                {
                    foreach (ISymbol symbol in t.GetMembers())
                    {
                        if (symbol is TMember member && (includeStatic || !member.IsStatic))
                        {
                            IMethodSymbol?
                                overriddenMethod = getOverriddenMethod(member),
                                underlyingMethod = getUnderlyingMethod(member);

                            if (overriddenMethod is not null)
                                overriddenMethods.Add(overriddenMethod);

                            if (overriddenMethods.Contains(underlyingMethod))
                                continue;

                            //
                            // If it was not yielded before (due to "new" or "override") and not private then we are fine.
                            //

                            if (underlyingMethod.GetAccessModifiers() > AccessModifiers.Private)
                                yield return member;
                        }
                    }
                }
            }
        }

        private static readonly IReadOnlyList<MethodKind> Ctors = new[]
        {
            MethodKind.Constructor, MethodKind.StaticConstructor
        };

        public static IEnumerable<IMethodSymbol> GetConstructors(this ITypeSymbol src)
        {
            //
            // Don't use ListMembersInternal() here as we don't need ctros from the ancestors.
            //

            foreach (ISymbol m in src.GetMembers())
            {
                if (m is not IMethodSymbol ctor || Ctors.IndexOf(ctor.MethodKind) is null || ctor.GetAccessModifiers() is AccessModifiers.Private || ctor.IsImplicitlyDeclared)
                    continue;

                yield return ctor;
            }
        }

        public static ITypeSymbol? GetElementType(this ITypeSymbol src, bool recurse = false)
        {
            ITypeSymbol? prev = null;

            for (ITypeSymbol? current = src; (current = (current as IArrayTypeSymbol)?.ElementType ?? (current as IPointerTypeSymbol)?.PointedAtType) != null;)
            {
                if (!recurse) return current;
                prev = current;
            }

            return prev;
        }

        public static string? GetAssemblyQualifiedName(this ITypeSymbol src)
        {
            //
            // Arrays and pointers have no contaning assembly
            //

            IAssemblySymbol? containingAsm = src.GetElementType(recurse: true)?.ContainingAssembly ?? src.ContainingAssembly;
            if (containingAsm is null)
                return null;

            return $"{src.GetQualifiedMetadataName()}, {containingAsm.Identity}";
        }

        //
        // Types (for instance arrays) derived from embedded types are no longer embedded. That's why this
        // GetElementType() magic.
        //

        public static bool IsNested(this ITypeSymbol src) => 
            (src.GetElementType(recurse: true)?.ContainingType ?? src.ContainingType) is not null && !src.IsGenericParameter();

        public static bool IsGenericParameter(this ITypeSymbol src)
        {
            src = src.GetElementType(recurse: true) ?? src;     
            /*
            return src.ContainingSymbol switch
            {
                IMethodSymbol method => method.TypeParameters.Some(tp => SymbolEqualityComparer.Default.Equals(tp, src)),
                INamedTypeSymbol type => type.TypeParameters.Some(tp => SymbolEqualityComparer.Default.Equals(tp, src)),
                _ => false
            };
            */
            return src.TypeKind is TypeKind.TypeParameter;
        }

        public static string GetQualifiedMetadataName(this ITypeSymbol src)
        {
            ITypeSymbol? elementType = src.GetElementType(recurse: true);

            StringBuilder sb = new();

            INamespaceSymbol? ns = elementType?.ContainingNamespace ?? src.ContainingNamespace;  // Pointers and arrays have no containing namespace in compile time

            if (ns is not null && !ns.IsGlobalNamespace)
            {
                sb.Append(ns);
                sb.Append(Type.Delimiter);
            }

            foreach (ITypeSymbol parent in new Stack<ITypeSymbol>(src.GetParents()))
            {
                sb.Append($"{GetName(parent)}+");
            }

            sb.Append($"{GetName(elementType ?? src)}");

            return sb.ToString();

            static string GetName(ITypeSymbol type) 
            {
                string name = !string.IsNullOrEmpty(type.Name)
                    ? type.Name // tupple es nullable eseten is helyes nevet ad vissza
                    : type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly));

                return type is INamedTypeSymbol named && named.IsGenericType()
                    ? $"{name}`{named.Arity}"
                    : name;
            }
        }

        public static IEnumerable<ITypeSymbol> GetAllInterfaces(this ITypeSymbol src) 
        {
            //
            // AllInterfaces may contain the same closed geneic interface more times if the generic arguments are
            // differ in Nullable annotations only:
            //
            // interface IA: IB, IC<string> {}, interface IB: IC<string?> -> IC<string> will be returned twice
            //

            #pragma warning disable RS1024 // Compare symbols correctly
            HashSet<INamedTypeSymbol> returnedSymbols = new(SymbolEqualityComparer.Default);
            #pragma warning restore RS1024

            foreach (INamedTypeSymbol t in src.AllInterfaces)
            {
                INamedTypeSymbol iface = t;

                if (iface.IsGenericType())
                {
                    ITypeSymbol[] tas = iface.TypeArguments.ConvertAr
                    (
                        static ta => !ta.IsValueType
                            //
                            // Drop nullable annotation (int? -> Nullable<int>, object? -> [Nullable] object)
                            //

                            ? ta.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                            : ta
                    );

                    iface = iface.OriginalDefinition.Construct(tas);
                }

                if (returnedSymbols.Add(iface))
                    yield return iface;
            }
        }

        public static string GetDebugString(this ITypeSymbol src, string? eol = null) 
        {
            SymbolDisplayFormat fmt = new
            (
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeRef,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

            eol ??= Environment.NewLine;

            StringBuilder sb = new StringBuilder().Append(src.ToDisplayString(fmt));

            List<ITypeSymbol> bases = new();
            if (src.BaseType is not null)
                bases.Add(src.BaseType);
            bases.AddRange(src.AllInterfaces);

            if (bases.Some())
                sb.Append($": {string.Join(", ", bases.Convert(@base => @base.ToDisplayString(fmt)))}");

            sb.Append($"{eol}{{");

            foreach (IMethodSymbol method in src.ListMethods(includeStatic: true).Convert(static m => m, static m => m.IsSpecial()))
                sb.Append($"{eol}  {method.ToDisplayString(fmt)};");

            foreach (IPropertySymbol property in src.ListProperties(includeStatic: true))
                sb.Append($"{eol}  {property.ToDisplayString(fmt)}");

            foreach (IEventSymbol evt in src.ListEvents(includeStatic: true))
                sb.Append($"{eol}  {evt.ToDisplayString(fmt)}");

            sb.Append($"{eol}}}");

            return sb.ToString();
        }
        public static AccessModifiers GetAccessModifiers(this ITypeSymbol src)
        {
            src = src.GetElementType(recurse: true) ?? src;

            AccessModifiers am = src.DeclaredAccessibility switch
            {
                Accessibility.Protected => AccessModifiers.Protected,
                Accessibility.Internal => AccessModifiers.Internal,
                Accessibility.ProtectedOrInternal => AccessModifiers.Protected | AccessModifiers.Internal,
                Accessibility.ProtectedAndInternal => AccessModifiers.Protected | AccessModifiers.Private,
                Accessibility.Public => AccessModifiers.Public,
                Accessibility.Private => AccessModifiers.Private,
                Accessibility.NotApplicable => AccessModifiers.Public, // TODO: FIXME
                #pragma warning disable CA2201 // In theory we should never reach here.
                _ => throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER)
                #pragma warning restore CA2201
            };

            if (src is INamedTypeSymbol namedType)
            {
                foreach (ITypeSymbol ta in namedType.TypeArguments)
                {
                    if (ta.IsGenericParameter())
                        continue;

                    AccessModifiers gaAm = ta.GetAccessModifiers();
                    if (gaAm < am)
                        am = gaAm;
                }
            }

            if (!src.IsGenericParameter() && src.ContainingType is not null)
            {
                AccessModifiers ctAm = src.ContainingType.GetAccessModifiers();
                if (ctAm < am)
                    am = ctAm;
            }

            return am;
        }
    }
}
