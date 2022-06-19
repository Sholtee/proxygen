/********************************************************************************
* SymbolTypeInfo.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolTypeInfo : ITypeInfo
    {
        protected ITypeSymbol UnderlyingSymbol { get; }

        protected Compilation Compilation { get; }

        private SymbolTypeInfo(ITypeSymbol typeSymbol, Compilation compilation)
        {
            UnderlyingSymbol = typeSymbol;
            Compilation = compilation;
        }
        public static ITypeInfo CreateFrom(ITypeSymbol typeSymbol, Compilation compilation)
        {
            typeSymbol.EnsureNotError();

            return typeSymbol switch
            {
                IArrayTypeSymbol array => new SymbolArrayTypeInfo(array, compilation),
                INamedTypeSymbol named when named.TypeArguments.Some() /*ne IsGenericType legyen*/ => new SymbolGenericTypeInfo(named, compilation),

                //
                // NET6_0 workaround
                //

                _ when typeSymbol.Kind is SymbolKind.FunctionPointerType => CreateFrom
                (
                    compilation.GetTypeByMetadataName(typeof(IntPtr).FullName)!,
                    compilation
                ),
                _ => new SymbolTypeInfo(typeSymbol, compilation)
            };
        }

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
            IArrayTypeSymbol => RefType.Array,
            _ when UnderlyingSymbol.IsRefLikeType => RefType.Ref,
            _ => RefType.None
        };

        public bool IsNested => UnderlyingSymbol.IsNested();

        public bool IsGenericParameter => UnderlyingSymbol.IsGenericParameter();

        public bool IsInterface => UnderlyingSymbol.IsInterface();

        public string? AssemblyQualifiedName => !IsGenericParameter ? UnderlyingSymbol.GetAssemblyQualifiedName() : null;

        public string? QualifiedName => !IsGenericParameter ? UnderlyingSymbol.GetQualifiedMetadataName() : null;

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType
        {
            get
            {
                if (FElementType is null)
                {
                    ITypeSymbol? realType = UnderlyingSymbol.GetElementType();

                    if (realType is not null)
                        FElementType = CreateFrom(realType, Compilation);
                }
                return FElementType;
            }
        }

        private ITypeInfo? FEnclosingType;
        public ITypeInfo? EnclosingType => UnderlyingSymbol.ContainingType is not null
            ? FEnclosingType ??= CreateFrom(UnderlyingSymbol.ContainingType, Compilation)
            : null;

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingSymbol
            .GetAllInterfaces()
            .ConvertAr(ti => CreateFrom(ti, Compilation));

        private ITypeInfo? FBaseType;
        public ITypeInfo? BaseType => UnderlyingSymbol.BaseType is not null
            ? FBaseType ??= CreateFrom(UnderlyingSymbol.BaseType, Compilation)
            : null;

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingSymbol
            .ListProperties(includeStatic: true)
            .ConvertAr(p => SymbolPropertyInfo.CreateFrom(p, Compilation));

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingSymbol
            .ListEvents(includeStatic: true)
            .ConvertAr(evt => SymbolEventInfo.CreateFrom(evt, Compilation));

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingSymbol
            .ListMethods(includeStatic: true)
            .ConvertAr(m => SymbolMethodInfo.CreateFrom(m, Compilation), static m => !m.IsClassMethod());

        private static readonly IReadOnlyList<MethodKind> Ctors = new[] 
        {
            MethodKind.Constructor, MethodKind.StaticConstructor
        };

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingSymbol
            //
            // Ne ListMembers()-t hasznaljunk mert az osok konstruktoraira nincs szukseg (kiveve parameter nelkuli amennyiben az osben nincs felulirva)
            //

            .GetMembers()
            .ConvertAr
            (
                m => (IConstructorInfo) SymbolMethodInfo.CreateFrom((IMethodSymbol) m, Compilation),
                m => m is not IMethodSymbol ctor || 
                    Ctors.IndexOf(ctor.MethodKind) is null ||
                    ctor.GetAccessModifiers() is AccessModifiers.Private || 
                    ctor.IsImplicitlyDeclared
            );

        public string Name => UnderlyingSymbol.GetFriendlyName();

        public bool IsClass => UnderlyingSymbol.IsClass();

        public bool IsFinal => UnderlyingSymbol.IsFinal();

        public bool IsAbstract => UnderlyingSymbol.IsAbstract;

        private IHasName? FContainingMember;
        public IHasName? ContainingMember => FContainingMember ??= UnderlyingSymbol.ContainingSymbol switch 
        {
            IMethodSymbol method => SymbolMethodInfo.CreateFrom(method, Compilation),
            ITypeSymbol type => SymbolTypeInfo.CreateFrom(type, Compilation),
            _ => null
        };

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        public override bool Equals(object obj) => obj is SymbolTypeInfo that && SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, that.UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();

        private sealed class SymbolGenericTypeInfo : SymbolTypeInfo, IGenericTypeInfo
        {
            private new INamedTypeSymbol UnderlyingSymbol => (INamedTypeSymbol) base.UnderlyingSymbol;

            public SymbolGenericTypeInfo(INamedTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            public bool IsGenericDefinition
            {
                get
                {
                    foreach (ITypeSymbol ta in UnderlyingSymbol.TypeArguments)
                    {
                        if (!ta.IsGenericParameter()) // "UnderlyingSymbol.IsUnboundGenericType" baszik mukodni
                            return false;
                    }

                    return true;
                }
            }
            private IGenericTypeInfo? FGenericDefinition;
            public IGenericTypeInfo GenericDefinition => FGenericDefinition ??= (IGenericTypeInfo) CreateFrom(UnderlyingSymbol.OriginalDefinition, Compilation);

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingSymbol
                .TypeArguments
                .ConvertAr(ti => CreateFrom(ti, Compilation));

            public IGenericTypeInfo Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingSymbol.ContainingType is not null) throw new NotSupportedException(); // TODO: implementalni ha hasznalni kell majd

                ITypeSymbol[] gas = new ITypeSymbol[genericArgs.Length];

                for (int i = 0; i < genericArgs.Length; i++)
                {
                    gas[i] = genericArgs[i].ToSymbol(Compilation);
                }

                return (IGenericTypeInfo) CreateFrom
                (
                    UnderlyingSymbol.Construct(gas),
                    Compilation
                );
            }
        }

        private sealed class SymbolArrayTypeInfo : SymbolTypeInfo, IArrayTypeInfo
        {
            public SymbolArrayTypeInfo(IArrayTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            public int Rank => ((IArrayTypeSymbol) UnderlyingSymbol).Rank;
        }
    }
}
