/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory : ProxyUnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector, LanguageVersion languageVersion) : base(outputType, containingAssembly, referenceCollector, languageVersion) { }
    }
}