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
        protected override IEnumerable<ConstructorDeclarationSyntax> ResolveConstructors(DuckSyntaxFactory self)
        {
            foreach (IConstructorInfo ctor in self.BaseType.GetPublicConstructors())
            {
                yield return ResolveConstructor(self, ctor);
            }
        }

        #if DEBUG
        internal
        #endif
        protected override ConstructorDeclarationSyntax ResolveConstructor(DuckSyntaxFactory self, IConstructorInfo ctor) => DeclareCtor(ctor, ResolveClassName(self));
    }
}