/********************************************************************************
* InterfaceInterceptor{TInterface}r.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Provides the mechanism for intercepting interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
    public class InterfaceInterceptor<TInterface> : InterfaceInterceptor<TInterface, TInterface> where TInterface : class
    {
        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        public InterfaceInterceptor(TInterface? target): base(target) { }
    }
}
