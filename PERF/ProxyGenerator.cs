/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Moq;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using Internals;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 30000)]
    public class ProxyGenerator
    {
        private const int OPERATIONS_PER_INVOKE = 100;

        private static readonly IAssemblyCachingConfiguration FCachingConfiguration = new Mock<IAssemblyCachingConfiguration>().Object;

        private sealed class Interceptor : IInterceptor
        {
            public object Invoke(IInvocationContext context) => context.Dispatch();
        }

        private static readonly Interceptor FInterceptor = new();

        [Benchmark]
        public void AssemblingProxyType() => InterfaceProxyGenerator<IInterface>
            .Instance
            .EmitAsync(FCachingConfiguration, SyntaxFactoryContext.Default with { AssemblyNameOverride = Guid.NewGuid().ToString() }, default)
            .GetAwaiter()
            .GetResult();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void GetGeneratedType()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                _ = InterfaceProxyGenerator<IInterface>.GetGeneratedType();
            }
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void Activate()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                _ = InterfaceProxyGenerator<IInterface>.Activate(FInterceptor);
            }
        }
    }
}
