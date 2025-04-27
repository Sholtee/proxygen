/********************************************************************************
* SymbolMethodInfo.cs                                                           *
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
                { TypeArguments.Length: > 0 } => new SymbolGenericMethodInfo(method, compilation),
                _ => new SymbolMethodInfo(method, compilation)
            };
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IParameterInfo>? FParameters;
        public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderlyingSymbol
            .Parameters
            .Select(p => SymbolParameterInfo.CreateFrom(p, Compilation))
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IParameterInfo? FReturnValue;
        public IParameterInfo ReturnValue => FReturnValue ??= UnderlyingSymbol.MethodKind != MethodKind.Constructor
            ? SymbolReturnParameterInfo.CreateFrom(UnderlyingSymbol, Compilation)
            : null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool? FIsSpecial;
        public bool IsSpecial => FIsSpecial ??= UnderlyingSymbol.IsSpecial();

        public bool IsAbstract => UnderlyingSymbol.IsAbstract;

        public bool IsVirtual => UnderlyingSymbol.IsVirtual();

        public AccessModifiers AccessModifiers => UnderlyingSymbol.GetAccessModifiers();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= SymbolTypeInfo.CreateFrom(UnderlyingSymbol.ContainingType, Compilation);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        private sealed class SymbolGenericMethodInfo(IMethodSymbol method, Compilation compilation) : SymbolMethodInfo(method, compilation), IGenericMethodInfo 
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private bool? FIsGenericDefinition;
            public bool IsGenericDefinition => FIsGenericDefinition ??= !UnderlyingSymbol
                .TypeParameters
                .Any(static tp => !SymbolEqualityComparer.Default.Equals(tp.OriginalDefinition, tp));

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IGenericMethodInfo? FGenericDefinition;
            public IGenericMethodInfo GenericDefinition => FGenericDefinition ??= new SymbolGenericMethodInfo(UnderlyingSymbol.OriginalDefinition, Compilation);

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingSymbol
                .TypeArguments
                .Select(ta => SymbolTypeInfo.CreateFrom(ta, Compilation))
                .ToImmutableList();

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IReadOnlyList<IGenericConstraint>? FGenericConstraints;
            public IReadOnlyList<IGenericConstraint> GenericConstraints => FGenericConstraints ??= UnderlyingSymbol
                .TypeParameters
                .Select(gc => SymbolGenericConstraint.CreateFrom(gc, Compilation)!)
                .Where(static gc => gc is not null)
                .ToImmutableList();

            public IGenericMethodInfo Close(params ITypeInfo[] genericArgs) =>
                //
                // We never specialize open generic methods
                //

                throw new NotImplementedException();
        }
    }
}
