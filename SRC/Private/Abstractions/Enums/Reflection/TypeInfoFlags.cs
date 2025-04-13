/********************************************************************************
* TypeInfoFlags.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    [Flags]
    internal enum TypeInfoFlags
    {
        None = 0,
        IsVoid = 1 << 0,
        /// <summary>
        /// The type is nested and not a generic parameter.
        /// </summary>
        IsNested = 1 << 1,
        /// <summary>
        /// The type represents a generic parameter (for e.g.: "T" in <see cref="List{T}"/>).
        /// </summary>
        IsGenericParameter = 1 << 2,
        IsInterface = 1 << 3,
        IsClass = 1 << 4,
        IsFinal = 1 << 5,
        IsAbstract = 1 << 6,
        IsDelegate = 1 << 7,
    }
}
