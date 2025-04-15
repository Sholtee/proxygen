/********************************************************************************
* ProxyUnitSyntaxFactoryBase.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactoryBase(ITypeInfo? targetType, SyntaxFactoryContext context) : UnitSyntaxFactoryBase(context)
    {
        public ITypeInfo? TargetType { get; } = targetType;

        public override CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            if (TargetType is not null)
                Visibility.Check(TargetType, ContainingAssembly);

            return base.ResolveUnit(context, cancellation);
        }

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