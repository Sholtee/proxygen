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
        // In case of explicit implementation the name is in form of "Interface.Tag"
        //

        private static readonly Regex FStripper = new("([\\w]+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string StrippedName(this ISymbol self) => FStripper.Match(self.MetadataName).Value;

        public static void EnsureNotError(this ISymbol self)
        {
            if (self.Kind is SymbolKind.ErrorType)
                throw new InvalidSymbolException(self);
        }
    }
}
