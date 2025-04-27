/********************************************************************************
* IDelegateWrapper.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Contract that specifies how to get the wrapped delegate
    /// </summary>
    /// <remarks>This interface is used by the generated proxies only therefore it is considered internal. User code should not depend on it.</remarks>
    public interface IDelegateWrapper
    {
        /// <summary>
        /// The wrapped delegate
        /// </summary>
        Delegate Wrapped { get; } 
    }
}
