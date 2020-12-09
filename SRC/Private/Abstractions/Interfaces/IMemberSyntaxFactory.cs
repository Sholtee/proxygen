/********************************************************************************
* IMemberSyntaxFactory.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IMemberSyntaxFactory : ISyntaxFactory 
    {
        IReadOnlyCollection<MemberDeclarationSyntax>? Members { get; }
    }
}
