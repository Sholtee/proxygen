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
    using Attributes;

    /// <summary>
    /// Base class of attributes that are used to mark generators supporting source generation. This annotation is required to let the system recognize generators in <code>[assembly: EmbedGeneratedType(typeof(XxXGenerator))]</code> expressions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal abstract class SupportsSourceGenerationAttributeBase : Attribute, ISupportsSourceGeneration
    {
        /// <summary>
        /// When implemented, returns the <see cref="ProxyUnitSyntaxFactoryBase"/> that creates the source content in compile time.
        /// </summary>
        /// <param name="generator">The symbol representing the type parameter of <see cref="EmbedGeneratedTypeAttribute"/></param>
        /// <param name="compilation">The actual compilation</param>
        /// <param name="context">The context to be passed to the unit syntax factory.</param>
        public abstract ProxyUnitSyntaxFactoryBase CreateMainUnit(INamedTypeSymbol generator, CSharpCompilation compilation, SyntaxFactoryContext context);
    }
}
