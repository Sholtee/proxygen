/********************************************************************************
* ISupportsSourceGeneration.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal interface ISupportsSourceGeneration
    {
        ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context);
    }
}
