﻿/********************************************************************************
* MetadataParameterInfo.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class MetadataParameterInfo : IParameterInfo
    {
        private ParameterInfo UnderlyingParameter { get; }

        private MetadataParameterInfo(ParameterInfo para) => UnderlyingParameter = para;

        public static IParameterInfo CreateFrom(ParameterInfo para) => new MetadataParameterInfo(para);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderlyingParameter.ParameterType);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ParameterKind? FKind;
        public ParameterKind Kind => FKind ??= UnderlyingParameter.GetParameterKind();

        public string Name => UnderlyingParameter.Name ?? string.Empty;

        public override bool Equals(object obj) => obj is MetadataParameterInfo that && UnderlyingParameter.Equals(that.UnderlyingParameter);

        public override int GetHashCode() => UnderlyingParameter.GetHashCode();

        public override string ToString() => UnderlyingParameter.ToString();
    }
}