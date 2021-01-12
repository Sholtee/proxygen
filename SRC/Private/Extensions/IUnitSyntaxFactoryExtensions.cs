/********************************************************************************
* IUnitSyntaxFactoryExtensions.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal static class IUnitSyntaxFactoryExtensions
    {
        public static SourceCode GetSourceCode(this IUnitSyntaxFactory src, string hint, CancellationToken cancellation) 
        {
            src.Build(cancellation);

            return new SourceCode(hint, src.Unit!.NormalizeWhitespace(eol: Environment.NewLine).ToFullString());
        }
    }
}
