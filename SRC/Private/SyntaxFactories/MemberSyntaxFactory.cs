/********************************************************************************
* MemberSyntaxFactory.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class MemberSyntaxFactory: SyntaxFactoryBase, IMemberSyntaxFactory
    {
        public ITypeInfo SourceType { get; }

        public MemberSyntaxFactory(ITypeInfo sourceType) => SourceType = sourceType;

        public IReadOnlyCollection<MemberDeclarationSyntax>? Members { get; protected set; }
    }
}