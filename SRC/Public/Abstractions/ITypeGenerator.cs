﻿/********************************************************************************
* ITypeGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Describes a type generator.
    /// </summary>
    public interface ITypeGenerator
    {
        /// <summary>
        /// The resolution strategy used to resolve the generated <see cref="Type"/>.
        /// </summary>
        ITypeResolutionStrategy TypeResolutionStrategy { get; }
    }
}
