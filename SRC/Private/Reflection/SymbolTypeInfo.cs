/********************************************************************************
* SymbolTypeInfo.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal class SymbolTypeInfo : ITypeInfo
    {
        protected ITypeSymbol UnderlyingSymbol { get; }

        protected Compilation Compilation { get; }

        private SymbolTypeInfo(ITypeSymbol typeSymbol, Compilation compilation)
        {
            UnderlyingSymbol = typeSymbol;
            Compilation = compilation;
        }
        public static ITypeInfo CreateFrom(ITypeSymbol typeSymbol, Compilation compilation) => typeSymbol switch
        {
            IArrayTypeSymbol array => new SymbolArrayTypeInfo(array, compilation),
            INamedTypeSymbol named when named.TypeArguments.Any() /*ne IsGenericType legyen*/ => new SymbolGenericTypeInfo(named, compilation),
            _ => new SymbolTypeInfo(typeSymbol, compilation)
        };

        private IAssemblyInfo? FDeclaringAssembly;
        public IAssemblyInfo? DeclaringAssembly
        {
            get
            {
                if (FDeclaringAssembly is null)
                {
                    IAssemblySymbol? asm = UnderlyingSymbol.GetElementType(recurse: true)?.ContainingAssembly ?? UnderlyingSymbol.ContainingAssembly;
                    if (asm != null) 
                        FDeclaringAssembly = SymbolAssemblyInfo.CreateFrom(asm, Compilation);
                }
                return FDeclaringAssembly;
            }
        }

        public bool IsVoid => UnderlyingSymbol.SpecialType == SpecialType.System_Void;

        public RefType RefType => UnderlyingSymbol switch
        {
            IPointerTypeSymbol => RefType.Pointer,
            _ => RefType.None
        };

        public bool IsNested => UnderlyingSymbol.IsNested();

        public bool IsGenericParameter => UnderlyingSymbol.IsGenericParameter();

        public bool IsInterface => UnderlyingSymbol.IsInterface();

        public string? AssemblyQualifiedName => !IsGenericParameter ? UnderlyingSymbol.GetAssemblyQualifiedName() : null;

        public string? FullName => !IsGenericParameter ? UnderlyingSymbol.GetQualifiedMetadataName() : null;

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType
        {
            get
            {
                if (FElementType == null)
                {
                    ITypeSymbol? realType = UnderlyingSymbol.GetElementType();

                    if (realType != null)
                        FElementType = CreateFrom(realType, Compilation);
                }
                return FElementType;
            }
        }

        private IReadOnlyList<ITypeInfo>? FEnclosingTypes;
        public IReadOnlyList<ITypeInfo> EnclosingTypes => FEnclosingTypes ??= UnderlyingSymbol
            .GetEnclosingTypes()
            .Select(ti => CreateFrom(ti, Compilation))
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingSymbol
            .AllInterfaces
            .Select(ti => CreateFrom(ti, Compilation))
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FBases;
        public IReadOnlyList<ITypeInfo> Bases => FBases ??= UnderlyingSymbol
            .GetBaseTypes()
            .Select(ti => CreateFrom(ti, Compilation))
            .ToArray();

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingSymbol
            //
            // Tomboknel az explicit interfacek implementaciok nem jatszanak 
            // (reflexio nem adja oket vissza, GetInterfaceMap() kivetelt dob).
            //

            .ListMembers<IPropertySymbol>(includeNonPublic: UnderlyingSymbol is not IArrayTypeSymbol /*explicit*/, includeStatic: true)
            .Select(p => SymbolPropertyInfo.CreateFrom(p, Compilation))
            .ToArray();

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingSymbol
            .ListMembers<IEventSymbol>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Select(evt => SymbolEventInfo.CreateFrom(evt, Compilation))
            .ToArray();

        private static readonly IReadOnlyList<MethodKind> RegularMethods = new[] 
        {
            MethodKind.Ordinary,
            MethodKind.ExplicitInterfaceImplementation,
            MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.EventRaise,
            MethodKind.PropertyGet, MethodKind.PropertySet,
            MethodKind.UserDefinedOperator
        };

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingSymbol
            .ListMembers<IMethodSymbol>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Where(m => RegularMethods.Contains(m.MethodKind) && m.GetAccessModifiers() != AccessModifiers.Private)
            .Select(m => SymbolMethodInfo.CreateFrom(m, Compilation))
            .ToArray();

        private static readonly IReadOnlyList<MethodKind> Ctors = new[] 
        {
            MethodKind.Constructor, MethodKind.StaticConstructor
        };

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingSymbol
            //
            // Ne ListMembers()-t hasznaljunk mert az osok konstruktoraira nincs
            // szukseg (kiveve parameter nelkuli amennyiben az osben nincs felulirva)
            //

            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => Ctors.Contains(m.MethodKind) && m.GetAccessModifiers() != AccessModifiers.Private && !m.IsImplicitlyDeclared)
            .Select(m => (IConstructorInfo) SymbolMethodInfo.CreateFrom(m, Compilation))
            .ToArray();

        public string Name => UnderlyingSymbol.GetFriendlyName();

        public override bool Equals(object obj) => obj is SymbolTypeInfo that && SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, that.UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();

        private sealed class SymbolGenericTypeInfo : SymbolTypeInfo, IGenericTypeInfo
        {
            private new INamedTypeSymbol UnderlyingSymbol => (INamedTypeSymbol) base.UnderlyingSymbol;

            public SymbolGenericTypeInfo(INamedTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            public bool IsGenericDefinition => UnderlyingSymbol.IsUnboundGenericType;

            private IGeneric? FGenericDefinition;
            public IGeneric GenericDefinition => FGenericDefinition ??= (IGeneric) CreateFrom(UnderlyingSymbol.OriginalDefinition, Compilation);

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingSymbol
                .TypeArguments
                .Select(ti => CreateFrom(ti, Compilation))
                .ToArray();

            public IGeneric Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingSymbol.ContainingType is not null) throw new NotSupportedException(); // TODO: implementalni ha hasznalni kell majd

                return (IGeneric) CreateFrom
                (
                    UnderlyingSymbol.Construct
                    (
                        genericArgs.Select(ga => TypeInfoToSymbol(ga, Compilation)).ToArray()
                    ),
                    Compilation
                );
            }
        }

        internal static ITypeSymbol TypeInfoToSymbol(ITypeInfo type, Compilation compilation)
        {
            INamedTypeSymbol symbol;

            if (type.EnclosingTypes.Any())
            {
                int arity = (type as IGenericTypeInfo)?.GenericArguments?.Count ?? 0;

                symbol = TypeInfoToSymbol(type.EnclosingTypes.Last(), compilation)
                    .GetTypeMembers(type.Name, arity)
                    .Single();
            }
            else
            {
                if (type is IArrayTypeInfo ar) 
                    //
                    // Tombot nem lehet lekerdezni nev alapjan
                    //

                    return compilation.CreateArrayTypeSymbol(TypeInfoToSymbol(ar.ElementType!, compilation), ar.Rank);

                if (type.RefType == RefType.Pointer)
                    return compilation.CreatePointerTypeSymbol(TypeInfoToSymbol(type.ElementType!, compilation));

                //
                // A GetTypeByMetadataName() nem mukodik lezart generikusokra, de ez nem is gond
                // mert a FullName a nyilt generikus tipushoz tartozo nevet adja vissza
                //

                symbol = compilation
                    .GetTypeByMetadataName(type.FullName ?? throw new NotSupportedException()) ?? throw new TypeLoadException(string.Format(Resources.Culture, Resources.TYPE_NOT_FOUND, type.FullName));
            }

            if (type is IGenericTypeInfo generic && !generic.IsGenericDefinition)
            {
                ITypeSymbol[] gaSymbols = generic
                    .GenericArguments
                    .Select(ga => TypeInfoToSymbol(ga, compilation))
                    .ToArray();

                return symbol.Construct(gaSymbols);
            }

            return symbol;
        }

        private sealed class SymbolArrayTypeInfo : SymbolTypeInfo, IArrayTypeInfo
        {
            public SymbolArrayTypeInfo(IArrayTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            public int Rank => ((IArrayTypeSymbol) UnderlyingSymbol).Rank;
        }
    }
}
