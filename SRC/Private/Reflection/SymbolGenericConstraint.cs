/********************************************************************************
* SymbolGenericConstraint.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolGenericConstraint(ITypeParameterSymbol genericArgument, Compilation compilation) : IGenericConstraint
    {
        public static IGenericConstraint? CreateFrom(ITypeParameterSymbol genericArgument, Compilation compilation) =>
            !genericArgument.HasConstructorConstraint && !genericArgument.HasReferenceTypeConstraint && !genericArgument.HasValueTypeConstraint && !genericArgument.ConstraintTypes.Any()
                ? null
                : new SymbolGenericConstraint(genericArgument, compilation);

        public bool DefaultConstructor => genericArgument.HasConstructorConstraint;

        public bool Reference => genericArgument.HasReferenceTypeConstraint;

        public bool Struct => genericArgument.HasValueTypeConstraint;

        private IReadOnlyList<ITypeInfo>? FConstraintTypes;
        public IReadOnlyList<ITypeInfo> ConstraintTypes => FConstraintTypes ??= genericArgument
            .ConstraintTypes
            .Select(t => SymbolTypeInfo.CreateFrom(t, compilation))
            .ToImmutableList();

        private ITypeInfo? FTarget;
        public ITypeInfo Target => FTarget ??= SymbolTypeInfo.CreateFrom(genericArgument, compilation);
    }
}
