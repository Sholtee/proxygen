/********************************************************************************
* ISymbolExtensions.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ISymbolExtensions
    {
        //
        // Explicit implementacional a nev "Interface.Tag" formaban van
        //

        private static readonly Regex FStripper = new Regex("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this ISymbol self) => FStripper.Match(self.Name).Value;
    }
}
