/********************************************************************************
* ProxyActivator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ProxyActivator
    {
        public static Func<object?, object> Create(Type proxyType) => (Func<object?, object>) 
        (
            proxyType
                .GetField(ProxyUnitSyntaxFactory.ACTIVATOR_NAME, BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) ?? throw new NotSupportedException()
        );
    }
}
