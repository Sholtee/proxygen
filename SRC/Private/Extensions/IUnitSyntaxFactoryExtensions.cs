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
        public static string GetSourceCode(this IUnitSyntaxFactory src, CancellationToken cancellation) 
        {
            src.Build(cancellation);

            return src.Unit!.NormalizeWhitespace(eol: Environment.NewLine).ToFullString();
        }
    }
}
