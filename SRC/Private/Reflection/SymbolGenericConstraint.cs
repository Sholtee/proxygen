/********************************************************************************
* SymbolGenericConstraint.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SymbolGenericConstraint : IGenericConstraint
    {
        private SymbolGenericConstraint(ITypeParameterSymbol genericArgument, Compilation compilation)
        {
            Target = SymbolTypeInfo.CreateFrom(genericArgument, compilation);
            ConstraintTypes = genericArgument.ConstraintTypes.ConvertAr(t => SymbolTypeInfo.CreateFrom(t, compilation));
            DefaultConstructor = genericArgument.HasConstructorConstraint;
            Reference = genericArgument.HasReferenceTypeConstraint;
            Struct = genericArgument.HasValueTypeConstraint;
        }

        public static IGenericConstraint? CreateFrom(ITypeParameterSymbol genericArgument, Compilation compilation)
        {
            SymbolGenericConstraint result = new(genericArgument, compilation);
            return !result.DefaultConstructor && !result.Reference && !result.Struct && !result.ConstraintTypes.Some()
                ? null
                : result;
        }

        public bool DefaultConstructor { get; }

        public bool Reference { get; }

        public bool Struct { get; }

        public IReadOnlyList<ITypeInfo> ConstraintTypes { get; }

        public ITypeInfo Target { get; }
    }
}
