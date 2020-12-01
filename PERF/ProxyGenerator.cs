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
        public void AssemblingProxyType() =>
            new ProxyGenerator<IInterface, InterfaceProxy>().GenerateType(null, Guid.NewGuid().ToString());
    }
}
