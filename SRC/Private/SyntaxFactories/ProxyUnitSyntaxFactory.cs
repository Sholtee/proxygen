/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory(ITypeInfo? targetType, OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector, LanguageVersion languageVersion) : ProxyUnitSyntaxFactoryBase(targetType, outputType, containingAssembly, referenceCollector, languageVersion)
    {
    }
}