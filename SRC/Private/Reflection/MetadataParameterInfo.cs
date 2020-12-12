/********************************************************************************
* MetadataParameterInfo.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataParameterInfo : IParameterInfo
    {
        private ParameterInfo UnderLyingParameter { get; }

        private MetadataParameterInfo(ParameterInfo para) => UnderLyingParameter = para;

        public static IParameterInfo CreateFrom(ParameterInfo para) => new MetadataParameterInfo(para);

        private ITypeInfo? FType;
        public ITypeInfo Type => FType ??= MetadataTypeInfo.CreateFrom(UnderLyingParameter.ParameterType);

        public ParameterKind Kind => UnderLyingParameter.GetParameterKind();

        public string Name => UnderLyingParameter.Name ?? string.Empty;

        public override bool Equals(object obj) => obj is MetadataParameterInfo that && UnderLyingParameter.Equals(that.UnderLyingParameter);

        public override int GetHashCode() => UnderLyingParameter.GetHashCode();

        public override string ToString() => UnderLyingParameter.ToString();
    }
}