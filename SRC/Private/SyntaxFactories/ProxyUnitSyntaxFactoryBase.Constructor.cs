/********************************************************************************
* ProxyUnitSyntaxFactory.Constructor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxyUnitSyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context) => cls.AddMembers
        (
            ResolveConstructor
            (
                MetadataTypeInfo.CreateFrom(typeof(object))
                    .Constructors
                    .Single(),
                cls.Identifier
            )
        );
    }
}