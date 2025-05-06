/********************************************************************************
* TypeInfoFlags.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Represents the additional read-only boolean properties of a particular type.
    /// </summary>
    [Flags]
    internal enum TypeInfoFlags
    {
        /// <summary>
        /// No special flags are set
        /// </summary>
        None = 0,

        /// <summary>
        /// The type represents the <see cref="Void"/> type
        /// </summary>
        IsVoid = 1 << 0,

        /// <summary>
        /// The type is nested and not a generic parameter.
        /// </summary>
        IsNested = 1 << 1,

        /// <summary>
        /// The type represents a generic parameter (for e.g.: "T" in <see cref="List{T}"/>).
        /// </summary>
        IsGenericParameter = 1 << 2,

        /// <summary>
        /// The type is an interface
        /// </summary>
        IsInterface = 1 << 3,

        /// <summary>
        /// The type is a class
        /// </summary>
        IsClass = 1 << 4,

        /// <summary>
        /// The type is sealed.
        /// </summary>
        IsFinal = 1 << 5,

        /// <summary>
        /// The type is abstract (not applied for interfaces)
        /// </summary>
        IsAbstract = 1 << 6,

        /// <summary>
        /// The type is a delegate, func or action
        /// </summary>
        IsDelegate = 1 << 7,
    }
}
