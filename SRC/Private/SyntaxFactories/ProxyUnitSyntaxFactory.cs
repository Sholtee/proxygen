/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory : UnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector): base(outputType, referenceCollector) =>
            ContainingAssembly = containingAssembly;

        public string ContainingAssembly { get; }

        //
        // Proxy egyseg mindig csak egy osztalyt definial
        //

        public override IReadOnlyCollection<string> DefinedClasses => new string[]
        {
            ResolveClassName(null!)
        };

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