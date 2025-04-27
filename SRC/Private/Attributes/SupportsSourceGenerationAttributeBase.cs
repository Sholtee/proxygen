/********************************************************************************
* SupportsSourceGenerationAttributeBase.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal abstract class SupportsSourceGenerationAttributeBase : Attribute, ISupportsSourceGeneration
    {
        public abstract ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context);
    }
}
