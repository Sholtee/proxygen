/********************************************************************************
* SourceCode.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SourceCode(string hint, CompilationUnitSyntax unit)
    {
        public string Hint { get; } = hint;

        public SourceText Value { get; } = SourceText.From
        (
            unit.Stringify(),
            Encoding.UTF8
        );
    }
}
