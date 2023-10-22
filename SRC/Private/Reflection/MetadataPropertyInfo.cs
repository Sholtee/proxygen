/********************************************************************************
* MetadataPropertyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataPropertyInfo : IPropertyInfo
    {
        private PropertyInfo UnderlyingProperty { get; }

        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => FGetMethod ??= UnderlyingProperty.GetMethod is not null
            ? MetadataMethodInfo.CreateFrom(UnderlyingProperty.GetMethod) 
            : null;

        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => FSetMethod ??= UnderlyingProperty.SetMethod is not null
            ? MetadataMethodInfo.CreateFrom(UnderlyingProperty.SetMethod)
            : null;

        public string Name => UnderlyingProperty.StrippedName();

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderlyingProperty.PropertyType);

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderlyingProperty
            .GetIndexParameters()
            .Select(MetadataParameterInfo.CreateFrom)
            .ToImmutableList();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        private MetadataPropertyInfo(PropertyInfo prop) => UnderlyingProperty = prop;

        public static IPropertyInfo CreateFrom(PropertyInfo prop) => new MetadataPropertyInfo(prop);

        public override bool Equals(object obj) => obj is MetadataPropertyInfo that && UnderlyingProperty.Equals(that.UnderlyingProperty);

        public override int GetHashCode() => UnderlyingProperty.GetHashCode();

        public override string ToString() => UnderlyingProperty.ToString();
    }
}
