/********************************************************************************
* IUnitSyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal interface IUnitSyntaxFactory : ISyntaxFactory 
    {
        CompilationUnitSyntax? Unit { get; }
        
        OutputType OutputType { get; }

        IReadOnlyCollection<string> DefinedClasses { get; }
    }
}
