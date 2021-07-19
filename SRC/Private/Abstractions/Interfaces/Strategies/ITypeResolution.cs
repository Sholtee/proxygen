/********************************************************************************
* ITypeResolution.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Represents the method how the proxy <see cref="Type"/> will be resolved.
    /// </summary>
    internal interface ITypeResolution
    {
        /// <summary>
        /// Tries to resolves the generated <see cref="Type"/>.
        /// </summary>
        Type? TryResolve(CancellationToken cancellation = default);
    }
}
