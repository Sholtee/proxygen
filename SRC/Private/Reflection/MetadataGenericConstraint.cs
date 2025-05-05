/********************************************************************************
* MetadataGenericConstraint.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataGenericConstraint(Type genericArgument, MemberInfo declaringMember) : IGenericConstraint
    {
        public static IGenericConstraint? CreateFrom(Type genericArgument, MemberInfo declaringMember) =>
            genericArgument.GenericParameterAttributes is > GenericParameterAttributes.VarianceMask and < (GenericParameterAttributes) 32 /*AllowByRefLike*/ || genericArgument.GetGenericConstraints(declaringMember).Any()
                ? new MetadataGenericConstraint(genericArgument, declaringMember)
                : null;

        public bool DefaultConstructor => !Struct && genericArgument
            .GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);

        public bool Reference => genericArgument
            .GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);

        public bool Struct => genericArgument
            .GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<ITypeInfo>? FConstraintTypes;
        public IReadOnlyList<ITypeInfo> ConstraintTypes => FConstraintTypes ??= genericArgument
            .GetGenericConstraints(declaringMember)
            .Select(MetadataTypeInfo.CreateFrom)
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FTarget;
        public ITypeInfo Target => FTarget ??= MetadataTypeInfo.CreateFrom(genericArgument);
    }
}
