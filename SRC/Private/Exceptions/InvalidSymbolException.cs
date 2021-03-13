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
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Only the parameterless constructor is used.")]
    internal class InvalidSymbolException: Exception
    {
        public ISymbol? Symbol { get; set; }
    }
}
