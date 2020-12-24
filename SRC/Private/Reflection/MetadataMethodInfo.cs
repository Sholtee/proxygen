/********************************************************************************
* MetadataMethodInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
                .ToArray();

            public abstract IParameterInfo ReturnValue { get; }

            public string Name => UnderlyingMethod.StrippedName();

            private ITypeInfo? FDeclaringType;
            public ITypeInfo DeclaringType => FDeclaringType ??= MetadataTypeInfo.CreateFrom(UnderlyingMethod.DeclaringType);

            private IReadOnlyList<ITypeInfo>? FDeclaringInterfaces;
            public IReadOnlyList<ITypeInfo> DeclaringInterfaces => FDeclaringInterfaces ??= UnderlyingMethod
                .GetDeclaringInterfaces()
                .Select(MetadataTypeInfo.CreateFrom)
                .ToArray();

            public bool IsStatic => UnderlyingMethod.IsStatic;

            public bool IsSpecial => UnderlyingMethod.IsSpecialName;

            public AccessModifiers AccessModifiers => UnderlyingMethod.GetAccessModifiers();

            public bool IsFinal => UnderlyingMethod.IsFinal;

            public override bool Equals(object obj) => obj is MetadataMethodBase<T> that && UnderlyingMethod.Equals(that.UnderlyingMethod);

            public override int GetHashCode() => UnderlyingMethod.GetHashCode();

            public override string ToString() => UnderlyingMethod.ToString();

            public abstract bool SignatureEquals(IMethodInfo that, bool ignoreVisibility);
        }

        private class MetadataMethodInfoImpl : MetadataMethodBase<MethodInfo>
        {
            public MetadataMethodInfoImpl(MethodInfo method) : base(method) { }

            private IParameterInfo? FReturnValue;
            public override IParameterInfo ReturnValue => FReturnValue ??= MetadataParameterInfo.CreateFrom(UnderlyingMethod.ReturnParameter);

            public override bool SignatureEquals(IMethodInfo that, bool ignoreVisibility) => 
                that is MetadataMethodInfoImpl thatMethod && UnderlyingMethod.SignatureEquals(thatMethod.UnderlyingMethod, ignoreVisibility);
        }

        private sealed class MetadataGenericMethodInfo : MetadataMethodInfoImpl, IGenericMethodInfo
        {
            public MetadataGenericMethodInfo(MethodInfo method) : base(method) { }

            public bool IsGenericDefinition => UnderlyingMethod.IsGenericMethodDefinition;

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingMethod
                .GetGenericArguments()
                .Select(MetadataTypeInfo.CreateFrom)
                .ToArray();

            private IGeneric? FGenericDefinition;
            public IGeneric GenericDefinition => FGenericDefinition ??= new MetadataGenericMethodInfo
            (
                UnderlyingMethod.GetGenericMethodDefinition()
            );

            public IGeneric Close(params ITypeInfo[] genericArgs) => new MetadataGenericMethodInfo
            (
                UnderlyingMethod.MakeGenericMethod
                (
                    genericArgs
                        .Select(MetadataTypeInfo.TypeInfoToMetadata)
                        .ToArray()
                )
            );
        }

        private sealed class MetadataConstructorInfo : MetadataMethodBase<ConstructorInfo>, IConstructorInfo 
        {
            public MetadataConstructorInfo(ConstructorInfo method) : base(method) { }

            public override IParameterInfo ReturnValue { get; } = null!;

            public override bool SignatureEquals(IMethodInfo that, bool ignoreVisibility) => ReferenceEquals(this, that);
        }
    }
}
