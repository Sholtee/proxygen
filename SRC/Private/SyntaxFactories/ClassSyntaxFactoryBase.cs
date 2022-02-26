/********************************************************************************
* ClassSyntaxFactoryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase: SyntaxFactoryBase, IClassSyntaxFactory
    {
        public ITypeInfo SourceType { get; }

        public ClassSyntaxFactoryBase(ITypeInfo sourceType) => SourceType = sourceType;

        public IReadOnlyCollection<MemberDeclarationSyntax>? Members { get; protected set; }
    }
}