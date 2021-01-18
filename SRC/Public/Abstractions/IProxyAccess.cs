/********************************************************************************
* IProxyAccess.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Class that implement this interface has access to the proxy instance that targets it.
    /// </summary>
    public interface IProxyAccess<TInterface> where TInterface: class
    {
        /// <summary>
        /// The most outer enclosing proxy.
        /// </summary>
        TInterface Proxy { set; }
    }
}
