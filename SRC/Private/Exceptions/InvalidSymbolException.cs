/********************************************************************************
* InvalidSymbolException.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "This exception should never reach the end-user.")]
    internal class InvalidSymbolException: Exception
    {
        public InvalidSymbolException(ISymbol symbol) => Symbol = symbol;

        public ISymbol? Symbol { get; }
    }
}
