/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy
{
    using Properties;
    using Internals;

    /// <summary>
    /// Provides the mechanism for intercepting interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
    public class InterfaceInterceptor<TInterface>: IHasTarget<TInterface?>, IProxyAccess<TInterface> where TInterface: class
    {
        /// <summary>
        /// The target of this interceptor.
        /// </summary>
        public TInterface? Target { get; }

        /// <summary>
        /// The most outer enclosing proxy.
        /// </summary>
        public TInterface Proxy
        {
            set 
            {
                if (Target is IProxyAccess<TInterface> proxyAccess)
                    proxyAccess.Proxy = value ?? throw new ArgumentNullException(nameof(value));
            } 
        }

        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        public InterfaceInterceptor(TInterface? target) => Target = target;

        /// <summary>
        /// Called on proxy method invocation.
        /// </summary>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        public virtual object? Invoke(InvocationContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (Target is null)
                throw new InvalidOperationException(Resources.NULL_TARGET);

            return context.InvokeTarget();
        }
    }
}
