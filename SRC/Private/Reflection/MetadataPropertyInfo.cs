/********************************************************************************
* MetadataPropertyInfo.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataPropertyInfo : IPropertyInfo
    {
        private PropertyInfo UnderlyingProperty { get; }

        //
        // Privat property metodusok leszarmazott tipusban nem lathatok
        //

        private PropertyInfo? FUnderlyingOriginalProperty;
        private PropertyInfo UnderlyingOriginalProperty => FUnderlyingOriginalProperty ??= UnderlyingProperty
            .DeclaringType
            .GetProperty(UnderlyingProperty.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => FGetMethod ??= UnderlyingOriginalProperty.GetMethod is not null ? MetadataMethodInfo.CreateFrom(UnderlyingOriginalProperty.GetMethod) : null;

        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => FSetMethod ??= UnderlyingOriginalProperty.SetMethod is not null ? MetadataMethodInfo.CreateFrom(UnderlyingOriginalProperty.SetMethod) : null;

        public string Name => UnderlyingProperty.StrippedName();

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderlyingProperty.PropertyType);

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderlyingProperty.GetIndexParameters().ConvertAr(MetadataParameterInfo.CreateFrom);

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        private MetadataPropertyInfo(PropertyInfo prop) => UnderlyingProperty = prop;

        public static IPropertyInfo CreateFrom(PropertyInfo prop) => new MetadataPropertyInfo(prop);

        public override bool Equals(object obj) => obj is MetadataPropertyInfo that && UnderlyingProperty.Equals(that.UnderlyingProperty);

        public override int GetHashCode() => UnderlyingProperty.GetHashCode();

        public override string ToString() => UnderlyingProperty.ToString();
    }
}
