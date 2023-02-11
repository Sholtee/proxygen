/********************************************************************************
* MetadataGenericConstraint.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataGenericConstraint : IGenericConstraint
    {
        private readonly GenericParameterAttributes FAttributes;

        private MetadataGenericConstraint(GenericParameterAttributes attrs) => FAttributes = attrs;

        public static IGenericConstraint? CreateFrom(Type genericArgument) => genericArgument.GenericParameterAttributes > GenericParameterAttributes.None
            ? new MetadataGenericConstraint(genericArgument.GenericParameterAttributes)
            : null;

        public bool DefaultConstructor => !Struct && FAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);

        public bool Reference => FAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);

        public bool Struct => FAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);
    }
}
