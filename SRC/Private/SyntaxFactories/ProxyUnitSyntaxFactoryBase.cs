/********************************************************************************
* ProxyUnitSyntaxFactoryBase.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactoryBase : UnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactoryBase(OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector, LanguageVersion languageVersion): base(outputType, referenceCollector, languageVersion) =>
            ContainingAssembly = containingAssembly;

        public string ContainingAssembly { get; }

        public override string ExposedClass => ResolveClassName(null!);

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<Func<ClassDeclarationSyntax, object, ClassDeclarationSyntax>> MemberResolvers
        {
            get
            {
                foreach (Func<ClassDeclarationSyntax, object, ClassDeclarationSyntax> resolver in base.MemberResolvers)
                {
                    yield return resolver;
                }
                yield return ResolveActivator;
                yield return ResolveInitializer;
            }
        }
    }
}