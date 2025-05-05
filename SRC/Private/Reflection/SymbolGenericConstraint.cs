/********************************************************************************
* SymbolGenericConstraint.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolGenericConstraint(ITypeParameterSymbol genericArgument, Compilation compilation) : IGenericConstraint
    {
        public static IGenericConstraint? CreateFrom(ITypeParameterSymbol genericArgument, Compilation compilation) =>
            genericArgument is { HasConstructorConstraint: true } or { HasReferenceTypeConstraint: true } or { HasValueTypeConstraint: true } or { ConstraintTypes.Length: > 0 }
                ? new SymbolGenericConstraint(genericArgument, compilation)
                : null;

        public bool DefaultConstructor => genericArgument.HasConstructorConstraint;

        public bool Reference => genericArgument.HasReferenceTypeConstraint;

        public bool Struct => genericArgument.HasValueTypeConstraint;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<ITypeInfo>? FConstraintTypes;
        public IReadOnlyList<ITypeInfo> ConstraintTypes => FConstraintTypes ??= genericArgument
            .ConstraintTypes
            .Select(t => SymbolTypeInfo.CreateFrom(t, compilation))
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FTarget;
        public ITypeInfo Target => FTarget ??= SymbolTypeInfo.CreateFrom(genericArgument, compilation);
    }
}
