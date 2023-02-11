/********************************************************************************
* MetadataGenericConstraint.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
            ConstraintTypes = genericArgument.GetGenericParameterConstraints().ConvertAr
            (
                MetadataTypeInfo.CreateFrom,
                
                //
                // We don't want a
                //     "where TT : struct, global::System.ValueType"
                //

                drop: t => t == typeof(ValueType) && Struct
            );
            Target = MetadataTypeInfo.CreateFrom(genericArgument);
        }

        public static IGenericConstraint? CreateFrom(Type genericArgument)
        {
            MetadataGenericConstraint result = new(genericArgument);
            return !result.DefaultConstructor && result.Reference && !result.Struct && !result.ConstraintTypes.Some()
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
