/********************************************************************************
* SymbolTypeInfo.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolTypeInfo(ITypeSymbol underlyingSymbol, Compilation compilation) : ITypeInfo
    {
        protected ITypeSymbol UnderlyingSymbol { get; } = underlyingSymbol;

        protected Compilation Compilation { get; } = compilation;

        public static ITypeInfo CreateFrom(ITypeSymbol typeSymbol, Compilation compilation)
        {
            typeSymbol.EnsureNotError();

            return typeSymbol switch
            {
                IArrayTypeSymbol array => new SymbolArrayTypeInfo(array, compilation),
                INamedTypeSymbol { TypeArguments.Length: > 0 } named => new SymbolGenericTypeInfo(named, compilation),

                //
                // NET6_0 workaround
                //

                { Kind: SymbolKind.FunctionPointerType } => CreateFrom
                (
                    compilation.GetTypeByMetadataName(typeof(IntPtr).FullName)!,
                    compilation
                ),
                _ => new SymbolTypeInfo(typeSymbol, compilation)
            };
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<IAssemblyInfo?> FDeclaringAssembly = new(() =>
        {
            ITypeSymbol? elementType = underlyingSymbol.GetElementType(recurse: true);

            IAssemblySymbol? asm = elementType?.ContainingAssembly ?? underlyingSymbol.ContainingAssembly;

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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<TypeInfoFlags> FFlags = new(() =>
        {
            TypeInfoFlags flags = TypeInfoFlags.None;

            if (underlyingSymbol.SpecialType == SpecialType.System_Void)
                flags |= TypeInfoFlags.IsVoid;

            if (underlyingSymbol.IsDelegate())
                flags |= TypeInfoFlags.IsDelegate;

            if (underlyingSymbol.IsGenericParameter())
                flags |= TypeInfoFlags.IsGenericParameter;

            if (underlyingSymbol.IsNested())
                flags |= TypeInfoFlags.IsNested;

            if (underlyingSymbol.IsInterface())
                flags |= TypeInfoFlags.IsInterface;

            if (underlyingSymbol.IsClass())
                flags |= TypeInfoFlags.IsClass;

            if (underlyingSymbol.IsFinal())
                flags |= TypeInfoFlags.IsFinal;

            if (underlyingSymbol.IsAbstract)
                flags |= TypeInfoFlags.IsAbstract;

            return flags;
        });
        public TypeInfoFlags Flags => FFlags.Value;

        public RefType RefType => UnderlyingSymbol switch
        {
            IPointerTypeSymbol => RefType.Pointer,
            IArrayTypeSymbol => RefType.Array,
            { IsRefLikeType: true } => RefType.Ref,
            _ => RefType.None
        };

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<string?> FAssemblyQualifiedName = new
        (
            () => !underlyingSymbol.IsGenericParameter()
                ? underlyingSymbol.GetAssemblyQualifiedName()
                : null
        );
        public string? AssemblyQualifiedName => FAssemblyQualifiedName.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<string?> FQualifiedName = new
        (
            () => !underlyingSymbol.IsGenericParameter()
                ? underlyingSymbol.GetQualifiedMetadataName()
                : null
        );
        public string? QualifiedName => FQualifiedName.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<ITypeInfo?> FElementType = new(() =>
        {
            ITypeSymbol? realType = underlyingSymbol.GetElementType();

            return realType is not null
                ? CreateFrom(realType, compilation)
                : null;
        });
        public ITypeInfo? ElementType => FElementType.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FEnclosingType;
        public ITypeInfo? EnclosingType => UnderlyingSymbol.GetEnclosingType() is not null
            ? FEnclosingType ??= CreateFrom(UnderlyingSymbol.GetEnclosingType()!, Compilation)
            : null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingSymbol
            .GetAllInterfaces()
            .Select(ti => CreateFrom(ti, Compilation))
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FBaseType;
        public ITypeInfo? BaseType => UnderlyingSymbol.BaseType is not null
            ? FBaseType ??= CreateFrom(UnderlyingSymbol.BaseType, Compilation)
            : null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingSymbol
            .ListProperties(includeStatic: true)
            .Select(p => SymbolPropertyInfo.CreateFrom(p, Compilation))
            .Sort()
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingSymbol
            .ListEvents(includeStatic: true)
            .Select(evt => SymbolEventInfo.CreateFrom(evt, Compilation))
            .Sort()
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingSymbol
            .ListMethods(includeStatic: true)
            .Where(IMethodSymbolExtensions.IsClassMethod)
            .Select(m => SymbolMethodInfo.CreateFrom(m, Compilation))
            .Sort()
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingSymbol
            .GetConstructors()
            .Select(m => (IConstructorInfo) SymbolMethodInfo.CreateFrom(m, Compilation))
            .Sort()
            .ToImmutableList();

        public string Name => UnderlyingSymbol.GetFriendlyName();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<IHasName?> FContainingMember = new(() =>
        {
            ITypeSymbol concreteType = underlyingSymbol.GetElementType(recurse: true) ?? underlyingSymbol;

            return concreteType.ContainingSymbol switch
            {
                IMethodSymbol method => SymbolMethodInfo.CreateFrom
                (
                    //
                    // Mimic the way how reflection works...
                    //

                    underlyingSymbol.IsGenericParameter() && method.IsGenericMethod
                        ? method.OriginalDefinition
                        : method,
                    compilation
                ),
                _ when underlyingSymbol.GetEnclosingType() is not null => CreateFrom(underlyingSymbol.GetEnclosingType()!, compilation),
                _ => null
            };
        });
        public IHasName? ContainingMember => FContainingMember.Value;

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        public override bool Equals(object obj) => obj is SymbolTypeInfo that && SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, that.UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();

        private sealed class SymbolGenericTypeInfo(INamedTypeSymbol underlyingSymbol, Compilation compilation) : SymbolTypeInfo(underlyingSymbol, compilation), IGenericTypeInfo
        {
            private new INamedTypeSymbol UnderlyingSymbol => (INamedTypeSymbol) base.UnderlyingSymbol;

            //
            // "UnderlyingSymbol.IsUnboundGenericType" doesn't work
            //

            public bool IsGenericDefinition => UnderlyingSymbol
                .TypeArguments
                .Any(static ta => ta.IsGenericParameter());

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IGenericTypeInfo? FGenericDefinition;
            public IGenericTypeInfo GenericDefinition => FGenericDefinition ??= (IGenericTypeInfo) CreateFrom(UnderlyingSymbol.OriginalDefinition, Compilation);

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        private sealed class SymbolArrayTypeInfo(IArrayTypeSymbol underlyingSymbol, Compilation compilation) : SymbolTypeInfo(underlyingSymbol, compilation), IArrayTypeInfo
        {
            public int Rank => ((IArrayTypeSymbol) UnderlyingSymbol).Rank;
        }
    }
}
