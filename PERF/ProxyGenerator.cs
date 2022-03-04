/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using Internals;

    [MemoryDiagnoser]
    public class ProxyGenerator
    {
        public class InterfaceProxy: InterfaceInterceptor<IInterface>
        {
            public InterfaceProxy(IInterface target) : base(target)
            {
            }
        }

        [Benchmark]
        public void AssemblingProxyType() => TypeEmitter.Emit
        (
            ProxyGenerator<IInterface, InterfaceProxy>
                .Instance
                .GetSyntaxFactory(Guid.NewGuid().ToString()),
            null, 
            default
        );
    }
}
