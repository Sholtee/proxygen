/********************************************************************************
* ISymbolExtensions.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Helper methods for the <see cref="ISymbol"/> interface.
    /// </summary>
    internal static class ISymbolExtensions
    {
        //
        // In case of explicit implementation the name is in form of "Interface.Tag"
        //

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Regex FStripper = new("(\\w+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Returns the raw name of the symbol (without any prefix).
        /// </summary>
        public static string StrippedName(this ISymbol self) => FStripper.Match(self.MetadataName).Value;

        /// <summary>
        /// Throws an <see cref="InvalidSymbolException"/> if the given symbol is not valid.
        /// </summary>
        public static void EnsureNotError(this ISymbol self)
        {
            if (self.Kind is SymbolKind.ErrorType)
                throw new InvalidSymbolException(self);
        }
    }
}
