/********************************************************************************
* ITypeGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes a type generator.
    /// </summary>
    internal interface ITypeGenerator
    {
        /// <summary>
        /// The resolution strategy used to resolve the generated <see cref="Type"/>.
        /// </summary>
        ITypeResolution TypeResolutionStrategy { get; }
    }
}
