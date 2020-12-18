/********************************************************************************
* ConcurrentInterfaceInterceptor.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Provides the mechanism for intercepting interface method calls in thread safe manner.
    /// </summary>
    public class ConcurrentInterfaceInterceptor<TInterface>: InterfaceInterceptor<TInterface> where TInterface : class
    {
        [ThreadStatic]
        private static Func<object>? FInvokeTarget;

        /// <inheritdoc/>
        protected internal override Func<object>? InvokeTarget
        {
            get => FInvokeTarget;
            set => FInvokeTarget = value;
        }

        /// <summary>
        /// Creates a new <see cref="ConcurrentInterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        public ConcurrentInterfaceInterceptor(TInterface? target) : base(target) { }
    }
}
