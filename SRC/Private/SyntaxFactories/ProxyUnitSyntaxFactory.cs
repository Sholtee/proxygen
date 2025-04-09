/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector, LanguageVersion languageVersion) : ProxyUnitSyntaxFactoryBase(outputType, containingAssembly, referenceCollector, languageVersion)
    {
    }
}