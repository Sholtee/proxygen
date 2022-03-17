/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 30000)]
    public class ProxyGenerator
    {
        private const int OPERATIONS_PER_INVOKE = 100;

        public class InterfaceProxy: InterfaceInterceptor<IInterface>
        {
            public InterfaceProxy(IInterface target) : base(target)
            {
            }
        }

        [Benchmark]
        public void AssemblingProxyType() => ProxyGenerator<IInterface, InterfaceProxy>
            .Instance
            .Emit(Guid.NewGuid().ToString(), default, default);

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void GetGeneratedType()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                ProxyGenerator<IInterface, InterfaceProxy>.GetGeneratedType();
            }
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void Activate()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                ProxyGenerator<IInterface, InterfaceProxy>.Activate(Tuple.Create((IInterface) null));
            }
        }
    }
}
