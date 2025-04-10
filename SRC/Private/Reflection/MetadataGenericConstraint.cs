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
    internal sealed class MetadataGenericConstraint: IGenericConstraint
    {
        private Type UnderlyingType { get; }

        private MemberInfo DeclaringMember { get; }

        private MetadataGenericConstraint(Type genericArgument, MemberInfo declaringMember)
        {
            UnderlyingType = genericArgument;
            DeclaringMember = declaringMember;
        }

        public static IGenericConstraint? CreateFrom(Type genericArgument, MemberInfo declaringMember) =>
            genericArgument.GenericParameterAttributes is > GenericParameterAttributes.VarianceMask and < (GenericParameterAttributes) 32 /*AllowByRefLike*/ || genericArgument.GetGenericConstraints(declaringMember).Any()
                ? new MetadataGenericConstraint(genericArgument, declaringMember)
                : null;

        public bool DefaultConstructor => !Struct && UnderlyingType
            .GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);

        public bool Reference => UnderlyingType
            .GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);

        public bool Struct => UnderlyingType
            .GenericParameterAttributes
            .HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<ITypeInfo>? FConstraintTypes;
        public IReadOnlyList<ITypeInfo> ConstraintTypes => FConstraintTypes ??= UnderlyingType
            .GetGenericConstraints(DeclaringMember)
            .Select(MetadataTypeInfo.CreateFrom)
            .ToImmutableList();

        private ITypeInfo? FTarget;
        public ITypeInfo Target => FTarget ??= MetadataTypeInfo.CreateFrom(UnderlyingType);
    }
}
