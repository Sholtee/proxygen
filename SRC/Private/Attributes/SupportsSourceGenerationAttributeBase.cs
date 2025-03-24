/********************************************************************************
* SupportsSourceGenerationAttributeBase.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal abstract class SupportsSourceGenerationAttributeBase : Attribute, ISupportsSourceGeneration
    {
        public abstract ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, Compilation compilation, ReferenceCollector? referenceCollector);
    }
}
