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

        public string Name => UnderLyingParameter.Name;

        public override bool Equals(object obj) => obj is MetadataParameterInfo self && UnderLyingParameter.Equals(self.UnderLyingParameter);

        public override int GetHashCode() => UnderLyingParameter.GetHashCode();
    }
}