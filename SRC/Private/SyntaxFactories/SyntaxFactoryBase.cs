/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class SyntaxFactoryBase
    {
        public ReferenceCollector? ReferenceCollector { get; }

        public LanguageVersion LanguageVersion { get; }

        public SyntaxFactoryBase(ReferenceCollector? referenceCollector, LanguageVersion languageVersion)
        {
            LanguageVersion= languageVersion;
            ReferenceCollector = referenceCollector;
        }
    }
}