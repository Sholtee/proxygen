/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Proxy.Perf
{
    using Generators;
    using Internals;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 30000)]
    public class DuckGenerator
    {
        private const int OPERATIONS_PER_INVOKE = 100;

        public class Implementation
        {
            public int DoSomething(string param) => param.GetHashCode();
        }

        [Benchmark]
        public void AssemblingProxyType() => DuckGenerator<IInterface, Implementation>
            .Instance
            .EmitAsync(SyntaxFactoryContext.Default with { AssemblyNameOverride = Guid.NewGuid().ToString() }, default)
            .GetAwaiter()
            .GetResult();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void GetGeneratedType()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                DuckGenerator<IInterface, Implementation>.GetGeneratedType();
            }
        }

        private static readonly Implementation FImplementation = new();

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public void Activate()
        {
            for (int i = 0; i < OPERATIONS_PER_INVOKE; i++)
            {
                DuckGenerator<IInterface, Implementation>.Activate(FImplementation);
            }
        }
    }
}
