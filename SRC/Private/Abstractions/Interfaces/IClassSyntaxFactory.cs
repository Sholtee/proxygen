/********************************************************************************
* IClassSyntaxFactory.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IClassSyntaxFactory : ISyntaxFactory // TODO: torolni
    {
        IReadOnlyCollection<MemberDeclarationSyntax>? Members { get; }
    }
}
