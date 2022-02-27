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
        protected override IEnumerable<ConstructorDeclarationSyntax> ResolveConstructors(object context)
        {
            foreach (IConstructorInfo ctor in BaseType.GetPublicConstructors())
            {
                yield return ResolveConstructor(null!, ctor);
            }
        }

        #if DEBUG
        internal
        #endif
        protected override ConstructorDeclarationSyntax ResolveConstructor(object context, IConstructorInfo ctor) => DeclareCtor(ctor, ResolveClassName(null!));
    }
}