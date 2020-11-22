/********************************************************************************
* MetadataPropertyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataPropertyInfo : IPropertyInfo
    {
        private PropertyInfo UnderLyingProperty { get; }

        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => FGetMethod ??= UnderLyingProperty.CanRead ? MetadataMethodInfo.CreateFrom(UnderLyingProperty.GetMethod) : null;

        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => FSetMethod ??= UnderLyingProperty.CanWrite ? MetadataMethodInfo.CreateFrom(UnderLyingProperty.SetMethod) : null;

        public string Name => UnderLyingProperty.StrippedName();

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderLyingProperty.PropertyType);

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= MetadataTypeInfo.CreateFrom(UnderLyingProperty.DeclaringType);

        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderLyingProperty
            .GetIndexParameters()
            .Select(MetadataParameterInfo.CreateFrom)
            .ToArray();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        private MetadataPropertyInfo(PropertyInfo prop) => UnderLyingProperty = prop;

        public static IPropertyInfo CreateFrom(PropertyInfo prop) => new MetadataPropertyInfo(prop);

        public override bool Equals(object obj) => obj is MetadataPropertyInfo self && UnderLyingProperty.Equals(self.UnderLyingProperty);

        public override int GetHashCode() => UnderLyingProperty.GetHashCode();
    }
}
