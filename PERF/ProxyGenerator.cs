/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using static Consts;

    [MemoryDiagnoser]
    public class ProxyGenerator
    {
        public class InterfaceProxy: InterfaceInterceptor<IInterface>
        {
            public InterfaceProxy(IInterface target) : base(target)
            {
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void AssemblingProxyType()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = ProxyGenerator<IInterface, InterfaceProxy>.GeneratedType;
            }
        }
    }
}
