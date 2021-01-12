/********************************************************************************
* InvalidSymbolException.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal class InvalidSymbolException: Exception
    {
        public ISymbol? Symbol { get; set; }
    }
}
