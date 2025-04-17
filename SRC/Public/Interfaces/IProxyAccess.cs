/********************************************************************************
* IProxyAccess.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Class implementing this interface has access to the proxy instance that targets it.
    /// </summary>
    public interface IProxyAccess<TInterface> where TInterface: class
    {
        /// <summary>
        /// The most outer enclosing proxy.
        /// </summary>
        [SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "The system needs a setter only although the implementers may define a getter for this property")]
        TInterface Proxy { set; }
    }
}
