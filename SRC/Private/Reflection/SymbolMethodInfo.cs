/********************************************************************************
* SymbolMethodInfo.cs                                                           *
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
    internal class SymbolMethodInfo : IMethodInfo, IConstructorInfo
    {
        private IMethodSymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        protected SymbolMethodInfo(IMethodSymbol method, Compilation compilation) 
        {
            UnderlyingSymbol = method;
            Compilation = compilation;
        }

        public static IMethodInfo CreateFrom(IMethodSymbol method, Compilation compilation)
        {
            method.EnsureNotError();

            return method switch
            {
                _ when method.TypeArguments.Length > 0 => new SymbolGenericMethodInfo(method, compilation),
                _ => new SymbolMethodInfo(method, compilation)
            };
        }

        private IReadOnlyList<IParameterInfo>? FParameters;
        public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderlyingSymbol
            .Parameters
            .Select(p => SymbolParameterInfo.CreateFrom(p, Compilation))
            .ToImmutableList();

        private IParameterInfo? FReturnValue;
        public IParameterInfo ReturnValue => FReturnValue ??= UnderlyingSymbol.MethodKind != MethodKind.Constructor
            ? SymbolReturnParameterInfo.CreateFrom(UnderlyingSymbol, Compilation)
            : null!;

        private bool? FIsSpecial;
        public bool IsSpecial => FIsSpecial ??= UnderlyingSymbol.IsSpecial();

        public bool IsAbstract => UnderlyingSymbol.IsAbstract;

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.ContainingType, Compilation);

        private IReadOnlyList<ITypeInfo>? FDeclaringInterfaces;
        public IReadOnlyList<ITypeInfo> DeclaringInterfaces => FDeclaringInterfaces ??= UnderlyingSymbol
            .GetDeclaringInterfaces()
            .Select(di => SymbolTypeInfo.CreateFrom(di, Compilation))
            .ToImmutableList();

        public bool IsStatic => UnderlyingSymbol.IsStatic;

        public string Name => UnderlyingSymbol.StrippedName();

        public override bool Equals(object obj) => obj is SymbolMethodInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();

        private sealed class SymbolGenericMethodInfo : SymbolMethodInfo, IGenericMethodInfo 
        {
            public SymbolGenericMethodInfo(IMethodSymbol method, Compilation compilation) : base(method, compilation) { }

            private bool? FIsGenericDefinition;
            public bool IsGenericDefinition => FIsGenericDefinition ??= !UnderlyingSymbol
                .TypeParameters
                .Any(static tp => !SymbolEqualityComparer.Default.Equals(tp.OriginalDefinition, tp));

            private IGenericMethodInfo? FGenericDefinition;
            public IGenericMethodInfo GenericDefinition => FGenericDefinition ??= new SymbolGenericMethodInfo(UnderlyingSymbol.OriginalDefinition, Compilation);

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingSymbol
                .TypeArguments
                .Select(ta => SymbolTypeInfo.CreateFrom(ta, Compilation))
                .ToImmutableList();

            private IEnumerable<IGenericConstraint> GetConstraints()
            {
                foreach (ITypeParameterSymbol ga in UnderlyingSymbol.TypeParameters)
                {
                    IGenericConstraint? constraint = SymbolGenericConstraint.CreateFrom(ga, Compilation);
                    if (constraint is not null)
                        yield return constraint;
                }
            }

            private IReadOnlyList<IGenericConstraint>? FGenericConstraints;
            public IReadOnlyList<IGenericConstraint> GenericConstraints => FGenericConstraints ??= !IsGenericDefinition
                ? ImmutableList.Create<IGenericConstraint>()
                : GetConstraints().ToImmutableList();

            public IGenericMethodInfo Close(params ITypeInfo[] genericArgs) => throw new NotImplementedException(); // Nincs ra szukseg
        }
    }
}
