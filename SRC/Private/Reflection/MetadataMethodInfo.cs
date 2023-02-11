﻿/********************************************************************************
* MetadataMethodInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderlyingMethod.GetParameters().ConvertAr(MetadataParameterInfo.CreateFrom);

            public abstract IParameterInfo ReturnValue { get; }

            public string Name => UnderlyingMethod.StrippedName();

            private ITypeInfo? FDeclaringType;
            public ITypeInfo DeclaringType => FDeclaringType ??= MetadataTypeInfo.CreateFrom(UnderlyingMethod.DeclaringType);

            private IReadOnlyList<ITypeInfo>? FDeclaringInterfaces;
            public IReadOnlyList<ITypeInfo> DeclaringInterfaces => FDeclaringInterfaces ??= UnderlyingMethod.GetDeclaringInterfaces().ConvertAr(MetadataTypeInfo.CreateFrom);

            public bool IsStatic => UnderlyingMethod.IsStatic;

            public bool IsSpecial => UnderlyingMethod.IsSpecial();

            public AccessModifiers AccessModifiers => UnderlyingMethod.GetAccessModifiers();

            public bool IsFinal => UnderlyingMethod.IsFinal();

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
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingMethod.GetGenericArguments().ConvertAr(MetadataTypeInfo.CreateFrom);

            private IGenericMethodInfo? FGenericDefinition;
            public IGenericMethodInfo GenericDefinition => FGenericDefinition ??= new MetadataGenericMethodInfo
            (
                UnderlyingMethod.GetGenericMethodDefinition()
            );

            private IReadOnlyDictionary<ITypeInfo, IReadOnlyList<object>>? FGenericConstraints;
            public IReadOnlyDictionary<ITypeInfo, IReadOnlyList<object>> GenericConstraints => FGenericConstraints ??= !UnderlyingMethod.IsGenericMethodDefinition
                ? new Dictionary<ITypeInfo, IReadOnlyList<object>>(0)
                : UnderlyingMethod.GetGenericArguments().ConvertDict
                (
                    static ga => new KeyValuePair<ITypeInfo, IReadOnlyList<object>>
                    (
                        MetadataTypeInfo.CreateFrom(ga),
                        ImmutableList.Create<object>().AddRange
                        (
                            ga.GetGenericConstraints()
                        )
                    ),
                    drop: static ga => !ga.GetGenericConstraints().Some()
                );

            public IGenericMethodInfo Close(params ITypeInfo[] genericArgs) => throw new NotImplementedException(); // Nincs ra szukseg
        }

        private sealed class MetadataConstructorInfo : MetadataMethodBase<ConstructorInfo>, IConstructorInfo 
        {
            public MetadataConstructorInfo(ConstructorInfo method) : base(method) { }

            public override IParameterInfo ReturnValue { get; } = null!;
        }
    }
}
