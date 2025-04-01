/********************************************************************************
* SymbolTypeInfo.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolTypeInfo(ITypeSymbol typeSymbol, Compilation compilation) : ITypeInfo
    {
        protected ITypeSymbol UnderlyingSymbol { get; } = typeSymbol;

        protected Compilation Compilation { get; } = compilation;


        public static ITypeInfo CreateFrom(ITypeSymbol typeSymbol, Compilation compilation)
        {
            typeSymbol.EnsureNotError();

            return typeSymbol switch
            {
                IArrayTypeSymbol array => new SymbolArrayTypeInfo(array, compilation),
                INamedTypeSymbol named when named.TypeArguments.Any() => new SymbolGenericTypeInfo(named, compilation),

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

        private readonly Lazy<IAssemblyInfo?> FDeclaringAssembly = new(() =>
        {
            ITypeSymbol? elementType = typeSymbol.GetElementType(recurse: true);

            IAssemblySymbol? asm = elementType?.ContainingAssembly ?? typeSymbol.ContainingAssembly;

            if (asm is not null)
                return SymbolAssemblyInfo.CreateFrom(asm, compilation);

            if (asm is null && elementType is IFunctionPointerTypeSymbol)
                return CreateFrom
                (
                    compilation.GetTypeByMetadataName(typeof(IntPtr).FullName)!,
                    compilation
                ).DeclaringAssembly;
            return null;
        });
        public IAssemblyInfo? DeclaringAssembly => FDeclaringAssembly.Value;

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

        private readonly Lazy<ITypeInfo?> FElementType = new(() =>
        {
            ITypeSymbol? realType = typeSymbol.GetElementType();

            return realType is not null
                ? CreateFrom(realType, compilation)
                : null;
        });
        public ITypeInfo? ElementType => FElementType.Value;

        private ITypeInfo? FEnclosingType;
        public ITypeInfo? EnclosingType => UnderlyingSymbol.GetEnclosingType() is not null
            ? FEnclosingType ??= CreateFrom(UnderlyingSymbol.GetEnclosingType()!, Compilation)
            : null;

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingSymbol
            .GetAllInterfaces()
            .Select(ti => CreateFrom(ti, Compilation))
            .ToImmutableList();

        private ITypeInfo? FBaseType;
        public ITypeInfo? BaseType => UnderlyingSymbol.BaseType is not null
            ? FBaseType ??= CreateFrom(UnderlyingSymbol.BaseType, Compilation)
            : null;

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingSymbol
            .ListProperties(includeStatic: true)
            .Select(p => SymbolPropertyInfo.CreateFrom(p, Compilation))
            .ToImmutableList();

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingSymbol
            .ListEvents(includeStatic: true)
            .Select(evt => SymbolEventInfo.CreateFrom(evt, Compilation))
            .ToImmutableList();

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingSymbol
            .ListMethods(includeStatic: true)
            .Where(IMethodSymbolExtensions.IsClassMethod)
            .Select(m => SymbolMethodInfo.CreateFrom(m, Compilation))
            .ToImmutableList();

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingSymbol
            .GetConstructors()
            .Select(m => (IConstructorInfo) SymbolMethodInfo.CreateFrom(m, Compilation))
            .ToImmutableList();

        public string Name => UnderlyingSymbol.GetFriendlyName();

        public bool IsClass => UnderlyingSymbol.IsClass();

        public bool IsFinal => UnderlyingSymbol.IsFinal();

        public bool IsAbstract => UnderlyingSymbol.IsAbstract;

        private readonly Lazy<IHasName?> FContainingMember = new(() =>
        {
            ITypeSymbol concreteType = typeSymbol.GetElementType(recurse: true) ?? typeSymbol;

            return concreteType.ContainingSymbol switch
            {
                IMethodSymbol method => SymbolMethodInfo.CreateFrom
                (
                    //
                    // Mimic the way how reflection works...
                    //

                    typeSymbol.IsGenericParameter() && method.IsGenericMethod
                        ? method.OriginalDefinition
                        : method,
                    compilation
                ),
                _ when typeSymbol.GetEnclosingType() is not null => CreateFrom(typeSymbol.GetEnclosingType()!, compilation),
                _ => null
            };
        });
        public IHasName? ContainingMember => FContainingMember.Value;

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        public override bool Equals(object obj) => obj is SymbolTypeInfo that && SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, that.UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();

        private sealed class SymbolGenericTypeInfo : SymbolTypeInfo, IGenericTypeInfo
        {
            private new INamedTypeSymbol UnderlyingSymbol => (INamedTypeSymbol) base.UnderlyingSymbol;

            public SymbolGenericTypeInfo(INamedTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            //
            // "UnderlyingSymbol.IsUnboundGenericType" doesn't work
            //

            public bool IsGenericDefinition => UnderlyingSymbol
                .TypeArguments
                .Any(static ta => ta.IsGenericParameter());

            private IGenericTypeInfo? FGenericDefinition;
            public IGenericTypeInfo GenericDefinition => FGenericDefinition ??= (IGenericTypeInfo) CreateFrom(UnderlyingSymbol.OriginalDefinition, Compilation);

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingSymbol
                .TypeArguments
                .Select(ti => CreateFrom(ti, Compilation))
                .ToImmutableList();

            public IReadOnlyList<IGenericConstraint> GenericConstraints =>
                //
                // We never generate open generic proxies so implementing this property not required
                //

                throw new NotImplementedException();

            public IGenericTypeInfo Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingSymbol.ContainingType is not null)
                    throw new NotImplementedException(); // TODO

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
