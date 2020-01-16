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
        Unknown = 0,
        Private,
        Protected,
        Internal,
        Public
    }
}
