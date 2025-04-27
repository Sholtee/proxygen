/********************************************************************************
* MetadataMethodInfo.cs                                                         *
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
    internal static class MetadataMethodInfo
    {
        public static IMethodInfo CreateFrom(MethodBase methodBase) => methodBase switch
        {
            ConstructorInfo constructor => new MetadataConstructorInfo(constructor),
            MethodInfo { IsGenericMethod: true } method => new MetadataGenericMethodInfo(method), 
            MethodInfo method => new MetadataMethodInfoImpl(method),
            _ => throw new NotSupportedException()
        };

        private abstract class MetadataMethodBase<T>(T method) : IMethodInfo where T: MethodBase
        {
            protected T UnderlyingMethod { get; } = method;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IReadOnlyList<IParameterInfo>? FParameters;
            public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderlyingMethod
                .GetParameters()
                .Select(MetadataParameterInfo.CreateFrom)
                .ToImmutableList();

            public abstract IParameterInfo ReturnValue { get; }

            public string Name => UnderlyingMethod.StrippedName();

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private ITypeInfo? FDeclaringType;
            public ITypeInfo DeclaringType => FDeclaringType ??= MetadataTypeInfo.CreateFrom(UnderlyingMethod.DeclaringType);

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        private class MetadataMethodInfoImpl(MethodInfo method) : MetadataMethodBase<MethodInfo>(method)
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IParameterInfo? FReturnValue;
            public override IParameterInfo ReturnValue => FReturnValue ??= MetadataParameterInfo.CreateFrom(UnderlyingMethod.ReturnParameter);
        }

        private sealed class MetadataGenericMethodInfo(MethodInfo method) : MetadataMethodInfoImpl(method), IGenericMethodInfo
        {
            public bool IsGenericDefinition => UnderlyingMethod.IsGenericMethodDefinition;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingMethod
                .GetGenericArguments()
                .Select(MetadataTypeInfo.CreateFrom)
                .ToImmutableList();

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IGenericMethodInfo? FGenericDefinition;
            public IGenericMethodInfo GenericDefinition => FGenericDefinition ??= new MetadataGenericMethodInfo
            (
                UnderlyingMethod.GetGenericMethodDefinition()
            );

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IReadOnlyList<IGenericConstraint>? FGenericConstraints;
            public IReadOnlyList<IGenericConstraint> GenericConstraints => FGenericConstraints ??= !IsGenericDefinition
                ? ImmutableList.Create<IGenericConstraint>()
                : UnderlyingMethod
                    .GetGenericArguments()
                    .Select(ga => MetadataGenericConstraint.CreateFrom(ga, UnderlyingMethod)!)
                    .Where(static c => c is not null)
                    .ToImmutableList();

            public IGenericMethodInfo Close(params ITypeInfo[] genericArgs) =>
                //
                // We never specialize open generic methods
                //

                throw new NotImplementedException();
        }

        private sealed class MetadataConstructorInfo(ConstructorInfo method) : MetadataMethodBase<ConstructorInfo>(method), IConstructorInfo 
        {
            public override IParameterInfo ReturnValue { get; } = null!;
        }
    }
}
