/********************************************************************************
* SourceCode.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class SourceCode(string hint, CompilationUnitSyntax unit)
    {
        public string Hint { get; } = hint;

        public SourceText Value { get; } = SourceText.From
        (
            unit.NormalizeWhitespace(eol: Environment.NewLine).ToFullString(),
            Encoding.UTF8
        );
    }
}
