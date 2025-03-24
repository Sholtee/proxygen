/********************************************************************************
* MetadataMethodInfo.cs                                                         *
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
    internal static class MetadataMethodInfo
    {
        public static IMethodInfo CreateFrom(MethodBase methodBase) => methodBase switch
        {
            ConstructorInfo constructor => new MetadataConstructorInfo(constructor),
            MethodInfo method when method.IsGenericMethod => new MetadataGenericMethodInfo(method), 
            MethodInfo method => new MetadataMethodInfoImpl(method),
            _ => throw new NotSupportedException()
        };

        private abstract class MetadataMethodBase<T>: IMethodInfo where T: MethodBase
        {
            protected T UnderlyingMethod { get; }

            protected MetadataMethodBase(T method) => UnderlyingMethod = method;

            private IReadOnlyList<IParameterInfo>? FParameters;
            public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderlyingMethod
                .GetParameters()
                .Select(MetadataParameterInfo.CreateFrom)
                .ToImmutableList();

            public abstract IParameterInfo ReturnValue { get; }

            public string Name => UnderlyingMethod.StrippedName();

            private ITypeInfo? FDeclaringType;
            public ITypeInfo DeclaringType => FDeclaringType ??= MetadataTypeInfo.CreateFrom(UnderlyingMethod.ReflectedType);

            private IReadOnlyList<ITypeInfo>? FDeclaringInterfaces;
            public IReadOnlyList<ITypeInfo> DeclaringInterfaces => FDeclaringInterfaces ??= UnderlyingMethod
                .GetDeclaringInterfaces()
                .Select(MetadataTypeInfo.CreateFrom)
                .ToImmutableList();

            public bool IsStatic => UnderlyingMethod.IsStatic;

            public bool IsSpecial => UnderlyingMethod.IsSpecialName;

            public bool IsAbstract => UnderlyingMethod.IsAbstract;

            public bool IsVirtual => UnderlyingMethod.IsVirtual();

            public AccessModifiers AccessModifiers => UnderlyingMethod.GetAccessModifiers();

            public override bool Equals(object obj) => obj is MetadataMethodBase<T> that && UnderlyingMethod.Equals(that.UnderlyingMethod);

            public override int GetHashCode() => UnderlyingMethod.GetHashCode();

            public override string ToString() => UnderlyingMethod.ToString();
        }

        private class MetadataMethodInfoImpl : MetadataMethodBase<MethodInfo>
        {
            public MetadataMethodInfoImpl(MethodInfo method) : base(method) { }

            private IParameterInfo? FReturnValue;
            public override IParameterInfo ReturnValue => FReturnValue ??= MetadataParameterInfo.CreateFrom(UnderlyingMethod.ReturnParameter);
        }

        private sealed class MetadataGenericMethodInfo : MetadataMethodInfoImpl, IGenericMethodInfo
        {
            public MetadataGenericMethodInfo(MethodInfo method) : base(method) { }

            public bool IsGenericDefinition => UnderlyingMethod.IsGenericMethodDefinition;

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingMethod
                .GetGenericArguments()
                .Select(MetadataTypeInfo.CreateFrom)
                .ToImmutableList();

            private IGenericMethodInfo? FGenericDefinition;
            public IGenericMethodInfo GenericDefinition => FGenericDefinition ??= new MetadataGenericMethodInfo
            (
                UnderlyingMethod.GetGenericMethodDefinition()
            );

            private IEnumerable<IGenericConstraint> GetConstraints()
            {
                foreach (Type ga in UnderlyingMethod.GetGenericArguments())
                {
                    IGenericConstraint? constraint = MetadataGenericConstraint.CreateFrom(ga);
                    if (constraint is not null)
                        yield return constraint;
                }
            }

            private IReadOnlyList<IGenericConstraint>? FGenericConstraints;
            public IReadOnlyList<IGenericConstraint> GenericConstraints => FGenericConstraints ??= !IsGenericDefinition
                ? ImmutableList.Create<IGenericConstraint>()
                : GetConstraints().ToImmutableList();

            public IGenericMethodInfo Close(params ITypeInfo[] genericArgs) => throw new NotImplementedException(); // out of use
        }

        private sealed class MetadataConstructorInfo : MetadataMethodBase<ConstructorInfo>, IConstructorInfo 
        {
            public MetadataConstructorInfo(ConstructorInfo method) : base(method) { }

            public override IParameterInfo ReturnValue { get; } = null!;
        }
    }
}
