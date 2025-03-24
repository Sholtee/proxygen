/********************************************************************************
* ISupportsSourceGeneration.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal interface ISupportsSourceGeneration
    {
        ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, Compilation compilation, ReferenceCollector? referenceCollector);
    }
}
