/********************************************************************************
* ConcurrentInterfaceInterceptor.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Provides the mechanism for intercepting interface method calls in thread safe manner.
    /// </summary>
    public class ConcurrentInterfaceInterceptor<TInterface>: InterfaceInterceptor<TInterface> where TInterface : class
    {
        private readonly ThreadLocal<Func<object>?> FInvokeTarget = new ThreadLocal<Func<object>?>();

        /// <inheritdoc/>
        protected internal override Func<object>? InvokeTarget 
        { 
            get => FInvokeTarget.Value; 
            set => FInvokeTarget.Value = value;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                FInvokeTarget.Dispose();

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Creates a new <see cref="ConcurrentInterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        public ConcurrentInterfaceInterceptor(TInterface? target) : base(target) { }
    }
}
