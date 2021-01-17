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
    internal class SourceCode 
    {
        public string Hint { get; }

        public SourceText Value { get; }

        public SourceCode(string hint, CompilationUnitSyntax unit) 
        {
            Hint = hint;
            Value = SourceText.From
            (
                unit.NormalizeWhitespace(eol: Environment.NewLine).ToFullString(),
                Encoding.UTF8
            );
        }
    }
}
