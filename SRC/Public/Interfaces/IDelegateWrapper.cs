/********************************************************************************
* IDelegateWrapper.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Contract that specifies how to get the wrapped delegate
    /// </summary>
    public interface IDelegateWrapper
    {
        /// <summary>
        /// The wrapped delegate
        /// </summary>
        Delegate Wrapped { get; } 
    }
}
