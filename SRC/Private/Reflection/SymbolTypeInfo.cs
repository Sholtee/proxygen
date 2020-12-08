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
    internal class SymbolTypeInfo : ITypeInfo
    {
        protected ITypeSymbol UnderlyingTypeSymbol { get; }

        protected Compilation Compilation { get; }

        private SymbolTypeInfo(ITypeSymbol typeSymbol, Compilation compilation)
        {
            UnderlyingTypeSymbol = typeSymbol;
            Compilation = compilation;
        }
        public static ITypeInfo CreateFrom(ITypeSymbol typeSymbol, Compilation compilation) => typeSymbol switch 
        {
            IArrayTypeSymbol array => new SymbolArrayTypeInfo(array, compilation),
            INamedTypeSymbol named when named.TypeArguments.Any() /*ne IsGenericType legyen*/ => new SymbolGenericTypeInfo(named, compilation),
            _ => new SymbolTypeInfo(typeSymbol, compilation)
        };

        private IAssemblyInfo? FAssemblyInfo;
        public IAssemblyInfo DeclaringAssembly => FAssemblyInfo ??= SymbolAssemblyInfo.CreateFrom(UnderlyingTypeSymbol.ContainingAssembly, Compilation);

        public bool IsVoid => UnderlyingTypeSymbol.SpecialType == SpecialType.System_Void;

        public bool IsByRef => false; // forras szinten nem jelenik meg

        public bool IsNested => UnderlyingTypeSymbol.IsNested();

        public bool IsGenericParameter => UnderlyingTypeSymbol.IsGenericParameter();

        public bool IsInterface => UnderlyingTypeSymbol.IsInterface();

        public string? AssemblyQualifiedName => UnderlyingTypeSymbol is INamedTypeSymbol named ? named.GetAssemblyQualifiedName() : null;

        public string? FullName => UnderlyingTypeSymbol is INamedTypeSymbol named ? named.GetQualifiedMetadataName() : null;

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType
        {
            get
            {
                if (FElementType == null)
                {
                    ITypeSymbol? realType = UnderlyingTypeSymbol.GetElementType();

                    if (realType != null)
                        FElementType = CreateFrom(realType, Compilation);
                }
                return FElementType;
            }
        }

        private IReadOnlyList<ITypeInfo>? FEnclosingTypes;
        public IReadOnlyList<ITypeInfo> EnclosingTypes => FEnclosingTypes ??= UnderlyingTypeSymbol
            .GetEnclosingTypes()
            .Select(ti => CreateFrom(ti, Compilation))
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingTypeSymbol
            .AllInterfaces
            .Select(ti => CreateFrom(ti, Compilation))
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FBases;
        public IReadOnlyList<ITypeInfo> Bases => FBases ??= UnderlyingTypeSymbol
            .GetBaseTypes()
            .Select(ti => CreateFrom(ti, Compilation))
            .ToArray();

        public IReadOnlyList<IPropertyInfo> Properties => throw new System.NotImplementedException();

        public IReadOnlyList<IEventInfo> Events => throw new System.NotImplementedException();

        public IReadOnlyList<IMethodInfo> Methods => throw new System.NotImplementedException();

        public IReadOnlyList<IConstructorInfo> Constructors => throw new System.NotImplementedException();

        public string Name => UnderlyingTypeSymbol.GetFriendlyName();

        public override bool Equals(object obj) => obj is SymbolTypeInfo that && SymbolEqualityComparer.Default.Equals(UnderlyingTypeSymbol, that.UnderlyingTypeSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingTypeSymbol);

        private sealed class SymbolGenericTypeInfo : SymbolTypeInfo, IGenericTypeInfo
        {
            private new INamedTypeSymbol UnderlyingTypeSymbol => (INamedTypeSymbol) base.UnderlyingTypeSymbol;

            public SymbolGenericTypeInfo(INamedTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            public bool IsGenericDefinition => UnderlyingTypeSymbol.IsUnboundGenericType;

            private IGeneric? FGenericDefinition;
            public IGeneric GenericDefinition => FGenericDefinition ??= (IGeneric) CreateFrom(UnderlyingTypeSymbol.OriginalDefinition, Compilation);

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingTypeSymbol
                .TypeArguments
                .Select(ti => CreateFrom(ti, Compilation))
                .ToArray();

            public IGeneric Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingTypeSymbol.ContainingType is not null) throw new NotSupportedException(); // TODO: implementalni ha hasznalni kell majd

                return (IGeneric) CreateFrom
                (
                    UnderlyingTypeSymbol.Construct
                    (
                        genericArgs.Select(TypeInfoToSymbol).ToArray()
                    ),
                    Compilation
                );
            }
        }

        internal INamedTypeSymbol TypeInfoToSymbol(ITypeInfo type)
        {
            INamedTypeSymbol symbol;

            if (type.EnclosingTypes.Any())
            {
                int arity = (type as IGenericTypeInfo)?.GenericArguments?.Count ?? 0;

                symbol = TypeInfoToSymbol(type.EnclosingTypes.Last())
                    .GetTypeMembers(type.Name, arity)
                    .Single();
            }
            else
                //
                // A GetTypeByMetadataName() nem mukodik lezart generikusokra, de ez nem is gond
                // mert a FullName a nyilt generikus tipushoz tartozo nevet adja vissza
                //

                symbol = Compilation
                    .GetTypeByMetadataName(type.FullName ?? throw new NotSupportedException()) ?? throw new NotSupportedException();

            if (type is IGenericTypeInfo generic && !generic.IsGenericDefinition)
            {
                INamedTypeSymbol[] gaSymbols = generic
                    .GenericArguments
                    .Select(TypeInfoToSymbol)
                    .ToArray();

                return symbol.Construct(gaSymbols);
            }

            return symbol;
        }

        private sealed class SymbolArrayTypeInfo : SymbolTypeInfo, IArrayTypeInfo
        {
            public SymbolArrayTypeInfo(IArrayTypeSymbol underlyingSymbol, Compilation compilation) : base(underlyingSymbol, compilation) { }

            public int Rank => ((IArrayTypeSymbol) UnderlyingTypeSymbol).Rank;
        }
    }
}
