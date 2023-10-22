/********************************************************************************
* MetadataGenericConstraint.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataGenericConstraint : IGenericConstraint
    {
        private MetadataGenericConstraint(Type genericArgument)
        {
            Struct = genericArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);
            DefaultConstructor = !Struct && genericArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);
            Reference = genericArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);
            ConstraintTypes = genericArgument
                .GetGenericParameterConstraints()

                //
                // We don't want a
                //     "where TT : struct, global::System.ValueType"
                //

                .Where(t => t != typeof(ValueType) || !Struct)
                .Select(MetadataTypeInfo.CreateFrom)
                .ToImmutableList();
            Target = MetadataTypeInfo.CreateFrom(genericArgument);
        }

        public static IGenericConstraint? CreateFrom(Type genericArgument)
        {
            MetadataGenericConstraint result = new(genericArgument);
            return !result.DefaultConstructor && !result.Reference && !result.Struct && !result.ConstraintTypes.Any()
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
