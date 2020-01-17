/********************************************************************************
* AccessModifiers.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    [Flags]
    internal enum AccessModifiers
    {
        Unknown   = 0,
        Private   = 1,
        Explicit  = 2,
        Protected = 4,
        Internal  = 8,
        Public    = 16
    }
}
