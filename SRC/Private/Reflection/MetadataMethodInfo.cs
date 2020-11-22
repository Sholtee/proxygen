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
            protected T UnderLyingMethod { get; }

            protected MetadataMethodBase(T method) => UnderLyingMethod = method;

            private IReadOnlyList<IParameterInfo>? FParameters;
            public IReadOnlyList<IParameterInfo> Parameters => FParameters ??= UnderLyingMethod
                .GetParameters()
                .Select(MetadataParameterInfo.CreateFrom)
                .ToArray();

            public abstract IParameterInfo ReturnValue { get; }

            public string Name => UnderLyingMethod.StrippedName();

            private ITypeInfo? FDeclaringType;
            public ITypeInfo DeclaringType => FDeclaringType ??= MetadataTypeInfo.CreateFrom(UnderLyingMethod.GetDeclaringType());

            public bool IsStatic => UnderLyingMethod.IsStatic;

            public bool IsSpecial => UnderLyingMethod.IsSpecialName;

            public AccessModifiers AccessModifiers => UnderLyingMethod.GetAccessModifiers();

            public override bool Equals(object obj) => obj is MetadataMethodBase<T> self && UnderLyingMethod.Equals(self.UnderLyingMethod);

            public override int GetHashCode() => UnderLyingMethod.GetHashCode();
        }

        private class MetadataMethodInfoImpl : MetadataMethodBase<MethodInfo>
        {
            public MetadataMethodInfoImpl(MethodInfo method) : base(method) { }

            private IParameterInfo? FReturnValue;
            public override IParameterInfo ReturnValue => FReturnValue ??= MetadataParameterInfo.CreateFrom(UnderLyingMethod.ReturnParameter);
        }

        private sealed class MetadataGenericMethodInfo : MetadataMethodInfoImpl, IGenericMethodInfo
        {
            public MetadataGenericMethodInfo(MethodInfo method) : base(method) { }

            public bool IsGenericDefinition => UnderLyingMethod.IsGenericMethodDefinition;

            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderLyingMethod
                .GetGenericArguments()
                .Select(MetadataTypeInfo.CreateFrom)
                .ToArray();

            public IGeneric GenericDefinition => new MetadataGenericMethodInfo(UnderLyingMethod.GetGenericMethodDefinition());
        }

        private sealed class MetadataConstructorInfo : MetadataMethodBase<ConstructorInfo>, IConstructorInfo 
        {
            public MetadataConstructorInfo(ConstructorInfo method) : base(method) { }

            public override IParameterInfo ReturnValue { get; } = null!;
        }
    }
}
