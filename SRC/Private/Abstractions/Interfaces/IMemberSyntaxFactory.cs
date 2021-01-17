/********************************************************************************
* IMemberSyntaxFactory.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IMemberSyntaxFactory : ISyntaxFactory 
    {
        IReadOnlyCollection<MemberDeclarationSyntax>? Members { get; }
    }
}
