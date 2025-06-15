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
    /// <summary>
    /// Exception to be thrown when our source generator encounters an invalid symbol. This exception should never reach the end-user therefore it doesn't have to be public. 
    /// </summary>
    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "This exception should never reach the end-user.")]
    internal sealed class InvalidSymbolException(ISymbol symbol) : Exception
    {
        /// <summary>
        /// The symbol that triggered this exception.
        /// </summary>
        public ISymbol? Symbol { get; } = symbol;
    }
}
