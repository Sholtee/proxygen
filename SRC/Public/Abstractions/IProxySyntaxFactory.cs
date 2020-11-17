/********************************************************************************
* IProxySyntaxFactory.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy.Abstractions
{
    /// <summary>
    /// Describes an abstract proxy syntax factory.
    /// </summary>
    public interface IProxySyntaxFactory : ISyntaxFactory
    {
        /// <summary>
        /// Returns the class name of the proxy.
        /// </summary>
        string ProxyClassName { get; }
    }
}
