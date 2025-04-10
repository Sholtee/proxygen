/********************************************************************************
* MetadataPropertyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataPropertyInfo : IPropertyInfo
    {
        private PropertyInfo UnderlyingProperty { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => UnderlyingProperty.GetMethod is not null
            ? FGetMethod ??= MetadataMethodInfo.CreateFrom(UnderlyingProperty.GetMethod) 
            : null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => UnderlyingProperty.SetMethod is not null 
            ? FSetMethod ??= MetadataMethodInfo.CreateFrom(UnderlyingProperty.SetMethod)
            : null;

        public string Name => UnderlyingProperty.StrippedName();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderlyingProperty.PropertyType);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderlyingProperty
            .GetIndexParameters()
            .Select(MetadataParameterInfo.CreateFrom)
            .ToImmutableList();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        public bool IsAbstract => (GetMethod ?? SetMethod!).IsAbstract;

        public bool IsVirtual => (GetMethod ?? SetMethod!).IsVirtual;

        private MetadataPropertyInfo(PropertyInfo prop) => UnderlyingProperty = prop;

        public static IPropertyInfo CreateFrom(PropertyInfo prop) => new MetadataPropertyInfo(prop);

        public override bool Equals(object obj) => obj is MetadataPropertyInfo that && UnderlyingProperty.Equals(that.UnderlyingProperty);

        public override int GetHashCode() => UnderlyingProperty.GetHashCode();

        public override string ToString() => UnderlyingProperty.ToString();
    }
}
