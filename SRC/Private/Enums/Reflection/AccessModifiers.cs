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
        Private   = 1 << 0,
        Explicit  = 1 << 1,
        Protected = 1 << 2,
        Internal  = 1 << 3,
        Public    = 1 << 4
    }
}
