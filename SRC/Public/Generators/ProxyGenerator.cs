/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Generators
{
    using Abstractions;
    using Internals;
    using Properties;

    /// <summary>
    /// Type generator for creating proxies that intercept interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to which the proxy will be created.</typeparam>
    /// <typeparam name="TInterceptor">An <see cref="InterfaceInterceptor{TInterface}"/> descendant that has at least one public constructor.</typeparam>
    public sealed class ProxyGenerator<TInterface, TInterceptor> : TypeGenerator<ProxyGenerator<TInterface, TInterceptor>> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        /// <summary>
        /// See <see cref="ITypeGenerator"/>.
        /// </summary>
        public override IUnitSyntaxFactory SyntaxFactory { get; } = new ProxySyntaxFactory
        (
            MetadataTypeInfo.CreateFrom(typeof(TInterface)),
            MetadataTypeInfo.CreateFrom(typeof(TInterceptor)),
            OutputType.Module
        );

        /// <summary>
        /// See <see cref="TypeGenerator{T}"/>.
        /// </summary>
        protected override void DoCheck()
        {
            CheckInterface();
            CheckBase();
        }

        private void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface) throw new InvalidOperationException(Resources.NOT_AN_INTERFACE);
            if (type.ContainsGenericParameters) throw new InvalidOperationException();
        }

        private void CheckBase()
        {
            Type type = typeof(TInterceptor);

            CheckVisibility(type);

            if (!type.IsClass) throw new InvalidOperationException(Resources.NOT_A_CLASS);
            if (type.IsSealed) throw new InvalidOperationException(Resources.SEALED_INTERCEPTOR);          
            if (type.IsAbstract) throw new NotSupportedException(Resources.ABSTRACT_INTERCEPTOR);
            if (type.ContainsGenericParameters) throw new InvalidOperationException();
        }
    }
}