﻿/********************************************************************************
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
        private PropertyInfo UnderlyingProperty { get; }

        //
        // Privat property metodusok leszarmazott tipusban nem lathatok
        //

        private PropertyInfo Declaration => UnderlyingProperty.DeclaringType.GetProperty(UnderlyingProperty.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private IMethodInfo? FGetMethod;
        public IMethodInfo? GetMethod => FGetMethod ??= Declaration.GetMethod is not null ? MetadataMethodInfo.CreateFrom(Declaration.GetMethod) : null;

        private IMethodInfo? FSetMethod;
        public IMethodInfo? SetMethod => FSetMethod ??= Declaration.SetMethod is not null ? MetadataMethodInfo.CreateFrom(Declaration.SetMethod) : null;

        public string Name => UnderlyingProperty.StrippedName();

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderlyingProperty.PropertyType);

        private ITypeInfo? FDeclaringType;
        public ITypeInfo DeclaringType => FDeclaringType ??= (GetMethod ?? SetMethod!).DeclaringType;

        private IReadOnlyList<IParameterInfo>? FIndices;
        public IReadOnlyList<IParameterInfo> Indices => FIndices ??= UnderlyingProperty
            .GetIndexParameters()
            .Select(MetadataParameterInfo.CreateFrom)
            .ToArray();

        public bool IsStatic => (GetMethod ?? SetMethod!).IsStatic;

        private MetadataPropertyInfo(PropertyInfo prop) => UnderlyingProperty = prop;

        public static IPropertyInfo CreateFrom(PropertyInfo prop) => new MetadataPropertyInfo(prop);

        public override bool Equals(object obj) => obj is MetadataPropertyInfo that && UnderlyingProperty.Equals(that.UnderlyingProperty);

        public override int GetHashCode() => UnderlyingProperty.GetHashCode();

        public override string ToString() => UnderlyingProperty.ToString();
    }
}
