/********************************************************************************
* ProxyEmbedder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
    [Generator(LanguageNames.CSharp)]
    #pragma warning restore CS3016
    internal sealed class ProxyEmbedder:
#if SRCGEN_V2_API
        ProxyEmbedder_RoslynV4
#else
        ProxyEmbedder_RoslynV3
#endif
    {
    }
}
