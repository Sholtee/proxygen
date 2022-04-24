/********************************************************************************
* DuckSyntaxFactory.ConstructorFactory.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveConstructors(object context)
        {
            foreach (IConstructorInfo ctor in BaseType.GetPublicConstructors())
            {
                foreach (MemberDeclarationSyntax member in ResolveConstructor(null!, ctor))
                {
                    yield return member;
                }
            }
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveConstructor(object context, IConstructorInfo ctor)
        {
            yield return ResolveConstructor(ctor, ResolveClassName(null!));
        }
    }
}